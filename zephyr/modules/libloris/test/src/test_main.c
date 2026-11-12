#include <assert.h>
#include <zephyr/ztest.h>

#include "loris/models/capabilities.h"
#include "loris/models/report.h"
#include "loris/models/sensor.h"
#include "zephyr/sys/min_heap.h"
#include "zephyr/ztest_assert.h"
#include "zephyr/ztest_test.h"

ZTEST_SUITE(runtime, NULL, NULL, NULL, NULL, NULL);

capabilities_t caps;

report_t report_func(struct sensor* t);
result_t do_sth_int(void* arg);
result_t do_sth_float(void* arg);

/*
ZTEST(capabilities, test_sensor_runtime_submodule) {
  capabilities_init(&caps, "capabilities_sensors", DEVICE_CLASS_A);

  zassert_equal(
      capabilities_add_sensor(&caps, "test_name 1", "Measures temperature",
                              VALUE_TYPE_TEMPERATURE, "C", &report_func, 6),
      OK);
  zassert_equal(
      capabilities_add_sensor(&caps, "test_name 2", "Measures temperature",
                              VALUE_TYPE_TEMPERATURE, "F", &report_func, 3),
      OK);

  run_sensors(&caps);
}
*/
