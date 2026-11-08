/*
 * Copyright (c) 2026 Tomasz Gadziński
 *
 * SPDX-License-Identifier: MIT
 */
#ifndef LORIS_CODEC_H
#define LORIS_CODEC_H

/**
 * @file
 * @brief loris protocol payload codec (encoding and decoding) API.
 */

#include "loris/models/downlink.h"
#include "loris/models/uplink.h"
#include "loris/result.h"

/**
 * @brief Encode an uplink structure into Protobuf format.
 *
 * @param[out] out_buf       Destination buffer for encoded bytes. Must not be
 * NULL.
 * @param[in]  out_buf_size  Capacity of @p out_buf in bytes.
 * @param[in]  ul            Source uplink to encode. Must not be NULL.
 * @param[out] bytes_written Optional pointer to store total number of bytes
 * written (encoded). May be NULL.
 *
 * @retval OK    Encoding completed successfully.
 * @retval ERROR Payload contents invalid or destination buffer space
 * insufficient or source uplink is NULL.
 */
result_t encode_uplink(
    char* out_buf,
    size_t out_buf_size,
    const uplink_t* ul,
    size_t* bytes_written
);

/**
 * @brief Decode a raw Protobuf bytes buffer into a downlink structure.
 *
 * @note Dynamically allocates memory for string call values. Caller must
 * release resources via downlink_free() after use, regardless of payload type.
 *
 * @param[out] out      Destination downlink structure to populate. Must not be
 * NULL.
 * @param[in]  buf_in   Input raw payload buffer. Must not be NULL.
 * @param[in]  buf_size Size of input payload in @p buf_in in bytes.
 *
 * @retval OK    Decoding completed successfully.
 * @retval ERROR Payload format malformed or contains unsupported fields or
 * types or out downlink is NULL.
 */
result_t decode_downlink(downlink_t* out, const char* buf_in, size_t buf_size);

#ifdef CONFIG_LORIS_TEST
/**
 * @brief Decode a raw Protobuf bytes buffer into an uplink structure.
 *
 * @note Test build feature only (`CONFIG_LORIS_TEST`). Dynamically allocates
 * memory for internal capabilities lists (sensors and functions). Caller must
 * release resources by passing @p out to uplink_free().
 *
 * @param[out] out      Destination uplink struct to populate. Must not be NULL.
 * @param[in]  buf_in   Input raw payload buffer. Must not be NULL.
 * @param[in]  buf_size Size of input payload in @p buf_in in bytes.
 *
 * @retval OK    Decoding completed successfully.
 * @retval ERROR Payload format malformed or memory allocation failed or out
 * pointer is NULL.
 */
result_t decode_uplink(uplink_t* out, const char* buf_in, size_t buf_size);

/**
 * @brief Encode a downlink payload structure into Protobuf format.
 *
 * @note Test build feature only (`CONFIG_LORIS_TEST`).
 *
 * @param[out] out_buf      Destination buffer for encoded bytes. Must not be
 * NULL.
 * @param[in]  out_buf_size Capacity of @p out_buf in bytes.
 * @param[in]  in           Source downlink struct to encode. Must not be
 * NULL.
 * @param[out] bytes_written Optional pointer to store total encoded payload
 * length. May be NULL.
 *
 * @retval OK    Encoding completed successfully.
 * @retval ERROR Payload contents invalid or destination buffer space
 * insufficient or input downlink is NULL.
 */
result_t encode_downlink(
    char* out_buf,
    size_t out_buf_size,
    const downlink_t* in,
    size_t* bytes_written
);

/**
 * @brief Free heap allocations owned by an uplink structure.
 *
 * @note Test build feature only (`CONFIG_LORIS_TEST`). Cleans up dynamically
 * allocated string buffers and linked list nodes created by decode_uplink().
 *
 * @param[in,out] ul Pointer to uplink structure to release. Does nothing if
 * NULL.
 */
void uplink_free(uplink_t* ul);
#endif

#endif
