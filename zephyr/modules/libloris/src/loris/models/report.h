#ifndef LORIS_REPORT_H
#define LORIS_REPORT_H

#include <inttypes.h>
#include <stdbool.h>
#include <zephyr/sys/slist.h>

#include "loris/str.h"

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

#endif
