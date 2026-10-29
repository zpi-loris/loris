#include <stdio.h>
#include <zephyr/kernel.h>
#include <zephyr/logging/log.h>

#include "loris/protobuf/pb.h"
#include "loris/protobuf/pb_decode.h"
#include "loris/protobuf/pb_encode.h"
#include "loris/protobuf/protocol.pb.h"
#include "loris/codec.h"

LOG_MODULE_REGISTER(codec);

// === Utility callbacks ===
bool pb_callback_encode_str(pb_ostream_t* ostream, const pb_field_t *field, const str_t* str) {
    if (str->ptr == NULL) {
      return false;
    }

    LOG_DBG("Encoding '%s'\n", str->ptr);

    if (!pb_encode_tag_for_field(ostream, field)) {
      LOG_WRN("pb_encode_tag_for_field failed\n");
      return false;
    }

    return pb_encode_string(ostream, (const uint8_t*)str->ptr, str->len);
}

bool pb_callback_decode_str(pb_istream_t* istream, const pb_field_t *field) {
    size_t len = istream->bytes_left;
    str_t s = {0};
    if (str_alloc_smart_len(&s, len) != OK) {
      LOG_WRN("str_alloc_smart_len failed\n");
      return false;
    }

    if (!pb_read(istream, (char*)s.ptr, len)) {
      LOG_WRN("pb_read failed\n");
      str_free(&s);
      return false;
    }

    *(str_t*)field->pData = s;

    return true;
}

//  == Protobuf named callback functions  ==
bool protocol_v1_Call_callback(pb_istream_t* istream, pb_ostream_t* ostream, const pb_field_t *field) {
  LOG_DBG("Call msg callback called");
  if (ostream && field->tag == PROTOCOL_V1_CALL_STR_VAL_TAG) {
    LOG_DBG("running in ENCODING mode\n");

    str_t* str = (str_t *)field->pData;
    return pb_callback_encode_str(ostream, field, str);
  }

  if (istream && field->tag == PROTOCOL_V1_CALL_STR_VAL_TAG) {
    LOG_DBG("running in DECODING mode\n");

    return pb_callback_decode_str(istream, field);
  } 

  LOG_WRN("Both istream and ostream are null!\n");
  return false;
}


result_t encode_uplink(
    char* out_buf,
    size_t out_buf_size,
    const uplink_t* ul,
    size_t* bytes_written
) {
  return ERROR;
}

result_t encode_downlink(
    char *out_buf,
    size_t out_buf_size,
    const downlink_t *dl,
    size_t *bytes_written
) {
  LOG_DBG("Encoding downlink\n");
  pb_ostream_t ostream = pb_ostream_from_buffer(out_buf, out_buf_size);

  protocol_v1_downlink_t pb_dl = {0};
  pb_dl.which_payload = dl->payload_type;
  if (dl->payload_type == DOWNLINK_PAYLOAD_TYPE_CALL) {
    pb_dl.payload.call.function_id = dl->payload.call.function_id;
    pb_dl.payload.call.which_value = dl->payload.call.which_value;
    switch (dl->payload.call.which_value) {
      case CALL_VALUE_INT:
        pb_dl.payload.call.value.int_val = dl->payload.call.value.int_val;
        break;
      case CALL_VALUE_FLOAT:
        pb_dl.payload.call.value.float_val = dl->payload.call.value.float_val;
        break;
      case CALL_VALUE_STRING:
        str_borrow(&pb_dl.payload.call.value.str_val, &dl->payload.call.value.str_val);
        break;
      case CALL_VALUE_BOOL:
        pb_dl.payload.call.value.bool_val = dl->payload.call.value.bool_val;
        break;
      default:
        LOG_WRN("Unknown or invalid call value type\n");
        return ERROR;
    }
    pb_dl.payload.call.has_wake_after_secs = dl->payload.call.has_wake_after_secs;
    pb_dl.payload.call.wake_after_secs = dl->payload.call.wake_after_secs;
  } else if (dl->payload_type == DOWNLINK_PAYLOAD_TYPE_PING) {
    pb_dl.payload.ping.type = (int)dl->payload.ping.type;
  } else {
    LOG_WRN("Unknown or invalid downlink payload type\n");
    return ERROR;
  }

  if (!pb_encode(&ostream, &protocol_v1_downlink_t_msg, &pb_dl)) {
    LOG_WRN("ENCODING FAILED!\n");
    return ERROR;
  }

  if (bytes_written) {
    *bytes_written = ostream.bytes_written;
  }

  LOG_DBG("Correctly encoded!\n");
  return OK;
}

result_t decode_downlink(downlink_t *out, const char* buf_in, size_t buf_size) {
  pb_istream_t istream = pb_istream_from_buffer(buf_in, buf_size);

  protocol_v1_downlink_t pb_dl = PROTOCOL_V1_DOWNLINK_INIT_ZERO;

  if(!pb_decode(&istream, &protocol_v1_downlink_t_msg, &pb_dl)) {
    return ERROR;
  };

  out->payload_type = pb_dl.which_payload;
  if (pb_dl.which_payload == PROTOCOL_V1_DOWNLINK_CALL_TAG) {
    out->payload.call.function_id = pb_dl.payload.call.function_id;
    out->payload.call.which_value = pb_dl.payload.call.which_value;
    switch (pb_dl.payload.call.which_value) {
      case PROTOCOL_V1_CALL_INT_VAL_TAG:
        out->payload.call.value.int_val = pb_dl.payload.call.value.int_val;
        break;
      case PROTOCOL_V1_CALL_FLOAT_VAL_TAG:
        out->payload.call.value.float_val = pb_dl.payload.call.value.float_val;
        break;
      case PROTOCOL_V1_CALL_STR_VAL_TAG:
        str_move(&out->payload.call.value.str_val, &pb_dl.payload.call.value.str_val);
        break;
      case PROTOCOL_V1_CALL_BOOL_VAL_TAG:
        out->payload.call.value.bool_val = pb_dl.payload.call.value.bool_val;
        break;
      default:
        LOG_WRN("Unknown or invalid call value type\n");
        return ERROR;
    }
    out->payload.call.has_wake_after_secs = pb_dl.payload.call.has_wake_after_secs;
    out->payload.call.wake_after_secs = pb_dl.payload.call.wake_after_secs;
  } else if (pb_dl.which_payload == PROTOCOL_V1_DOWNLINK_PING_TAG) {
    out->payload.ping.type = (int)pb_dl.payload.ping.type;
  } else {
    LOG_WRN("Unknown or invalid downlink payload type\n");
    return ERROR;
  }

  return OK;
}
void downlink_free(downlink_t* dl) {
  if (dl->payload_type != DOWNLINK_PAYLOAD_TYPE_CALL) {
    return;
  }

  if (dl->payload.call.which_value != CALL_VALUE_STRING) {
    return;
  }

  str_free(&dl->payload.call.value.str_val);
  LOG_DBG("freed downlink\n");
} 
