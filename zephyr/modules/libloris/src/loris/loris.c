#include "loris.h"
#include "loris/codec.h"
#include "loris/models/call.h"
#include "loris/models/capabilities.h"
#include "loris/models/downlink.h"
#include "loris/models/report.h"
#include "loris/models/sensor.h"
#include "loris/models/uplink.h"
#include "loris/result.h"
#include "zephyr/sys/clock.h"

#include <stdint.h>
#include <zephyr/kernel.h>
#include <zephyr/logging/log.h>
#include <zephyr/sleep.h>
#include <zephyr/sys/min_heap.h>
#include <zephyr/sys/slist.h>

#define HEAP_CAPACITY CONFIG_LORIS_SENSORS_AMOUNT

LOG_MODULE_REGISTER(main);

typedef struct lora_data {
  char* data;
  int data_size;
} lora_data_t;

typedef struct sensor_time {
  sensor_t* sensor;
  int64_t timestamp;
} sensor_time_t;

typedef struct uplink_wrp {
  uplink_t uplink;
  size_t bytes_written;
} uplink_wrp_t;

struct k_msgq downlink_q;
struct k_msgq uplink_q;
struct k_msgq lora_q;

bool device_connected = false;

static int compare(const void* a, const void* b) {
  const sensor_time_t* st_a = a;
  const sensor_time_t* st_b = b;

  return st_a->timestamp - st_b->timestamp;
}

MIN_HEAP_DEFINE_STATIC(sensors_heap, HEAP_CAPACITY, sizeof(sensor_time_t),
                       __alignof__(sensor_time_t), compare);

extern void uplink_thread(void* capabilities, void* unused1, void* unused2) {
  /* 1. Check if the device is connected
   *  1.a) If it's connected proceed with the usual sensor reporting
   *  1.b) If it's disconnected then we must establish a connection:
   *    - add a payload with capabilities to the downlink_q and wait for
   *      CONFIG_RECONNECT_INTERVAL (It could be improved later on but for now
   *      it'll be simple enough and work).
   */
  if (capabilities == NULL) {
    return;
  }
  capabilities_t* capabilities_ptr = capabilities;
  sensor_t* sensor;

  SYS_SLIST_FOR_EACH_CONTAINER(&capabilities_ptr->sensors, sensor, node) {
    sensor_time_t st = {sensor, 0};
    min_heap_push(&sensors_heap, &st);
  }

  sensor_time_t st;
  while (1) {
    if (device_connected) {
      LOG_DBG("Woke up at: %d\n", (int)k_uptime_get());

      min_heap_pop(&sensors_heap, &st);
      st.timestamp += (st.sensor->wake_interval_sec * 1000);
      min_heap_push(&sensors_heap, &st);

      report_t report = st.sensor->read_func(st.sensor);
      switch (report.which_value) {
      case REPORT_VALUE_INT:
        LOG_INF("Report from sensor %d, reads: %d", st.sensor->id,
                report.value.int_val);
        break;
      case REPORT_VALUE_BOOL:
        LOG_INF("Report from sensor %d, reads: %d", st.sensor->id,
                report.value.bool_val);
        break;
      case REPORT_VALUE_FLOAT:
        LOG_INF("Report from sensor %d, reads: %f", st.sensor->id,
                (double)report.value.float_val);
        break;
      case REPORT_VALUE_STRING:
        LOG_INF("Report from sensor %d, reads: %s", st.sensor->id,
                report.value.str_val.ptr);
        break;
      default:
        LOG_INF("Report from sensor %d, unspecified value", st.sensor->id);
        break;
      }

      uplink_t report_uplink = {.payload.report = report,
                                .payload_type = UPLINK_PAYLOAD_TYPE_REPORT};

      k_msgq_put(&uplink_q, &report_uplink, K_NO_WAIT);

      LOG_DBG("Going to sleep at: %d\n", (int)k_uptime_get());
      k_msleep(((sensor_time_t*)min_heap_peek(&sensors_heap))->timestamp -
               k_uptime_get());
    } else {
      uplink_t capabilities_uplink = {.payload.capabilities = *capabilities_ptr,
                                      .payload_type =
                                          UPLINK_PAYLOAD_TYPE_CAPABILITIES};

      k_msgq_put(&uplink_q, &capabilities_uplink, K_NO_WAIT);
      k_timeout_t timeout = {.ticks = CONFIG_LORIS_RECONNECT_INTERVAL};
      k_sleep(timeout);
    }
  }
}

extern void downlink_thread(void* capabilities, void* unused1, void* unused2) {
  /*
   * 1. Wait for a message from downlink_q
   *  1.a) If a message is a function - run it
   *  1.b) Else it must be a ping - set device_connected flag accordingly
   *    - Now in rest of the threads, the threads check whether the device
   * is connected and what to do.
   */
  if (capabilities == NULL) {
    return;
  }
  capabilities_t* capabilities_ptr = capabilities;
  downlink_t downlink_msg;

  while (1) {
    k_msgq_get(&downlink_q, &downlink_msg, K_FOREVER);
    if (!device_connected) {
      if (downlink_msg.payload_type != DOWNLINK_PAYLOAD_TYPE_PING) {
        continue;
      }
      if (downlink_msg.payload.ping.type != DOWN_PING_TYPE_HELLO) {
        continue;
      }
      device_connected = true;
    } else {
      if (downlink_msg.payload_type == DOWNLINK_PAYLOAD_TYPE_PING) {
        if (downlink_msg.payload.ping.type == DOWN_PING_TYPE_DISCONNECT) {
          device_connected = false;
        }
      } else {
        function_t* func;
        SYS_SLIST_FOR_EACH_CONTAINER(&capabilities_ptr->functions, func, node) {
          if (func->id == downlink_msg.payload.call.function_id) {
            func->call_function(&downlink_msg.payload.call.value);
          }
        }
      }
    }
  }
}

