#ifndef LORIS_NVS_H
#define LORIS_NVS_H

/**
 * @file
 * @brief loris Non-Volatile Storage (NVS) API
 */

#include <stddef.h>

/**
 * @brief Status codes returned by NVS operations.
 */
typedef enum lr_nvs_result {
  /** Operation completed successfully. */
  LR_NVS_OK = 0,

  /** Flash device issue or unready flash partition. */
  LR_NVS_ERR_DEVICE,

  /** Invalid argument provided (e.g. NULL pointer). */
  LR_NVS_ERR_INVALID_ARG,

  /** Provided ID is invalid or out of bounds. */
  LR_NVS_ERR_INV_ID,

  /** Low-level flash I/O failure. */
  LR_NVS_ERR_IO,

  /** Flash has no remaining space for write. */
  LR_NVS_ERR_NO_SPACE_LEFT,

  /** No value for specified ID in NVS. */
  LR_NVS_ERR_NO_VALUE,

  /** NVS subsystem or ZMS storage backend is not initialized. */
  LR_NVS_ERR_UNINITIALIZED,

  /** Unknown internal Zephyr ZMS error. */
  LR_NVS_ERR_UNKNOWN,
} lr_nvs_result_t;

/**
 * @brief IDs for data items stored in NVS.
 */
typedef enum lr_nvs_id {
  /** Internal sentinel for lower bound checking. Shouldn't be used. */
  LR_NVS_ID_MIN_SENTINEL = 0,

  /** Last used LoRaWAN DevNonce value. */
  LR_NVS_ID_LW_LAST_USED_DEVNONCE,

  /** Internal sentinel for upper bound checking. Shouldn't be used. */
  LR_NVS_ID_MAX_SENTINEL,
} lr_nvs_id_t;

/**
 * @brief Initialize the NVS subsystem.
 *
 * Configures flash sector parameters and mounts the underlying Zephyr ZMS
 * storage filesystem. Is thread-safe and can be called multiple times;
 * subsequent calls return LR_NVS_OK immediately.
 *
 * @retval LR_NVS_OK NVS mounted successfully or already initialized.
 * @retval LR_NVS_ERR_DEVICE Flash partition device not ready or invalid info.
 * @retval LR_NVS_ERR_IO Mount failed due to device I/O error.
 * @retval LR_NVS_ERR_UNKNOWN Flash mount failed with unknown error code.
 */
lr_nvs_result_t lr_nvs_init(void);

/**
 * @brief Read a record from NVS by ID.
 *
 * @param[in]  id         Record ID to read.
 * @param[out] data       Destination memory buffer. Must not be NULL.
 * @param[in]  len        Length of data to read (usually size of @p data).
 * @param[out] bytes_read Optional pointer to store actual bytes read.  Will be
 * smaller than @p len if stored data is smaller than requested. May be NULL.
 *
 * @retval LR_NVS_OK Success.
 * @retval LR_NVS_ERR_INVALID_ARG data buffer pointer is NULL.
 * @retval LR_NVS_ERR_UNINITIALIZED NVS system is not initialized.
 * @retval LR_NVS_ERR_INV_ID ID is out of bounds.
 * @retval LR_NVS_ERR_NO_VALUE No stored entry found for given ID.
 * @retval LR_NVS_ERR_IO Flash hardware read error.
 * @retval LR_NVS_ERR_UNKNOWN Unexpected error from storage layer.
 */
lr_nvs_result_t lr_nvs_read(
    lr_nvs_id_t id,
    void* data,
    size_t len,
    size_t* bytes_read
);

/**
 * @brief Write a record to NVS by ID.
 *
 * @param[in]  id            Record ID to write.
 * @param[in]  data          Payload source buffer. Must not be NULL.
 * @param[in]  len           Payload length in bytes.
 * @param[out] bytes_written Optional pointer to store actual bytes written.
 * May be NULL. Zero bytes written means stored data didn't change - no write
 * needed.
 *
 * @retval LR_NVS_OK Success.
 * @retval LR_NVS_ERR_INVALID_ARG Data buffer pointer is NULL.
 * @retval LR_NVS_ERR_UNINITIALIZED Subsystem is not initialized.
 * @retval LR_NVS_ERR_INV_ID ID is out of valid bounds.
 * @retval LR_NVS_ERR_DEVICE Flash write failed due to device fault.
 * @retval LR_NVS_ERR_IO Flash hardware write error.
 * @retval LR_NVS_ERR_NO_SPACE_LEFT Partition storage is full.
 * @retval LR_NVS_ERR_UNKNOWN Unexpected error from storage layer.
 */
lr_nvs_result_t lr_nvs_write(
    lr_nvs_id_t id,
    const void* data,
    size_t len,
    size_t* bytes_written
);

#ifdef CONFIG_LORIS_TEST
/**
 * @brief Clear all stored NVS records and reset subsystem init state.
 *
 * @note Test build feature only (`CONFIG_LORIS_TEST`). Uninitializes NVS -
 * requires calling lr_nvs_init() before executing subsequent reads or writes.
 */
void lr_nvs_clear(void);
#endif

#endif
