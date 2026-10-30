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
  if (ostream == NULL) {
    LOG_DBG("ostream is NULL\n");
    return false;
  }

  if (str->ptr == NULL  && str->src_type == STR_SRC_NONE) { 
    // its not set, just skip it
    return true;
  }

  if (str->ptr == NULL) {
    // it should be set, but isn't
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
  if (istream == NULL) {
    LOG_DBG("istream is NULL\n");
    return false;
  }

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

  LOG_DBG("Decoded '%s'\n", s.ptr);

  *(str_t*)field->pData = s;
  return true;
}

bool pb_callback_codec_str(pb_istream_t *istream, pb_ostream_t *ostream, const pb_field_t *field) {
  if (ostream) {
    LOG_DBG("running in ENCODING mode\n");
    str_t* str = (str_t *)field->pData;
    return pb_callback_encode_str(ostream, field, str);
  } 

  if (istream) {
    LOG_DBG("running in DECODING mode\n");
    return pb_callback_decode_str(istream, field);
  }

  LOG_WRN("both istream and ostream are NULL");
  return false;
}

bool pb_callback_encode_sensor_list(pb_ostream_t *ostream, const pb_field_t* field, void* const* arg) {
  LOG_DBG("Encoding sensors\n");

  sys_snode_t* node;
  sys_slist_t* list = *(sys_slist_t**)arg;

  SYS_SLIST_FOR_EACH_NODE(list, node) {
    sensor_t* s;

    s = CONTAINER_OF(node, sensor_t, node);
    LOG_DBG("Encoding sensor with id=%i; name='%s'\n", s->id, s->name.ptr);

    protocol_v1_sensor_t pb_s = PROTOCOL_V1_SENSOR_INIT_ZERO;
    pb_s.id = s->id;
    str_borrow(&pb_s.name, &s->name);
    str_borrow(&pb_s.description, &s->description);
    pb_s.value_type = (protocol_v1_value_type_t)s->value_type;
    if (s->has_unit) {
      str_borrow(&pb_s.unit, &s->unit);
    } else {
      pb_s.unit = LORIS_STR_NONE;
    }

    if (!pb_encode(ostream, &protocol_v1_sensor_t_msg, &pb_s)) {
      LOG_DBG("Encoding a sensor failed\n");
      return false;
    }
  }
  return true;
}

bool pb_callback_decode_sensor_list(pb_istream_t *istream, const pb_field_t* field, void** arg) {
  return false; // TODO
}

bool pb_callback_encode_function_list(pb_ostream_t *ostream, const pb_field_t* field, void* const* arg) {
  LOG_DBG("Encoding functions\n");

  sys_snode_t* node;
  sys_slist_t* list = *(sys_slist_t**)arg;

  SYS_SLIST_FOR_EACH_NODE(list, node) {
    function_t* f;

    f = CONTAINER_OF(node, function_t, node);
    LOG_DBG("Encoding function with id=%i; name='%s'\n", f->id, f->name.ptr);

    protocol_v1_function_t pb_f = PROTOCOL_V1_FUNCTION_INIT_ZERO;
    pb_f.id = f->id;
    str_borrow(&pb_f.name, &f->name);
    str_borrow(&pb_f.description, &f->description);
    pb_f.arg_type = (protocol_v1_func_arg_type_t)f->arg_type;
    if (!pb_encode(ostream, &protocol_v1_function_t_msg, &pb_f)) {
      LOG_DBG("Encoding a function failed\n");
      return false;
    }
  }

  return true;
}

bool pb_callback_decode_function_list(pb_istream_t *istream, const pb_field_t* field, void** arg) {
  return false; // TODO
}

//  == Protobuf named callback functions  ==
bool protocol_v1_Sensor_callback(pb_istream_t *istream, pb_ostream_t *ostream, const pb_field_t *field) {
  LOG_DBG("Sensor msg callback called\n");
  if (field->tag != PROTOCOL_V1_SENSOR_NAME_TAG &&
      field->tag != PROTOCOL_V1_SENSOR_DESCRIPTION_TAG &&
      field->tag != PROTOCOL_V1_SENSOR_UNIT_TAG) { // TODO: check if its presents - its optional but theres no 'has_unit' field
    LOG_WRN("Unknown or invalid (field with no callback) field tag\n");
    return false;
  }

  return pb_callback_codec_str(istream, ostream, field);
}

bool protocol_v1_Function_callback(pb_istream_t *istream, pb_ostream_t *ostream, const pb_field_t *field) {
  LOG_DBG("Function msg callback called\n");
  if (field->tag != PROTOCOL_V1_FUNCTION_NAME_TAG &&
      field->tag != PROTOCOL_V1_FUNCTION_DESCRIPTION_TAG) {
    LOG_WRN("Unknown or invalid (field with no callback) field tag\n");
    return false;
  }

  return pb_callback_codec_str(istream, ostream, field);
}

