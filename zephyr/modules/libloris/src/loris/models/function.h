#ifndef LORIS_FUNCTION_H
#define LORIS_FUNCTION_H

#include <inttypes.h>
#include <zephyr/sys/slist.h>

#include "loris/protobuf/protocol.pb.h"
#include "loris/str.h"

/*
 * Argument type used in capabilities.function
 */
typedef enum func_arg_type {
    FUNC_ARG_TYPE_UNSPECIFIED = PROTOCOL_V1_FUNC_ARG_TYPE_FUNC_ARG_TYPE_UNSPECIFIED,
    FUNC_ARG_TYPE_VOID        = PROTOCOL_V1_FUNC_ARG_TYPE_FUNC_ARG_TYPE_VOID,
    FUNC_ARG_TYPE_INT         = PROTOCOL_V1_FUNC_ARG_TYPE_FUNC_ARG_TYPE_INT,
    FUNC_ARG_TYPE_FLOAT       = PROTOCOL_V1_FUNC_ARG_TYPE_FUNC_ARG_TYPE_FLOAT,
    FUNC_ARG_TYPE_STRING      = PROTOCOL_V1_FUNC_ARG_TYPE_FUNC_ARG_TYPE_STRING,
    FUNC_ARG_TYPE_BOOL        = PROTOCOL_V1_FUNC_ARG_TYPE_FUNC_ARG_TYPE_BOOL,
} func_arg_type_t;

BUILD_ASSERT((int)FUNC_ARG_TYPE_UNSPECIFIED == (int)_PROTOCOL_V1_FUNC_ARG_TYPE_MIN, "Enums don't match, possible missing members");
BUILD_ASSERT((int)FUNC_ARG_TYPE_BOOL        == (int)_PROTOCOL_V1_FUNC_ARG_TYPE_MAX, "Enums don't match, possible missing members");

typedef struct function {
  sys_snode_t node;

  int32_t id;
  str_t name;
  str_t description;
  func_arg_type_t arg_type;
  // Callback to a function that makes the actuator do something
  // TODO: add different arg types (union probably)
  result_t (*call_function)(void *arg);
  // wake_interval_sec is optional, if not present should always be set to 0
  int32_t wake_interval_sec;
} function_t;

#endif
