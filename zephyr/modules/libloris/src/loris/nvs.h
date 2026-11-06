#ifndef LORIS_NVS_H
#define LORIS_NVS_H

#include <stddef.h>

typedef enum lr_nvs_result {
  LR_NVS_OK = 0,
  LR_NVS_ERR_DEVICE,
  LR_NVS_ERR_INVALID_ARG,
  LR_NVS_ERR_INV_ID,
  LR_NVS_ERR_IO,
  LR_NVS_ERR_NO_SPACE_LEFT,
  LR_NVS_ERR_NO_VALUE,
  LR_NVS_ERR_UNINITIALIZED,
  LR_NVS_ERR_UNKNOWN,
} lr_nvs_result_t;

typedef enum lr_nvs_id {
  LR_NVS_ID_MIN_SENTINEL = 0,

  LR_NVS_ID_LW_DEV_NONCE,

  LR_NVS_ID_MAX_SENTINEL,
} lr_nvs_id_t;

lr_nvs_result_t lr_nvs_init(void);

lr_nvs_result_t lr_nvs_read(
    lr_nvs_id_t id,
    void* data,
    size_t len,
    size_t* bytes_read
);

lr_nvs_result_t lr_nvs_write(
    lr_nvs_id_t id,
    const void* data,
    size_t len,
    size_t* bytes_written
);

#ifdef CONFIG_LORIS_TEST
void lr_nvs_clear(void);
#endif

#endif
