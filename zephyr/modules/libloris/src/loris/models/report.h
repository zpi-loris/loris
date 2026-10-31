#ifndef LORIS_REPORT_H
#define LORIS_REPORT_H

#include <inttypes.h>
#include <stdbool.h>
#include <zephyr/sys/slist.h>

#include "loris/protobuf/protocol.pb.h"
#include "loris/str.h"

// clang-format off
typedef enum report_value_type {
  REPORT_VALUE_INT    = PROTOCOL_V1_REPORT_INT_VAL_TAG,
  REPORT_VALUE_FLOAT  = PROTOCOL_V1_REPORT_FLOAT_VAL_TAG,
  REPORT_VALUE_STRING = PROTOCOL_V1_REPORT_STR_VAL_TAG,
  REPORT_VALUE_BOOL   = PROTOCOL_V1_REPORT_BOOL_VAL_TAG,
} report_value_type_t;
// clang-format on

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
