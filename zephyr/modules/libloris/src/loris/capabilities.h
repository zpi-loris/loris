#ifndef LORIS_CAPABILITIES_H
#define LORIS_CAPABILITIES_H

#include "models.h"
#include <zephyr/sys/slist.h>

sensor_t add_sensor(sys_slist_t *sensor_list, char *name, char *description,
                    value_type_t value_type, char *unit,
                    report_t (*report_func)(struct sensor),
                    int32_t wake_interval_sec);

function_t add_function(sys_slist_t *function_list, char *name,
                        char *description, func_arg_type_t,
                        void (*call_function)(), int32_t wake_interval_sec);

capabilities_t create_capabilities(char *device_name, sys_slist_t *sensor_list,
                                   sys_slist_t *function_list);

#endif
