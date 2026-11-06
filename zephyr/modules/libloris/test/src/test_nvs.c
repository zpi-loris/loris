#include <stdbool.h>
#include <stdint.h>
#include <string.h>
#include <zephyr/ztest.h>

#include "loris/nvs.h"

static void lr_nvs_test_before(void* fixture) {
  lr_nvs_init();
  lr_nvs_clear();
  lr_nvs_init();
}

ZTEST_SUITE(lr_nvs, NULL, NULL, lr_nvs_test_before, NULL, NULL);

ZTEST(lr_nvs, test_init_idempotent) {
  zassert_equal(lr_nvs_init(), LR_NVS_OK);
  zassert_equal(lr_nvs_init(), LR_NVS_OK);
}

typedef struct nvs_type_param {
  lr_nvs_id_t id;
  const void* data;
  size_t len;
} nvs_type_param_t;

static const uint32_t val_u32 = 0xDEADBEEF;
static const char val_char = 'Z';
static const bool val_bool = true;
static const char val_str[] = "Zephyr NVS test string";
static const int32_t val_i32 = -123456;

static const nvs_type_param_t type_params[] = {
    {.id = LR_NVS_ID_LW_DEV_NONCE, .data = &val_u32, .len = sizeof(val_u32)},
    {.id = LR_NVS_ID_LW_DEV_NONCE, .data = &val_char, .len = sizeof(val_char)},
    {.id = LR_NVS_ID_LW_DEV_NONCE, .data = &val_bool, .len = sizeof(val_bool)},
    {.id = LR_NVS_ID_LW_DEV_NONCE, .data = val_str, .len = sizeof(val_str)},
    {.id = LR_NVS_ID_LW_DEV_NONCE, .data = &val_i32, .len = sizeof(val_i32)},
};

ZTEST_DEFINE_PARAM_VALUES_ARRAY(nvs_type_params, type_params);
ZTEST_INSTANTIATE_TEST_SUITE_P(
    type_roundtrips,
    lr_nvs,
    test_write_read_roundtrip_types,
    nvs_type_params
);

ZTEST_P(lr_nvs, test_write_read_roundtrip_types) {
  nvs_type_param_t param = ZTEST_GET_PARAM(nvs_type_param_t);
  uint8_t read_buf[64] = {0};
  size_t bytes_written = 0;
  size_t bytes_read = 0;

  zassert_equal(
      lr_nvs_write(param.id, (void*)param.data, param.len, &bytes_written),
      LR_NVS_OK
  );
  zassert_equal(bytes_written, param.len);

  zassert_equal(
      lr_nvs_read(param.id, read_buf, sizeof(read_buf), &bytes_read),
      LR_NVS_OK
  );
  zassert_equal(bytes_read, param.len);
  zassert_mem_equal(read_buf, param.data, param.len);
}

ZTEST(lr_nvs, test_read_unwritten_id) {
  uint32_t buf = 0;
  size_t bytes_read = 0;

  lr_nvs_result_t res =
      lr_nvs_read(LR_NVS_ID_LW_DEV_NONCE, &buf, sizeof(buf), &bytes_read);
  zassert_equal(res, LR_NVS_ERR_NO_VALUE, "res = %d", res);
}

ZTEST(lr_nvs, test_id_sentinels_and_out_of_bounds) {
  uint32_t val = 42;
  size_t bytes_written = 0;
  size_t bytes_read = 0;

  zassert_equal(
      lr_nvs_write(LR_NVS_ID_MIN_SENTINEL, &val, sizeof(val), &bytes_written),
      LR_NVS_ERR_INV_ID
  );
  zassert_equal(
      lr_nvs_read(LR_NVS_ID_MIN_SENTINEL, &val, sizeof(val), &bytes_read),
      LR_NVS_ERR_INV_ID
  );

  zassert_equal(
      lr_nvs_write(LR_NVS_ID_MAX_SENTINEL, &val, sizeof(val), &bytes_written),
      LR_NVS_ERR_INV_ID
  );
  zassert_equal(
      lr_nvs_read(LR_NVS_ID_MAX_SENTINEL, &val, sizeof(val), &bytes_read),
      LR_NVS_ERR_INV_ID
  );

  lr_nvs_id_t underflow_id = (lr_nvs_id_t)(LR_NVS_ID_MIN_SENTINEL - 1);
  zassert_equal(
      lr_nvs_write(underflow_id, &val, sizeof(val), &bytes_written),
      LR_NVS_ERR_INV_ID
  );
  zassert_equal(
      lr_nvs_read(underflow_id, &val, sizeof(val), &bytes_read),
      LR_NVS_ERR_INV_ID
  );

  lr_nvs_id_t overflow_id = (lr_nvs_id_t)(LR_NVS_ID_MAX_SENTINEL + 1);
  zassert_equal(
      lr_nvs_write(overflow_id, &val, sizeof(val), &bytes_written),
      LR_NVS_ERR_INV_ID
  );
  zassert_equal(
      lr_nvs_read(overflow_id, &val, sizeof(val), &bytes_read),
      LR_NVS_ERR_INV_ID
  );
}

ZTEST(lr_nvs, test_null_bytes_pointers) {
  uint32_t write_val = 0xABCD1234;
  uint32_t read_val = 0;

  zassert_equal(
      lr_nvs_write(LR_NVS_ID_LW_DEV_NONCE, &write_val, sizeof(write_val), NULL),
      LR_NVS_OK
  );
  zassert_equal(
      lr_nvs_read(LR_NVS_ID_LW_DEV_NONCE, &read_val, sizeof(read_val), NULL),
      LR_NVS_OK
  );
  zassert_equal(read_val, write_val);
}

ZTEST(lr_nvs, test_null_data_buffer) {
  uint32_t val = 123;
  size_t bytes_written = 0;
  size_t bytes_read = 0;

  zassert_equal(
      lr_nvs_write(LR_NVS_ID_LW_DEV_NONCE, NULL, sizeof(val), &bytes_written),
      LR_NVS_ERR_INVALID_ARG
  );
  zassert_equal(
      lr_nvs_read(LR_NVS_ID_LW_DEV_NONCE, NULL, sizeof(val), &bytes_read),
      LR_NVS_ERR_INVALID_ARG
  );
}

ZTEST(lr_nvs, test_partial_read_truncation) {
  uint32_t write_val = 0x12345678;
  uint16_t read_val = 0;
  size_t bytes_read = 0;

  zassert_equal(
      lr_nvs_write(LR_NVS_ID_LW_DEV_NONCE, &write_val, sizeof(write_val), NULL),
      LR_NVS_OK
  );
  zassert_equal(
      lr_nvs_read(
          LR_NVS_ID_LW_DEV_NONCE,
          &read_val,
          sizeof(read_val),
          &bytes_read
      ),
      LR_NVS_OK
  );
  zassert_equal(bytes_read, sizeof(read_val));
  zassert_mem_equal(&read_val, &write_val, sizeof(read_val));
}
