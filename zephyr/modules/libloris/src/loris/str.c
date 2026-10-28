#include <zephyr/kernel.h>
#include <string.h>

#include "loris/result.h"
#include "loris/str.h"

K_MEM_SLAB_DEFINE_STATIC(str_slab, SLAB_STR_SIZE, 8, 8);

void str_free(str_t* str) {
  if (str == NULL || str->ptr == NULL) {
    return;
  }

  if (str->src_type == STR_SRC_DYNAMIC) {
    k_free((void *)str->ptr);
  } else if (str->src_type == STR_SRC_SLAB) {
    k_mem_slab_free(&str_slab, (void *)(str->ptr));
  }

  *str = LORIS_STR_NONE;
}

result_t str_from_static(str_t* out, const char* cstr) {
  if (out == NULL || cstr == NULL) {
    return ERROR;
  }

  str_free(out);

  *out = (str_t){ 
    .ptr = (char*)cstr,
    .len = strlen(cstr),
    .src_type = STR_SRC_STATIC
  };
  return OK;
}

result_t str_borrow_cstr(str_t *out, const char *cstr) {
  if (out == NULL || cstr == NULL) {
    return ERROR;
  }

  str_free(out);

  *out = (str_t){
    .ptr = cstr,
    .len = strlen(cstr),
    .src_type = STR_SRC_BORROWED
  };

  return OK;
}

result_t str_borrow(str_t *out, const str_t *src) {
  if (out == NULL || src == NULL) {
    return ERROR;
  }

  str_free(out);

  *out = *src;
  out->src_type = STR_SRC_BORROWED;

  return OK;
}

result_t str_alloc_slab(str_t* out, const char* cstr) {
  if (out == NULL || cstr == NULL) {
    return ERROR;
  }

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

  str_free(out);

  out->ptr = buf;
  out->len = len;
  out->src_type = STR_SRC_SLAB;

  return OK;
}

result_t str_alloc_dynamic(str_t* out, const char *cstr) {
  if (out == NULL || cstr == NULL) {
    return ERROR;
  }

  size_t len = strlen(cstr);
  char* buf = k_malloc(len + 1);
  if (buf == NULL) {
    return ERROR;
  }

  memcpy(buf, cstr, len);
  buf[len] = '\0';

  str_free(out);

  out->ptr = buf;
  out->len = len;
  out->src_type = STR_SRC_DYNAMIC;

  return OK;
}

result_t str_alloc_smart(str_t *out, const char *cstr) {
  if (out == NULL || cstr == NULL) {
    return ERROR;
  }

  char* buf;
  size_t len = strlen(cstr);
  if (strlen(cstr) < SLAB_STR_SIZE && k_mem_slab_alloc(&str_slab, (void**)&buf, K_NO_WAIT) == 0) {
    out->src_type = STR_SRC_SLAB;
  } else {
    buf = k_malloc(len + 1);
    if (buf == NULL) {
      return ERROR;
    }

    out->src_type = STR_SRC_DYNAMIC;
  }

  memcpy(buf, cstr, len);
  buf[len] = '\0';

  str_free(out);

  out->ptr = buf;
  out->len = len;

  return OK;
}
