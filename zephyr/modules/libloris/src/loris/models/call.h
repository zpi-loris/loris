#ifndef LORIS_CALL_H
#define LORIS_CALL_H

#include <inttypes.h>
#include <stdbool.h>
#include <zephyr/sys/slist.h>

#include "loris/str.h"

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
