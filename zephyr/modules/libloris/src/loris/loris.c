/*
 * Copyright (c) 2026 Jan Kubów
 *
 * SPDX-License-Identifier: MIT
 */

#include "loris.h"
#include "loris/codec.h"
#include "loris/lorawan.h"
#include "loris/models/call.h"
#include "loris/models/capabilities.h"
#include "loris/models/downlink.h"
#include "loris/models/report.h"
#include "loris/models/sensor.h"
#include "loris/models/uplink.h"

#include <stdint.h>
#include <zephyr/kernel.h>
#include <zephyr/logging/log.h>
#include <zephyr/lorawan/lorawan.h>
#include <zephyr/sleep.h>
#include <zephyr/sys/min_heap.h>
#include <zephyr/sys/slist.h>

#define HEAP_CAPACITY CONFIG_LORIS_SENSORS_AMOUNT

LOG_MODULE_REGISTER(loris, LOG_LEVEL_DBG);

/* Holds inbound data */
typedef struct lr_data {
  const char* data;
  int data_size;
} lr_data_t;

/* Holds sensor with senor wake-up time */
typedef struct sensor_time {
  sensor_t* sensor;
  int64_t timestamp;
} sensor_time_t;

struct k_msgq dl_q;
struct k_msgq ul_q;
struct k_msgq lora_q;

struct lorawan_downlink_cb dl_cb;

bool device_connected = false;

/* Comparison function for min_heap */
static int compare(const void* a, const void* b) {
  const sensor_time_t* st_a = a;
  const sensor_time_t* st_b = b;

  return st_a->timestamp - st_b->timestamp;
}

MIN_HEAP_DEFINE_STATIC(
    sensors_heap,
    HEAP_CAPACITY,
    sizeof(sensor_time_t),
    __alignof__(sensor_time_t),
    compare
);

extern void ul_thread(void* capabilities, void* unused1, void* unused2) {
  /* 1. Check if the device is connected
   *  1.a) If it's connected, proceed with the usual sensor reporting
   *  1.b) If it's disconnected then we must establish a connection:
   *    - add a payload with capabilities to the ul_q
   *    - wait for CONFIG_RECONNECT_INTERVAL (It could be improved later on but
   *      for now it'll be simple enough and work).
   */
  if (capabilities == NULL) {
    return;
  }
  capabilities_t* capabilities_ptr = capabilities;
  sensor_t* sensor;

  SYS_SLIST_FOR_EACH_CONTAINER(&capabilities_ptr->sensors, sensor, node) {
    sensor_time_t st = { sensor, 0 };
    min_heap_push(&sensors_heap, &st);
  }

  sensor_time_t st;

  LOG_DBG("Uplink thread started successfully!");
  while (1) {
    if (device_connected) {
      LOG_DBG("Woke up at: %d\n", (int)k_uptime_get());

      min_heap_pop(&sensors_heap, &st);
      st.timestamp += (st.sensor->wake_interval_sec * 1000);
      if (st.timestamp < k_uptime_get()) {
        st.timestamp += k_uptime_get();
      }
      min_heap_push(&sensors_heap, &st);

      report_t report = st.sensor->read_func(st.sensor);
      switch (report.which_value) {
      case REPORT_VALUE_INT:
        LOG_INF(
            "Report from sensor %d, reads: %d",
            report.sensor_id,
            report.value.int_val
        );
        break;
      case REPORT_VALUE_BOOL:
        LOG_INF(
            "Report from sensor %d, reads: %d",
            report.sensor_id,
            report.value.bool_val
        );
        break;
      case REPORT_VALUE_FLOAT:
        LOG_INF(
            "Report from sensor %d, reads: %f",
            report.sensor_id,
            (double)report.value.float_val
        );
        break;
      case REPORT_VALUE_STRING:
        LOG_INF(
            "Report from sensor %d, reads: %s",
            report.sensor_id,
            report.value.str_val.ptr
        );
        break;
      default:
        LOG_INF("Report from sensor %d, unspecified value", st.sensor->id);
        break;
      }

      uplink_t report_ul = { .payload.report = report,
                             .payload_type = UPLINK_PAYLOAD_TYPE_REPORT };
      k_msgq_put(&ul_q, &report_ul, K_NO_WAIT);

      LOG_DBG("Going to sleep at: %d\n", (int)k_uptime_get());
      int64_t sleep_time =
          ((sensor_time_t*)min_heap_peek(&sensors_heap))->timestamp -
          k_uptime_get();
      if (sleep_time < 1) {
        sleep_time = 1;
      }

      k_msleep(sleep_time);
    } else {
      LOG_DBG("Trying to connect to loris client");
      uplink_t capabilities_ul = { .payload.capabilities = *capabilities_ptr,
                                   .payload_type =
                                       UPLINK_PAYLOAD_TYPE_CAPABILITIES };
      uplink_t hello_ul = { .payload.ping.type = UP_PING_TYPE_HELLO,
                            .payload_type = UPLINK_PAYLOAD_TYPE_PING };

      k_msgq_put(&ul_q, &capabilities_ul, K_NO_WAIT);
      k_msleep(1000);
      k_msgq_put(&ul_q, &hello_ul, K_NO_WAIT);
      k_msleep(CONFIG_LORIS_RECONNECT_INTERVAL * 1000);
    }
  }
}

