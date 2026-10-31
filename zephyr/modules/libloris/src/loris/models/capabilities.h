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

/**
 * @brief Initialize capabilities struct that describes the device
 *
 * @param out A pointer to the capabilities_t
 * @param device_name Name of the device
 * @param device_class LoRaWAN device class
 *
 * @return result_t enum
 */
result_t capabilities_init(capabilities_t* out, const char* device_name,
                           device_class_t device_class);

/**
 * @brief Adds a sensor to the capabilities of the device
 *
 * @param capabilities - A pointer to the capabilities_t
 * @param name - Name of the sensor to be displayed on the dashboard
 * @param description - Describes the sensor on the dashboard
 * @param value_type - Returned value type in report
 * @param unit - Optional unit of the measurement
 * @param report_func - A pointer to the function that takes input from the
 * sensor and outputs it in a result_t
 * @param interval_sec - How frequent is the sensor polled in seconds
 *
 * @return result_t enum
 */
result_t add_sensor(capabilities_t* capabilities, const char* name,
                    const char* description, value_type_t value_type,
                    const char* unit, report_t (*report_func)(struct sensor*),
                    int32_t interval_sec);

/**
 * @brief Adds a function to the capabilities of the device
 *
 * @param capabilities A pointer to the capabilities_t
 * @param name Name of the function to be displayed on the dashboard
 * @param description Describes the function on the dashboard
 * @param func_arg_type Type of the argument
 * @param call_function A pointer to the function
 * @param wake_interval_sec When to next wake up after running this function
 *
 * @return result_t enum
 */
result_t add_function(capabilities_t* capabilities, char* name,
                      char* description, func_arg_type_t func_arg_type,
                      result_t (*call_function)(void*),
                      int32_t wake_interval_sec);

#endif
