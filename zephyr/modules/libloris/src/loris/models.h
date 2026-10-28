#ifndef LORIS_MODELS_H
#define LORIS_MODELS_H

#include <inttypes.h>
#include <stdbool.h>
#include <zephyr/sys/slist.h>

#include "loris/str.h"

/*
 * Argument type used in capabilities.function
 */
typedef enum func_arg_type {
  FUNC_ARG_TYPE_UNSPECIFIED = 0,
  FUNC_ARG_TYPE_VOID = 1,
  FUNC_ARG_TYPE_INT = 2,
  FUNC_ARG_TYPE_FLOAT = 3,
  FUNC_ARG_TYPE_STRING = 4,
  FUNC_ARG_TYPE_BOOL = 5
} func_arg_type_t;

/*
 * Value type used in capabilities.sensor
 */
typedef enum value_type {
  /* General */
  VALUE_TYPE_UNSPECIFIED = 0,
  VALUE_TYPE_NUMERIC = 1,
  VALUE_TYPE_TEXT = 2,
  VALUE_TYPE_BOOL = 3,
  /* Common */
  VALUE_TYPE_TEMPERATURE = 4,
  VALUE_TYPE_PRESSURE = 5,
  VALUE_TYPE_FREQUENCY = 6,
  VALUE_TYPE_MOISTURE = 7,
  VALUE_TYPE_UPTIME = 8,
  VALUE_TYPE_AREA = 9,
  VALUE_TYPE_DISTANCE = 10,
  VALUE_TYPE_DURATION = 11,
  VALUE_TYPE_ILLUMINANCE = 12,
  VALUE_TYPE_WATER = 13,
  VALUE_TYPE_WEIGHT = 14,
  /* Air index */
  VALUE_TYPE_AQI = 15,
  VALUE_TYPE_CO2 = 16,
  VALUE_TYPE_PM1 = 17,
  VALUE_TYPE_PM25 = 18,
  VALUE_TYPE_PM4 = 19,
  VALUE_TYPE_PM10 = 20,
  VALUE_TYPE_HUMIDITY = 21,
  /* Wind */
  VALUE_TYPE_WIND_SPEED = 22,
  /* Electricity */
  VALUE_TYPE_BATTERY = 23,
  VALUE_TYPE_ENERGY = 24,
  VALUE_TYPE_VOLTAGE = 25,
  VALUE_TYPE_CURRENT = 26,
  /* Transmission */
  VALUE_TYPE_DATA_RATE = 27,
  VALUE_TYPE_DATA_SIZE = 28,
  VALUE_TYPE_SIGNAL_STRENGTH = 29,
  /* Earth */
  VALUE_TYPE_PH = 30,
  VALUE_TYPE_POTASSIUM = 31,
  VALUE_TYPE_PHOSPHORUS = 32,
  VALUE_TYPE_NITROGEN = 33
} value_type_t;

/*
 * Specifies device class according to LoRaWAN standard
 */
typedef enum device_class {
  DEVICE_CLASS_A = 0,
  DEVICE_CLASS_B = 1,
  DEVICE_CLASS_C = 2
} device_class_t;

/*
 * Callback to a function that reads data from a sensor
 */
typedef union {
  int (*int_read_func)();
  float (*float_read_func)();
  bool (*bool_read_func)();
  str_t (*str_read_func)();
} read_func;

typedef struct sensor {
  int32_t id;
  str_t name;
  str_t description;
  value_type_t value_type;

  // Determines whether 'unit' field is present
  bool has_unit;
  // Unit is optional, if not present should always be set to LORIS_STR_NONE
  str_t unit;
  report_t read_function;
  // wake_interval_sec is optional, if not present should always be set to 0
  int32_t wake_interval_sec;
} sensor_t;

typedef struct sensor_node {
  sys_snode_t node;
  sensor_t sensor;
} sensor_node_t;

typedef struct function {
  int32_t id;
  str_t name;
  str_t description;
  func_arg_type_t arg_type;
  // Callback to a function that makes the actuator do something
  void (*call_function)();
  // wake_interval_sec is optional, if not present should always be set to 0
  int32_t wake_interval_sec;
} function_t;

typedef struct function_node {
  sys_snode_t node;
  function_t function;
} function_node_t;

typedef struct capabilities {
  str_t device_name;
  device_class_t device_class;

  // NOTE: sys_slist functions are not thread safe
  sys_slist_t sensors;
  // NOTE: sys_slist functions are not thread safe
  sys_slist_t functions;
} capabilities_t;

typedef enum report_value_type {
  REPORT_VALUE_INT,
  REPORT_VALUE_FLOAT,
  REPORT_VALUE_STRING,
  REPORT_VALUE_BOOL,
} report_value_type_t;

typedef struct report {
  int32_t sensor_id;
  report_value_type_t which_value;
  union {
    int32_t int_val;
    float float_val;
    str_t str_val;
    bool bool_val;
  } value;
} report_t;

typedef struct hello {
  char dummy_field;
} hello_t;

typedef enum call_value_type {
  CALL_VALUE_INT,
  CALL_VALUE_FLOAT,
  CALL_VALUE_STRING,
  CALL_VALUE_BOOL,
} call_value_type_t;

typedef struct call {
  int32_t function_id;
  call_value_type_t which_value;
  union {
    int32_t int_val;
    float float_val;
    str_t str_val;
    bool bool_val;
  } value;
  bool has_wake_after_secs;
  int32_t wake_after_secs;
} call_t;

#endif
