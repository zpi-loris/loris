#ifndef LORIS_CAPABILITIES_H
#define LORIS_CAPABILITIES_H


#include <inttypes.h>
#include <stdbool.h>
#include <zephyr/sys/slist.h>

#include "loris/str.h"
#include "loris/models/devclass.h"

typedef struct capabilities {
  str_t device_name;
  device_class_t device_class;

  // NOTE: sys_slist functions are not thread safe
  sys_slist_t sensors;
  // NOTE: sys_slist functions are not thread safe
  sys_slist_t functions;
} capabilities_t;

#endif
