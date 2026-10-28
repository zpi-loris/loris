#include "models/capabilities.h"
#include "loris/models/devclass.h"
#include "str.h"

capabilities_t create_capabilities(const char *device_name,
                                   device_class_t device_class,
                                   sys_slist_t *sensor_list,
                                   sys_slist_t *function_list) {
  str_t device_name_str;
  str_from_static(&device_name_str, device_name);

  capabilities_t cap = {.device_name = device_name_str,
                        .device_class = device_class,
                        .functions = function_list,
                        .sensors = sensor_list};

  return cap;
}
