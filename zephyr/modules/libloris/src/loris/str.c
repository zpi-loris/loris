#include <zephyr/kernel.h>
#include <string.h>

#include "loris/result.h"
#include "loris/str.h"

K_MEM_SLAB_DEFINE_STATIC(str_slab, SLAB_STR_SIZE, 8, 8);

void str_free(str_t* str) {
  if (str == NULL || str->ptr == NULL) {
    return;
  }

  if (str->alloc_type == STR_ALLOC_DYNAMIC) {
    k_free((void *)str->ptr);
  } else if (str->alloc_type == STR_ALLOC_SLAB) {
    k_mem_slab_free(&str_slab, (void *)(str->ptr));
  }

  *str = LORIS_STR_NONE;
}

result_t str_alloc_static(str_t* out, const char* cstr) {
  if (out == NULL || cstr == NULL) {
    return ERROR;
  }

  str_free(out);

  *out = (str_t){ 
    .ptr = (char*)cstr,
    .len = strlen(cstr),
    .alloc_type = STR_ALLOC_STATIC
  };
  return OK;
}

result_t str_alloc_slab(str_t* out, const char* cstr) {
  if (out == NULL || cstr == NULL) {
    return ERROR;
  }

  str_free(out);

  size_t len = strlen(cstr);
  if (len >= SLAB_STR_SIZE) {
    return ERROR;
  }

  char* buf;
  if (k_mem_slab_alloc(&str_slab, (void**)&buf, K_NO_WAIT) != 0) {
    return ERROR;
  }

  memcpy(buf, cstr, len);
  buf[len] = '\0';

  out->ptr = buf;
  out->len = len;
  out->alloc_type = STR_ALLOC_SLAB;

  return OK;
}

result_t str_alloc_dynamic(str_t* out, const char *cstr) {
  if (out == NULL || cstr == NULL) {
    return ERROR;
  }

  str_free(out);

  size_t len = strlen(cstr);
  char* buf = k_malloc(len + 1);
  if (buf == NULL) {
    return ERROR;
  }

  memcpy(buf, cstr, len);
  buf[len] = '\0';

  out->ptr = buf;
  out->len = len;
  out->alloc_type = STR_ALLOC_DYNAMIC;

  return OK;
}

result_t str_alloc_smart(str_t *out, const char *cstr) {
  if (out == NULL || cstr == NULL) {
    return ERROR;
  }

  // NOTE: strlen is used twice, but I'm not sure whether its worth extracting
  if (strlen(cstr) < SLAB_STR_SIZE) {
    return str_alloc_slab(out, cstr);
  } else {
    return str_alloc_dynamic(out, cstr);
  }
}
