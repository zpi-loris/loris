#include <assert.h>
#include <zephyr/ztest.h>

#include "loris/models/capabilities.h"
#include "loris/models/report.h"
#include "loris/models/sensor.h"
#include "loris/str.h"
#include "zephyr/sys/slist.h"
#include "zephyr/ztest_assert.h"
#include "zephyr/ztest_test.h"

ZTEST_SUITE(capabilities, NULL, NULL, NULL, NULL, NULL);

report_t report_func(struct sensor *t) {
  report_t rep = {
      .sensor_id = t->id, .value = {2}, .which_value = REPORT_VALUE_INT};
  return rep;
};

sensor_t *get_sensor_at_idx(sys_slist_t *sensor_list, int idx) {
  sensor_t *sensor;

  int i = 0;
  SYS_SLIST_FOR_EACH_CONTAINER(sensor_list, sensor, node) {
    if (i == idx) {
      return sensor;
    }
    i++;
  }
  return NULL;
}

sys_slist_t sensor_list;

ZTEST(capabilities, test_add_sensors_to_the_list) {
  sys_slist_init(&sensor_list);

  zassert_equal(add_sensor(&sensor_list, "test_name 1", "Measures temperature",
                           VALUE_TYPE_TEMPERATURE, "C", &report_func, 60),
                OK);
  zassert_equal(add_sensor(&sensor_list, "test_name 2", "Measures temperature",
                           VALUE_TYPE_TEMPERATURE, "F", &report_func, 30),
                OK);

  sensor_t *fst_sensor = get_sensor_at_idx(&sensor_list, 0);
  sensor_t *snd_sensor = get_sensor_at_idx(&sensor_list, 1);

  str_t str1;
  str_t str2;
  str_from_static(&str1, "test_name 1");
  str_from_static(&str2, "test_name 2");

  if (fst_sensor != NULL) {
    zassert_str_equal(fst_sensor->name.ptr, str1.ptr);
  }
  if (snd_sensor != NULL) {
    zassert_str_equal(snd_sensor->name.ptr, str2.ptr);
  }
  zassert_not_equal(sys_slist_len(&sensor_list), 0);
  zassert_equal(sys_slist_len(&sensor_list), 2);
  if (fst_sensor != NULL && snd_sensor != NULL) {
    zassert_equal(fst_sensor->node.next, &snd_sensor->node);
  }
}

ZTEST(capabilities, test_check_read_function_from_capabilities) {
  sensor_t *fst_sensor = get_sensor_at_idx(&sensor_list, 0);
  sensor_t *snd_sensor = get_sensor_at_idx(&sensor_list, 1);

  report_t fst_report = fst_sensor->read_func(fst_sensor);
  report_t snd_report = snd_sensor->read_func(snd_sensor);

  zassert_equal(fst_report.value.int_val, snd_report.value.int_val);
  zassert_equal(fst_report.value.int_val, 2);
}
