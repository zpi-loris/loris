#ifndef LORIS_CAPABILITIES_H
#define LORIS_CAPABILITIES_H

#include <inttypes.h>
#include <stdbool.h>
#include <zephyr/sys/slist.h>

#include "loris/models/devclass.h"
#include "loris/models/function.h"
#include "loris/models/sensor.h"
#include "loris/str.h"

typedef struct capabilities {
  str_t device_name;
  device_class_t device_class;

  // NOTE: sys_slist functions are not thread safe
  sys_slist_t sensors;
  // NOTE: sys_slist functions are not thread safe
  sys_slist_t functions;
} capabilities_t;

result_t add_sensor(sys_slist_t *sensor_list, const char *name,
                    const char *description, value_type_t value_type,
                    const char *unit, report_t (*report_func)(struct sensor *),
                    int32_t wake_interval_sec);

result_t add_function(sys_slist_t *function_list, char *name, char *description,
                      func_arg_type_t, result_t (*call_function)(void *),
                      int32_t wake_interval_sec);

result_t create_capabilities(capabilities_t *out, const char *device_name,
                             device_class_t device_class,
                             sys_slist_t *sensor_list,
                             sys_slist_t *function_list);
#endif
