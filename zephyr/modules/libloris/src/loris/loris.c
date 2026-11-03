#include "loris.h"
#include "loris/codec.h"
#include "loris/models/call.h"
#include "loris/models/capabilities.h"
#include "loris/models/downlink.h"
#include "loris/models/report.h"
#include "loris/models/sensor.h"
#include "loris/models/uplink.h"
#include "loris/result.h"

#include <stdint.h>
#include <zephyr/kernel.h>
#include <zephyr/logging/log.h>
#include <zephyr/sleep.h>
#include <zephyr/sys/min_heap.h>
#include <zephyr/sys/slist.h>

#define HEAP_CAPACITY CONFIG_LORIS_SENSORS_AMOUNT

LOG_MODULE_REGISTER(runtime);

typedef struct sensor_time {
  sensor_t* sensor;
  int64_t timestamp;
} sensor_time_t;

static int compare(const void* a, const void* b) {
  const sensor_time_t* st_a = a;
  const sensor_time_t* st_b = b;

  return st_a->timestamp - st_b->timestamp;
}

MIN_HEAP_DEFINE_STATIC(sensors_heap, HEAP_CAPACITY, sizeof(sensor_time_t),
                       __alignof__(sensor_time_t), compare);

static result_t sensors_thread(capabilities_t* capabilities) {
  if (capabilities == NULL) {
    return ERROR;
  }

  sensor_t* sensor;

  SYS_SLIST_FOR_EACH_CONTAINER(&capabilities->sensors, sensor, node) {
    sensor_time_t st = {sensor, 0};
    min_heap_push(&sensors_heap, &st);
  }

  sensor_time_t st;
  while (1) {
    LOG_DBG("Woke up at: %d\n", (int)k_uptime_get());

    min_heap_pop(&sensors_heap, &st);
    st.timestamp += (st.sensor->wake_interval_sec * 1000);
    min_heap_push(&sensors_heap, &st);

    report_t report = st.sensor->read_func(st.sensor);
    switch (report.which_value) {
    case REPORT_VALUE_INT:
      LOG_DBG("Report from sensor %d, reads: %d", st.sensor->id,
              report.value.int_val);
      break;
    case REPORT_VALUE_BOOL:
      LOG_DBG("Report from sensor %d, reads: %d", st.sensor->id,
              report.value.bool_val);
      break;
    case REPORT_VALUE_FLOAT:
      LOG_DBG("Report from sensor %d, reads: %f", st.sensor->id,
              (double)report.value.float_val);
      break;
    case REPORT_VALUE_STRING:
      LOG_DBG("Report from sensor %d, reads: %s", st.sensor->id,
              report.value.str_val);
      break;
    default:
      LOG_DBG("Report from sensor %d, unspecified value", st.sensor->id);
      break;
    }

    uplink_t ul = {.payload.report = report,
                   .payload_type = UPLINK_PAYLOAD_TYPE_REPORT};

    char buf[1024];
    size_t bytes_written = 0;
    encode_uplink(buf, sizeof(buf), &ul, &bytes_written);

    LOG_DBG("Going to sleep at: %d\n", (int)k_uptime_get());
    k_msleep(((sensor_time_t*)min_heap_peek(&sensors_heap))->timestamp -
             k_uptime_get());
  }
  return OK;
}

char uplink_q_buf[10 * sizeof(uplink_t)];
char downlink_q_buf[10 * sizeof(downlink_t)];
struct k_msgq downlink_q;

static result_t downlink_thread(capabilities_t* capabilities) {
  if (capabilities == NULL) {
    return ERROR;
  }

  k_msgq_init(&downlink_q, downlink_q_buf, sizeof(downlink_t), 10);
  downlink_t downlink_msg;
  while (1) {
    /* get a data item
     * NOTE: this is a thread blocking call, meaning if no msg in queue wait
     * thread
     * */
    k_msgq_get(&downlink_q, &downlink_msg, K_FOREVER);

    /* process data item */
    if (downlink_msg.payload_type == DOWNLINK_PAYLOAD_TYPE_CALL) {
      function_t* func;
      SYS_SLIST_FOR_EACH_CONTAINER(&capabilities->functions, func, node) {
        if (func->id == downlink_msg.payload.call.function_id) {
          func->call_function(&downlink_msg.payload.call.value);
        }
      }
    } else if (downlink_msg.payload_type == DOWNLINK_PAYLOAD_TYPE_PING) {
      // do sth
    }
  }
  return OK;
}

result_t loris_main_loop(capabilities_t* capabilties) {
  // TODO: make it a thread
  if (capabilties->sensors.head != NULL) {
    sensors_thread(capabilties);
  }

  // TODO: make it a thread
  return OK;
}
