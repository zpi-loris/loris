#include <stdbool.h>
#include <zephyr/kernel.h>
#include <zephyr/kvss/zms.h>
#include <zephyr/logging/log.h>
#include <zephyr/storage/flash_map.h>
#include <zephyr/sys/atomic.h>

#include "loris/nvs.h"

LOG_MODULE_REGISTER(lr_nvs);

#define ZMS_PARTITION storage_partition
#define ZMS_PARTITION_DEVICE PARTITION_DEVICE(ZMS_PARTITION)
#define ZMS_PARTITION_OFFSET PARTITION_OFFSET(ZMS_PARTITION)

static const uint32_t SECTOR_COUNT = 4; // TODO: maybe put in Kconfig

static struct zms_fs fs = {0};

static atomic_t is_initialized = ATOMIC_INIT(false);
static K_MUTEX_DEFINE(init_mutex);

lr_nvs_result_t lr_nvs_init(void) {
  if (atomic_get(&is_initialized)) {
    return LR_NVS_OK;
  }

  k_mutex_lock(&init_mutex, K_FOREVER); // NOTE: is forever a good choice?
  if (atomic_get(&is_initialized)) {
    k_mutex_unlock(&init_mutex);
    return LR_NVS_OK;
  }

  fs.flash_device = ZMS_PARTITION_DEVICE;
  if (!device_is_ready(fs.flash_device)) {
    LOG_ERR("Flash device is not ready");
    k_mutex_unlock(&init_mutex);
    return LR_NVS_ERR_DEVICE;
  }

  fs.offset = ZMS_PARTITION_OFFSET;
  struct flash_pages_info info = {0};
  if (flash_get_page_info_by_offs(fs.flash_device, fs.offset, &info) != 0) {
    LOG_ERR("Unable to get flash page info");
    k_mutex_unlock(&init_mutex);
    return LR_NVS_ERR_DEVICE;
  }

  fs.sector_size = info.size;
  fs.sector_count = SECTOR_COUNT;

  int rc = zms_mount(&fs);
  lr_nvs_result_t ret;
  switch (rc) {
  case 0:
    LOG_INF("Initialized NVS");
    atomic_set(&is_initialized, true);
    ret = LR_NVS_OK;
    break;
  case -ENXIO:
    LOG_ERR("Error initializing NVS: device error");
    ret = LR_NVS_ERR_DEVICE;
    break;
  case -EIO:
    LOG_ERR("Error initializing NVS: IO error");
    ret = LR_NVS_ERR_IO;
    break;
  default:
    LOG_ERR("Error initializing NVS: unknown error, rc=%d", rc);
    ret = LR_NVS_ERR_UNKNOWN;
    break;
  }

  k_mutex_unlock(&init_mutex);
  return ret;
}

static bool is_id_ok(lr_nvs_id_t id) {
  return id > LR_NVS_ID_MIN_SENTINEL && id < LR_NVS_ID_MAX_SENTINEL;
}

lr_nvs_result_t lr_nvs_read(
    lr_nvs_id_t id,
    void* data,
    size_t len,
    size_t* bytes_read
) {
  LOG_DBG("Reading NVS data with id=%d", id);

  if (data == NULL) {
    LOG_WRN("Data pointer is NULL");
    return LR_NVS_ERR_INVALID_ARG;
  }

  if (!atomic_get(&is_initialized)) {
    LOG_WRN("NVS is not initialized");
    return LR_NVS_ERR_UNINITIALIZED;
  }

  if (!is_id_ok(id)) {
    LOG_WRN("Invalid NVS id");
    return LR_NVS_ERR_INV_ID;
  }

  ssize_t rc = zms_read(&fs, id, data, len);
  if (rc > 0) {
    LOG_DBG("Read %zd bytes", rc);
    if (bytes_read) {
      *bytes_read = rc;
    }
    return LR_NVS_OK;
  }

  switch (rc) {
  case -EACCES:
    LOG_WRN("Error reading NVS: backend (ZMS) is uninitialized");
    return LR_NVS_ERR_UNINITIALIZED;
  case -EIO:
    LOG_WRN("Error reading NVS: IO error");
    return LR_NVS_ERR_IO;
  case -ENOENT:
    LOG_WRN("Error reading NVS: no value for id=%d found", id);
    return LR_NVS_ERR_NO_VALUE;
  default:
    LOG_WRN("Error reading NVS: unknown error, rc=%zd", rc);
    return LR_NVS_ERR_UNKNOWN;
  }
}

lr_nvs_result_t lr_nvs_write(
    lr_nvs_id_t id,
    const void* data,
    size_t len,
    size_t* bytes_written
) {
  LOG_DBG("Writing NVS data with id=%d", id);

  if (data == NULL) {
    LOG_WRN("Data pointer is NULL");
    return LR_NVS_ERR_INVALID_ARG;
  }

  if (!atomic_get(&is_initialized)) {
    LOG_WRN("NVS is not initialized");
    return LR_NVS_ERR_UNINITIALIZED;
  }

  if (!is_id_ok(id)) {
    LOG_WRN("Invalid NVS id");
    return LR_NVS_ERR_INV_ID;
  }

  ssize_t rc = zms_write(&fs, id, data, len);
  if (rc >= 0) {
    LOG_DBG("Written %zd bytes", rc);
    if (bytes_written) {
      *bytes_written = rc;
    }
    return LR_NVS_OK;
  }

  switch (rc) {
  case -EACCES:
    LOG_WRN("Error writing NVS: backend (ZMS) is uninitialized");
    return LR_NVS_ERR_UNINITIALIZED;
  case -ENXIO:
    LOG_WRN("Error writing NVS: device error");
    return LR_NVS_ERR_DEVICE;
  case -EIO:
    LOG_WRN("Error writing NVS: IO error");
    return LR_NVS_ERR_IO;
  case -ENOSPC:
    LOG_WRN("Error writing NVS: no space left");
    return LR_NVS_ERR_NO_SPACE_LEFT;
  default:
    LOG_WRN("Error writing NVS: unknown error, rc=%zd", rc);
    return LR_NVS_ERR_UNKNOWN;
  }
}

#ifdef CONFIG_LORIS_TEST
// Clears the NVS system and deinitializes it - must be reinitialized
void lr_nvs_clear(void) {
  if (!atomic_get(&is_initialized)) {
    return;
  }

  zms_clear(&fs);
  atomic_set(&is_initialized, false);
}
#endif
