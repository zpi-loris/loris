#include <zephyr/ztest.h>

#include "loris/models/capabilities.h"
#include "loris/models/report.h"
#include "loris/models/sensor.h"
#include "loris/str.h"
#include "zephyr/sys/slist.h"

ZTEST_SUITE(capabilities, NULL, NULL, NULL, NULL, NULL);

report_t report_func(struct sensor *t) {
  report_t rep = {
      .sensor_id = t->id, .value = {2}, .which_value = REPORT_VALUE_INT};
  return rep;
};

ZTEST(capabilities, test_adds_sensor_to_the_list) {

  sys_slist_t sensor_list;
  printf("hello1\n");
  sys_slist_init(&sensor_list);
  printf("hello2\n");

  zassert_equal(add_sensor(&sensor_list, "test_name 1", "Measures temperature",
                           VALUE_TYPE_TEMPERATURE, "C", &report_func, 60),
                OK);
  zassert_equal(add_sensor(&sensor_list, "test_name 2", "Measures temperature",
                           VALUE_TYPE_TEMPERATURE, "F", &report_func, 30),
                OK);

  printf("hello3\n");
  sensor_t *sensor;
  sensor_t *found_sensor;

  int32_t idx = 0;

  SYS_SLIST_FOR_EACH_CONTAINER(&sensor_list, sensor, node) {
    printf("hello from loop\n");
    printf("Found value: %s\n", sensor->name.ptr);
    if (idx == 0) {
      found_sensor = sensor;
    }
  }

  str_t str;
  str_from_static(&str, "test_name 1");
  // zassert_equal(*(found_sensor->sensor.name.ptr), *(str.ptr));
}
