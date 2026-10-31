#ifndef LORIS_CALL_H
#define LORIS_CALL_H

#include <inttypes.h>
#include <stdbool.h>
#include <zephyr/sys/slist.h>

#include "loris/str.h"
#include "loris/protobuf/protocol.pb.h"

// clang-format off
typedef enum call_value_type {
  CALL_VALUE_INT    = PROTOCOL_V1_CALL_INT_VAL_TAG,
  CALL_VALUE_FLOAT  = PROTOCOL_V1_CALL_FLOAT_VAL_TAG,
  CALL_VALUE_STRING = PROTOCOL_V1_CALL_STR_VAL_TAG,
  CALL_VALUE_BOOL   = PROTOCOL_V1_CALL_BOOL_VAL_TAG,
} call_value_type_t;
// clang-format on

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
