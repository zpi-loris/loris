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

sensor_t add_sensor(sys_slist_t *sensor_list, char *name, char *description,
                    value_type_t value_type, char *unit,
                    read_func read_function, int32_t wake_interval_sec);

function_t add_function(sys_slist_t *function_list, char *name,
                        char *description, func_arg_type_t,
                        void (*call_function)(), int32_t wake_interval_sec);

capabilities_t create_capabilities(char *device_name, sys_slist_t *sensor_list,
                                   sys_slist_t *function_list);
#endif
