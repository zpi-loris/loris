#ifndef LORIS_FUNCTION_H
#define LORIS_FUNCTION_H

#include <inttypes.h>
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

typedef struct function {
  int32_t id;
  str_t name;
  str_t description;
  func_arg_type_t arg_type;
  // Callback to a function that makes the actuator do something
  // TODO: add different arg types (union probably)
  void (*call_function)();
  // wake_interval_sec is optional, if not present should always be set to 0
  int32_t wake_interval_sec;
} function_t;

typedef struct function_node {
  sys_snode_t node;
  function_t function;
} function_node_t;

#endif
