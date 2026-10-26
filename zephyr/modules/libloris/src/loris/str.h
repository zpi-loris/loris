#ifndef LORIS_STR_H
#define LORIS_STR_H

#include <stddef.h>
#include <stdint.h>

#include "loris/result.h"

#define SLAB_STR_SIZE 128

typedef enum str_alloc_type {
  STR_ALLOC_NONE = 0,
  STR_ALLOC_STATIC = 1,
  STR_ALLOC_SLAB = 2,
  STR_ALLOC_DYNAMIC = 3,
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

#define LORIS_STR_NONE ((str_t){ .ptr = NULL, .len = 0, .alloc_type = STR_ALLOC_NONE})

void str_free(str_t* str);

// Warning: strings allocated using this function might be in read-only memory,
// thus the user of a statically allocated str_t should not mutate the
// underlying cstr pointer, even though it's not marked as const (to allows
// dynamically and slab allocated strings to be mutated)
result_t str_alloc_static(str_t* out, const char* cstr);

result_t str_alloc_dynamic(str_t* out, const char *cstr);

// allocate the string using a slab if its length < SLAB_STR_SIZE, dynamically
// otherwise
result_t str_alloc(str_t* out, const char *cstr);

// allocate the string in a SLAB_STR_SIZE slab byte buffer
//
// preferable when size of the string is known to be smaller than
// SLAB_STR_SIZE, so we can avoid dynamic memory allocation problems like
// memory fragmentation
result_t str_alloc_slab(str_t* out, const char* cstr);


#endif
