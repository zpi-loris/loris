#ifndef LORIS_STR_H
#define LORIS_STR_H

#include <stddef.h>
#include <stdint.h>

#include "loris/result.h"

#define SLAB_STR_SIZE 128

typedef enum str_src_type {
  STR_SRC_NONE = 0,
  STR_SRC_STATIC = 1,
  STR_SRC_BORROWED = 2,
  STR_SRC_SLAB = 3,
  STR_SRC_DYNAMIC = 4,
} str_src_type_t;

typedef struct str {
  const char *ptr;

  /*
   * length without the null terminator '\0' -- only the payload,
   * e.g. 'hello' -> len=5
  */
  size_t len;

  str_src_type_t src_type;
} str_t;

#define LORIS_STR_NONE ((str_t){ .ptr = NULL, .len = 0, .src_type = STR_SRC_NONE})

void str_free(str_t* str);

result_t str_from_static(str_t* out, const char* cstr);
result_t str_borrow_cstr(str_t* out, const char* cstr);
result_t str_borrow(str_t* out, const str_t* src);

result_t str_alloc_dynamic(str_t* out, const char *cstr);

// allocate the string using a slab if its length < SLAB_STR_SIZE, dynamically
// otherwise
result_t str_alloc_smart(str_t* out, const char *cstr);

// allocate the string in a SLAB_STR_SIZE slab byte buffer
//
// preferable when size of the string is known to be smaller than
// SLAB_STR_SIZE, so we can avoid dynamic memory allocation problems like
// memory fragmentation
result_t str_alloc_slab(str_t* out, const char* cstr);


#endif
