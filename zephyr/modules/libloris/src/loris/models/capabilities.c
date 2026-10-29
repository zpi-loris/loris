#include "capabilities.h"
#include "../str.h"
#include "loris/models/devclass.h"
#include "loris/models/function.h"
#include "loris/models/sensor.h"
#include "loris/result.h"
#include "zephyr/sys/slist.h"

static int32_t sensor_id = 0;
static int32_t func_id = 0;
static sensor_node_t sensor_nodes[CONFIG_LORIS_SENSORS_AMOUNT];
static sys_snode_t sensor_s_nodes[CONFIG_LORIS_SENSORS_AMOUNT];
static function_node_t func_nodes[CONFIG_LORIS_FUNCS_AMOUNT];
static sys_snode_t func_s_nodes[CONFIG_LORIS_FUNCS_AMOUNT];

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
    if (str_from_static(&s_desc, description) == ERROR) {
      return ERROR;
    }
    has_unit = true;
  }

  sensor_t sensor = {.id = sensor_id,
                     .name = s_name,
                     .description = s_desc,
                     .value_type = value_type,
                     .has_unit = has_unit,
                     .unit = s_unit,
                     .read_func = report_func,
                     .wake_interval_sec = wake_interval_sec};

  sys_snode_t *prev_node = sys_slist_peek_tail(sensor_list);
  if (prev_node != NULL) {
    prev_node->next = &sensor_s_nodes[sensor_id];
  }

  // TODO: figure out how to save s_node and sensor_node so they do not get
  // removed from stack
  sensor_node_t sensor_node = {.sensor = sensor,
                               .node = sensor_s_nodes[sensor_id]};
  sensor_nodes[sensor_id] = sensor_node;

  sys_slist_append(sensor_list, &sensor_s_nodes[sensor_id]);

  sensor_id++;
  return OK;
};

result_t add_function(sys_slist_t *function_list, char *name, char *description,
                      func_arg_type_t func_arg_type, call_func_u *call_func,
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

  function_t function = {.id = sensor_id,
                         .name = s_name,
                         .description = s_desc,
                         .call_function = *call_func,
                         .wake_interval_sec = wake_interval_sec};

  sys_snode_t *prev_node = sys_slist_peek_tail(function_list);
  if (prev_node != NULL) {
    prev_node->next = &func_s_nodes[func_id];
  }

  function_node_t function_node = {.function = function,
                                   .node = func_s_nodes[func_id]};
  func_nodes[func_id] = function_node;

  sys_slist_append(function_list, &func_s_nodes[func_id]);

  func_id++;
  return OK;
}
