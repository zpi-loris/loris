#ifndef LORIS_CODEC_H
#define LORIS_CODEC_H

#include "loris/models/downlink.h"
#include "loris/models/uplink.h"
#include "loris/result.h"

result_t encode_uplink(
    char* out_buf,
    size_t out_buf_size,
    const uplink_t* ul,
    size_t* bytes_written
);

// TODO guard the not needed in production decode and encode with #ifdef LORIS_TESTS
result_t decode_uplink(uplink_t *out, const char* buf_in, size_t buf_size);

result_t encode_downlink(
    char* out_buf,
    size_t out_buf_size,
    const downlink_t *in,
    size_t* bytes_written
);
result_t decode_downlink(downlink_t *out, const char* buf_in, size_t buf_size);

void downlink_free(downlink_t* dl); // NOTE: maybe move to downlink.h

#endif