extern void dl_thread(void* capabilities, void* unused1, void* unused2) {
  /*
   * 1. Wait for a message from dl_q
   *  1.a) If a message is a function - run it
   *  1.b) Else it must be a ping - set device_connected flag accordingly
   */
  if (capabilities == NULL) {
    return;
  }
  capabilities_t* capabilities_ptr = capabilities;
  downlink_t dl_msg;

  LOG_DBG("Downlink thread started successfully!");
  while (1) {
    k_msgq_get(&dl_q, &dl_msg, K_FOREVER);
    if (!device_connected) {
      if (dl_msg.payload_type != DOWNLINK_PAYLOAD_TYPE_PING) {
        continue;
      }
      if (dl_msg.payload.ping.type != DOWN_PING_TYPE_HELLO) {
        continue;
      }
      LOG_DBG("Device connected!");
      device_connected = true;
    } else {
      if (dl_msg.payload_type == DOWNLINK_PAYLOAD_TYPE_PING) {
        if (dl_msg.payload.ping.type == DOWN_PING_TYPE_DISCONNECT) {
          LOG_DBG("Disconnecting device...");
          device_connected = false;
        }
      } else {
        function_t* func;
        SYS_SLIST_FOR_EACH_CONTAINER(&capabilities_ptr->functions, func, node) {
          if (func->id == dl_msg.payload.call.function_id) {
            LOG_DBG(
                "Calling function id: %i, name: %s",
                dl_msg.payload.call.function_id,
                func->name.ptr
            );
            func->call_function(&dl_msg.payload.call.value);
          }
        }
      }
    }
  }
}

/* Takes the outbound message, encodes it and sends it via LoRaWAN */
extern void ul_comm_thread() {
  LOG_DBG("Uplink communication thread started successfully!");
  while (1) {
    uplink_t ul;
    k_msgq_get(&ul_q, &ul, K_FOREVER);

    char buf[1024];
    size_t bytes_written;
    encode_uplink(buf, sizeof(buf), &ul, &bytes_written);

    lr_lw_result_t ret =
        lr_lw_send(1, buf, bytes_written, LORAWAN_MSG_CONFIRMED);

    if (ret != LR_LW_OK) {
      LOG_ERR("Sending message failed!");
    }
  }
}

/* Takes the inbound message, decodes it and puts it into the downlink_queue */
extern void dl_comm_thread() {
  LOG_DBG("Downlink communication thread started successfully!");
  while (1) {
    downlink_t dl;
    lr_data_t incoming_data;
    k_msgq_get(&lora_q, &incoming_data, K_FOREVER);

    LOG_DBG("Received downlink message");
    decode_downlink(&dl, incoming_data.data, incoming_data.data_size);
    k_msgq_put(&dl_q, &dl, K_FOREVER);
  }
}

