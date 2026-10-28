#include <zephyr/kernel.h>
#include <string.h>
#include <stdio.h>

#include "loris/result.h"
#include "loris/str.h"

K_MEM_SLAB_DEFINE_STATIC(str_slab, STR_SLAB_BLOCK_SIZE, STR_SLAB_BLOCKS, 8);

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
  if (len >= STR_SLAB_BLOCK_SIZE) {
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

  size_t len = strlen(cstr);
  if (str_alloc_smart_len(out, len) != OK) {
    return ERROR;
  }

  // NOTE: kinda tricky getting rid of the const qualifier
  // Might be good to rethink that
  memcpy((char*)out->ptr, cstr, len); 
  ((char*)out->ptr)[len] = '\0';

  return OK;
}

result_t str_alloc_smart_len(str_t* out, size_t len) {
  if (out == NULL) {
    return ERROR;
  }

  char* buf;
  str_src_type_t src_type;
  if (len < STR_SLAB_BLOCK_SIZE && k_mem_slab_alloc(&str_slab, (void**)&buf, K_NO_WAIT) == 0) {
    src_type = STR_SRC_SLAB;
  } else {
    buf = k_malloc(len + 1);
    if (buf == NULL) {
      return ERROR;
    }

    src_type = STR_SRC_DYNAMIC;
  }

  str_free(out);

  buf[len] = '\0';

  out->ptr = buf;
  out->len = len;
  out->src_type = src_type;

  return OK;
}

result_t str_move(str_t* dst, str_t* src) {
  if (dst == NULL || src == NULL) {
    return ERROR;
  }

  if (dst == src) {
    return OK;
  }

  str_free(dst);
  *dst = *src;
  *src = LORIS_STR_NONE;

  return OK;
}

size_t str_slab_num_free(void) {
  return k_mem_slab_num_free_get(&str_slab);
}
