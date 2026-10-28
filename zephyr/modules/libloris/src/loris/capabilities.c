#include "models/capabilities.h"
#include "loris/models/devclass.h"
#include "loris/result.h"
#include "str.h"

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