/* Puts the message into the inbound lora queue */
static void dl_callback(
    uint8_t port,
    uint8_t flags,
    int16_t rssi,
    int8_t snr,
    uint8_t len,
    const uint8_t* hex_data
) {
  if (hex_data) {
    lr_data_t data = { .data = hex_data, .data_size = len };
    k_msgq_put(&lora_q, &data, K_FOREVER);
  }
}

// TODO: These could be optimized in the future, but this works for now
// Could be calculated from the ul/dl queue size (param of KCONFIG - also todo)
#define UL_THREAD_SIZE 4096
#define DL_THREAD_SIZE 4096
#define UL_COMM_THREAD_SIZE 4096
#define DL_COMM_THREAD_SIZE 4096

#define DL_Q_SIZE CONFIG_LORIS_DOWNLINK_QUEUE_SIZE
#define UL_Q_SIZE CONFIG_LORIS_UPLINK_QUEUE_SIZE

K_THREAD_STACK_DEFINE(ul_stack_area, UL_THREAD_SIZE);
K_THREAD_STACK_DEFINE(dl_stack_area, DL_THREAD_SIZE);
K_THREAD_STACK_DEFINE(ul_comm_stack_area, UL_COMM_THREAD_SIZE);
K_THREAD_STACK_DEFINE(dl_comm_stack_area, DL_COMM_THREAD_SIZE);

void lr_start(capabilities_t* capabilities) {
  lr_lw_register_downlink_callback(&dl_cb, &dl_callback);

  char dl_q_buf[sizeof(downlink_t) * DL_Q_SIZE];
  char ul_q_buf[sizeof(uplink_t) * UL_Q_SIZE];
  char lora_q_buf[sizeof(lr_data_t) * DL_Q_SIZE];

  struct k_thread ul_tdata;
  struct k_thread dl_tdata;
  struct k_thread ul_comm_tdata;
  struct k_thread dl_comm_tdata;

  k_tid_t ul_tid = k_thread_create(
      &ul_tdata,
      ul_stack_area,
      K_THREAD_STACK_SIZEOF(ul_stack_area),
      ul_thread,
      capabilities,
      NULL,
      NULL,
      3,
      0,
      K_FOREVER
  );
  k_tid_t dl_tid = k_thread_create(
      &dl_tdata,
      dl_stack_area,
      K_THREAD_STACK_SIZEOF(dl_stack_area),
      dl_thread,
      capabilities,
      NULL,
      NULL,
      2,
      0,
      K_FOREVER
  );
  k_tid_t ul_comm_tid = k_thread_create(
      &ul_comm_tdata,
      ul_comm_stack_area,
      K_THREAD_STACK_SIZEOF(ul_comm_stack_area),
      ul_comm_thread,
      NULL,
      NULL,
      NULL,
      3,
      0,
      K_FOREVER
  );

  k_tid_t dl_comm_tid = k_thread_create(
      &dl_comm_tdata,
      dl_comm_stack_area,
      K_THREAD_STACK_SIZEOF(dl_comm_stack_area),
      dl_comm_thread,
      NULL,
      NULL,
      NULL,
      2,
      0,
      K_FOREVER
  );

  k_msgq_init(&dl_q, dl_q_buf, sizeof(downlink_t), DL_Q_SIZE);
  k_msgq_init(&ul_q, ul_q_buf, sizeof(uplink_t), UL_Q_SIZE);
  k_msgq_init(&lora_q, lora_q_buf, sizeof(lr_data_t), DL_Q_SIZE);

  k_thread_start(ul_tid);
  k_thread_start(dl_tid);
  k_thread_start(ul_comm_tid);
  k_thread_start(dl_comm_tid);

  // In order to never end - threads do all the job
  while (1) {
    k_sleep(K_FOREVER);
  }
}
