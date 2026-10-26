#ifndef LORIS_STR_H
#define LORIS_STR_H

#include <stddef.h>


typedef enum str_alloc_type {
  STR_ALLOC_STATIC = 0,
  STR_ALLOC_SLAB = 1,
  STR_ALLOC_DYNAMIC = 2,
} str_alloc_type_t;

typedef struct str {
  char *ptr;

  /*
   * length without the null terminator '\0' -- only the payload,
   * e.g. 'hello' -> len=5
  */
  size_t len;

  str_alloc_type_t alloc_type;
} str_t;

#define LORIS_STR_NONE ((str_t){ .ptr = NULL, .len = 0, .alloc_type = STR_ALLOC_STATIC})

// TODO: Helper methods:
// 1. str_free,
// 2. str_alloc_static
// 3. str_alloc_slab
// 4. str_alloc_dynamic

#endif