bool protocol_v1_Capabilities_callback(pb_istream_t *istream, pb_ostream_t *ostream, const pb_field_t *field) {
  LOG_DBG("Capabilities msg callback called\n");
  if (field->tag == PROTOCOL_V1_CAPABILITIES_SENSORS_TAG ||
      field->tag == PROTOCOL_V1_CAPABILITIES_FUNCTIONS_TAG) {
    return pb_default_field_callback(istream, ostream, field);
  }

  if (field->tag != PROTOCOL_V1_CAPABILITIES_DEVICE_NAME_TAG) {
    LOG_WRN("Unknown or invalid (field with no callback) field tag: %i\n", field->tag);
    return false;
  }

  return pb_callback_codec_str(istream, ostream, field);
}

bool protocol_v1_Report_callback(pb_istream_t *istream, pb_ostream_t *ostream, const pb_field_t *field) {
  LOG_DBG("Report msg callback called\n");
  if (field->tag != PROTOCOL_V1_REPORT_STR_VAL_TAG) {
    LOG_WRN("Unknown or invalid (field with no callback) field tag\n");
    return false;
  }

  return pb_callback_codec_str(istream, ostream, field);
}

bool protocol_v1_Call_callback(pb_istream_t* istream, pb_ostream_t* ostream, const pb_field_t *field) {
  LOG_DBG("Call msg callback called\n");
  if (field->tag != PROTOCOL_V1_CALL_STR_VAL_TAG) {
    LOG_WRN("Unknown or invalid (field with no callback) field tag\n");
    return false;
  }

  return pb_callback_codec_str(istream, ostream, field);
}


result_t encode_uplink(
    char* out_buf,
    size_t out_buf_size,
    const uplink_t* ul,
    size_t* bytes_written
) {
  LOG_DBG("Encoding uplink\n");

  pb_ostream_t ostream = pb_ostream_from_buffer(out_buf, out_buf_size);
  protocol_v1_uplink_t pb_ul = PROTOCOL_V1_UPLINK_INIT_ZERO;

  pb_ul.which_payload = ul->payload_type; 
  switch (ul->payload_type) {
    case UPLINK_PAYLOAD_TYPE_REPORT:
      pb_ul.payload.report.sensor_id = ul->payload.report.sensor_id;
      pb_ul.payload.report.which_value = ul->payload.report.which_value;
      switch (ul->payload.report.which_value) {
        case REPORT_VALUE_INT:
          pb_ul.payload.report.value.int_val = ul->payload.report.value.int_val;
          break;
        case REPORT_VALUE_FLOAT:
          pb_ul.payload.report.value.float_val = ul->payload.report.value.float_val;
          break;
        case REPORT_VALUE_STRING:
          str_borrow(&pb_ul.payload.report.value.str_val, &ul->payload.report.value.str_val);
          break;
        case REPORT_VALUE_BOOL:
          pb_ul.payload.report.value.bool_val = ul->payload.report.value.bool_val;
          break;
        default:
          LOG_WRN("Invalid report value type: '%i'\n", ul->payload.report.which_value);
          return ERROR;
      }
      break;
    case UPLINK_PAYLOAD_TYPE_CAPABILITIES:
      str_borrow(&pb_ul.payload.capabilities.device_name, &ul->payload.capabilities.device_name);

      // sensors
      pb_ul.payload.capabilities.sensors.funcs.encode = &pb_callback_encode_sensor_list;

      // NOTE: cast to void ptr to silence 'const qualifier discarded' warning,
      // I want the function to take const uplink, but callback discards it,
      // not sure what to do to ensure const in a different way
      pb_ul.payload.capabilities.sensors.arg = (void*)&ul->payload.capabilities.sensors;

      // functions
      pb_ul.payload.capabilities.functions.funcs.encode = &pb_callback_encode_function_list;
      // NOTE: same as above
      pb_ul.payload.capabilities.functions.arg = (void*)&ul->payload.capabilities.functions;
      break;
    case UPLINK_PAYLOAD_TYPE_PING:
      pb_ul.payload.ping.type = (int)ul->payload.ping.type;
      break;
    default:
      LOG_WRN("Invalid uplink payload type: '%i'\n", ul->payload_type);
      return ERROR;
  }

  if (!pb_encode(&ostream, &protocol_v1_uplink_t_msg, &pb_ul)) {
    LOG_WRN("ENCODING FAILED!\n");
    return ERROR;
  }

  if (bytes_written) {
    *bytes_written = ostream.bytes_written;
  }

  LOG_DBG("Correctly encoded!\n");
  return OK;
}

result_t decode_uplink(uplink_t *out, const char* buf_in, size_t buf_size) {
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

  protocol_v1_downlink_t pb_dl = PROTOCOL_V1_DOWNLINK_INIT_ZERO;
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
