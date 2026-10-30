#include "capabilities.h"
#include "../str.h"
#include "loris/models/devclass.h"
#include "loris/models/function.h"
#include "loris/models/sensor.h"
#include "loris/result.h"
#include "zephyr/sys/slist.h"

static int32_t sensor_id = 0;
static int32_t func_id = 0;
static sensor_t sensor_buffer[CONFIG_LORIS_SENSORS_AMOUNT];
static function_t func_buffer[CONFIG_LORIS_FUNCS_AMOUNT];

result_t create_capabilities(capabilities_t *out, const char *device_name,
                             device_class_t device_class,
                             sys_slist_t *sensor_list,
                             sys_slist_t *function_list) {

  if (out == NULL) {
    return ERROR;
  }

  str_t device_name_str;
  if (str_from_static(&device_name_str, device_name) == ERROR) {
    return ERROR;
  }

  *out = (capabilities_t){.device_name = device_name_str,
                          .device_class = device_class,
                          .functions = function_list,
                          .sensors = sensor_list};

  return OK;
}

result_t add_sensor(sys_slist_t *sensor_list, const char *name,
                    const char *description, value_type_t value_type,
                    const char *unit, report_t (*report_func)(struct sensor *),
                    int32_t wake_interval_sec) {
  if (sensor_list == NULL || report_func == NULL) {
    return ERROR;
  }

  str_t s_name;
  str_t s_desc;
  str_t s_unit = LORIS_STR_NONE;
  bool has_unit = false;

  if (str_from_static(&s_name, name) == ERROR) {
    return ERROR;
  }
  if (str_from_static(&s_desc, description) == ERROR) {
    return ERROR;
  }
  if (unit != NULL) {
    if (str_from_static(&s_unit, unit) == ERROR) {
      return ERROR;
    }
    has_unit = true;
  }

  sensor_buffer[sensor_id] = (sensor_t){.id = sensor_id,
                                        .name = s_name,
                                        .description = s_desc,
                                        .value_type = value_type,
                                        .has_unit = has_unit,
                                        .unit = s_unit,
                                        .read_func = report_func,
                                        .wake_interval_sec = wake_interval_sec,
                                        .node = {.next = NULL}};

  sys_slist_append(sensor_list, &sensor_buffer[sensor_id].node);

  sensor_id++;
  return OK;
}

result_t add_function(sys_slist_t *function_list, char *name, char *description,
                      func_arg_type_t func_arg_type,
                      result_t (*call_func)(void *),
                      int32_t wake_interval_sec) {
  if (function_list == NULL || call_func == NULL) {
    return ERROR;
  }

  str_t s_name;
  str_t s_desc;

  if (str_from_static(&s_name, name) == ERROR) {
    return ERROR;
  }
  if (str_from_static(&s_desc, description) == ERROR) {
    return ERROR;
  }

  func_buffer[func_id] = (function_t){.id = sensor_id,
                                      .name = s_name,
                                      .description = s_desc,
                                      .arg_type = func_arg_type,
                                      .call_function = call_func,
                                      .wake_interval_sec = wake_interval_sec,
                                      .node = {.next = NULL}};

  sys_slist_append(function_list, &func_buffer[func_id].node);

  func_id++;
  return OK;
}