extern void uplink_communication_thread() {
  while (1) {
    uplink_t uplink;
    /*
     * 1. Check if there are any incoming packets from physical LoRA
     * (K_NO_WAIT) 1.a) If yes convert them to downlink_t and put them into
     * the downlink_q (which is later handled by the downlink_thread)
     * 2. Take messages from uplink_q, encode them and send them via LoRa
     * (K_FOREVER)
     *  - this way the thread wakes up each time a sensor reports or a
     *    function sends a ping after timer ends.
     */
    k_msgq_get(&uplink_q, &uplink, K_FOREVER);
    char buf[1024];
    size_t bytes_written;
    encode_uplink(buf, sizeof(buf), &uplink, &bytes_written);
    lora_data_t outgoing_data = {.data = buf, .data_size = bytes_written};
    LOG_DBG("Sending uplink packet...");
  }
}

extern void downlink_communication_thread() {
  while (1) {
    downlink_t downlink;
    /*
     * 1. Check if there are any incoming packets from physical LoRA
     * (K_NO_WAIT) 1.a) If yes convert them to downlink_t and put them into
     * the downlink_q (which is later handled by the downlink_thread)
     * 2. Take messages from uplink_q, encode them and send them via LoRa
     * (K_FOREVER)
     *  - this way the thread wakes up each time a sensor reports or a
     *    function sends a ping after timer ends.
     */
    lora_data_t incoming_data;
    k_msgq_get(&lora_q, &incoming_data, K_FOREVER);
    LOG_DBG("Received downlink packet...");
    decode_downlink(&downlink, incoming_data.data, incoming_data.data_size);
    k_msgq_put(&downlink_q, &downlink, K_FOREVER);
  }
}

#define UPLINK_THREAD_SIZE 500
#define DOWNLINK_THREAD_SIZE 500
#define UPLINK_COMMUNICATION_THREAD_SIZE 500
#define DOWNLINK_COMMUNICATION_THREAD_SIZE 500

K_THREAD_STACK_DEFINE(uplink_stack_area, UPLINK_THREAD_SIZE);
K_THREAD_STACK_DEFINE(downlink_stack_area, DOWNLINK_THREAD_SIZE);
K_THREAD_STACK_DEFINE(uplink_communication_stack_area,
                      UPLINK_COMMUNICATION_THREAD_SIZE);
K_THREAD_STACK_DEFINE(downlink_communication_stack_area,
                      DOWNLINK_COMMUNICATION_THREAD_SIZE);

result_t loris_main_loop(capabilities_t* capabilities) {
  char downlink_q_buf[1024];
  char uplink_q_buf[1024];
  char lora_q_buf[1024];

  struct k_thread uplink_tdata;
  struct k_thread downlink_tdata;
  struct k_thread uplink_communication_tdata;
  struct k_thread downlink_communication_tdata;

  k_tid_t uplink_tid =
      k_thread_create(&uplink_tdata, uplink_stack_area,
                      K_THREAD_STACK_SIZEOF(uplink_stack_area), uplink_thread,
                      &capabilities, NULL, NULL, 3, 0, K_FOREVER);
  k_tid_t downlink_tid = k_thread_create(
      &downlink_tdata, downlink_stack_area,
      K_THREAD_STACK_SIZEOF(downlink_stack_area), downlink_thread,
      &capabilities, NULL, NULL, 2, 0, K_FOREVER);
  k_tid_t uplink_communication_tid = k_thread_create(
      &uplink_communication_tdata, uplink_communication_stack_area,
      K_THREAD_STACK_SIZEOF(uplink_communication_stack_area),
      uplink_communication_thread, NULL, NULL, NULL, 3, 0, K_FOREVER);

  k_tid_t downlink_communication_tid = k_thread_create(
      &downlink_communication_tdata, downlink_communication_stack_area,
      K_THREAD_STACK_SIZEOF(downlink_communication_stack_area),
      downlink_communication_thread, NULL, NULL, NULL, 2, 0, K_FOREVER);

  k_msgq_init(&downlink_q, downlink_q_buf, sizeof(downlink_t), 10);
  k_msgq_init(&uplink_q, uplink_q_buf, sizeof(uplink_t), 10);
  k_msgq_init(&lora_q, lora_q_buf, sizeof(lora_data_t), 10);

  k_thread_start(uplink_tid);
  k_thread_start(downlink_tid);
  k_thread_start(uplink_communication_tid);
  k_thread_start(downlink_communication_tid);

  return OK;
}
