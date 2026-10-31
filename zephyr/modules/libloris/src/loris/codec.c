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

bool pb_callback_encode_sensor_list(pb_ostream_t *ostream, const pb_field_t* field) {
  LOG_DBG("Encoding sensors\n");

  sys_snode_t* node;
  sys_slist_t* list = (sys_slist_t*)field->pData;

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

    if (!pb_encode_tag_for_field(ostream, field)) {
      LOG_DBG("Encoding tag for sensor failed\n");
      return false;
    }

    if (!pb_encode_submessage(ostream, &protocol_v1_sensor_t_msg, &pb_s)) {
      LOG_DBG("Encoding a sensor failed\n");
      return false;
    }
  }
  return true;
}

bool pb_callback_decode_sensor_to_list(pb_istream_t *istream, const pb_field_t* field) {
  LOG_DBG("Decoding sensor to list\n");

  sys_slist_t* list = (sys_slist_t*)field->pData;

  // NOTE: dynamically allocates but this function is only used for testing
  // purposes, so we don't care about memory fragmentation
  protocol_v1_sensor_t read_sensor = PROTOCOL_V1_SENSOR_INIT_ZERO;
  if (!pb_decode(istream, &protocol_v1_sensor_t_msg, &read_sensor)) {
    LOG_DBG("Failed decoding sensor\n");
    return false;
  }

  LOG_DBG("Decoded sensor; id=%i, name='%s'\n", read_sensor.id, read_sensor.name.ptr);

  sensor_t* s = k_calloc(1, sizeof(sensor_t));
  s->id = read_sensor.id;
  str_move(&s->name, &read_sensor.name);
  str_move(&s->description, &read_sensor.description);
  s->value_type = (value_type_t)read_sensor.value_type;
  if (read_sensor.unit.ptr != 0 && read_sensor.unit.len != 0) {
    s->has_unit = true;
    str_move(&s->unit, &read_sensor.unit);
  } else {
    s->has_unit = false;
    s->unit = LORIS_STR_NONE;
  }

  sys_slist_append(list, &s->node);

  return true;
}

bool pb_callback_encode_function_list(pb_ostream_t *ostream, const pb_field_t* field) {
  LOG_DBG("Encoding functions\n");

  sys_snode_t* node;
  sys_slist_t* list = (sys_slist_t*)field->pData;

  SYS_SLIST_FOR_EACH_NODE(list, node) {
    function_t* f;

    f = CONTAINER_OF(node, function_t, node);
    LOG_DBG("Encoding function with id=%i; name='%s'\n", f->id, f->name.ptr);

    protocol_v1_function_t pb_f = PROTOCOL_V1_FUNCTION_INIT_ZERO;
    pb_f.id = f->id;
    str_borrow(&pb_f.name, &f->name);
    str_borrow(&pb_f.description, &f->description);
    pb_f.arg_type = (protocol_v1_func_arg_type_t)f->arg_type;

    if (!pb_encode_tag_for_field(ostream, field)) {
      LOG_DBG("Encoding tag for function failed\n");
      return false;
    }

    if (!pb_encode_submessage(ostream, &protocol_v1_function_t_msg, &pb_f)) {
      LOG_DBG("Encoding a function failed\n");
      return false;
    }
  }

  return true;
}

bool pb_callback_decode_function_to_list(pb_istream_t *istream, const pb_field_t* field) {
  LOG_DBG("Decoding function to list\n");

  sys_slist_t* list = (sys_slist_t*)field->pData;

  // NOTE: dynamically allocates but this function is only used for testing
  // purposes, so we don't care about memory fragmentation
  protocol_v1_function_t read_function = PROTOCOL_V1_FUNCTION_INIT_ZERO;
  if (!pb_decode(istream, &protocol_v1_function_t_msg, &read_function)) {
    LOG_DBG("Failed decoding function\n");
    return false;
  }

  function_t* f = k_calloc(1, sizeof(function_t));
  f->id = read_function.id;
  str_move(&f->name, &read_function.name);
  str_move(&f->description, &read_function.description);
  f->arg_type = (func_arg_type_t)read_function.arg_type;

  sys_slist_append(list, &f->node);

  return true;
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
  if (field->tag == PROTOCOL_V1_CAPABILITIES_SENSORS_TAG) {
    if (ostream) {
      return pb_callback_encode_sensor_list(ostream, field);
    } else if (istream) {
      return pb_callback_decode_sensor_to_list(istream, field);
    } else {
      LOG_WRN("both istream and ostream are NULL");
      return false;
    }
  }

  if (field->tag == PROTOCOL_V1_CAPABILITIES_FUNCTIONS_TAG) {
    if (ostream) {
      return pb_callback_encode_function_list(ostream, field);
    } else if (istream) {
      return pb_callback_decode_function_to_list(istream, field);
    } else {
      LOG_WRN("both istream and ostream are NULL");
      return false;
    }
  }

  if (field->tag == PROTOCOL_V1_CAPABILITIES_DEVICE_NAME_TAG) {
    return pb_callback_codec_str(istream, ostream, field);
  }

  LOG_WRN("Unknown or invalid (field with no callback) field tag: %i\n", field->tag);
  return false;
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

      pb_ul.payload.capabilities.sensors = ul->payload.capabilities.sensors;
      pb_ul.payload.capabilities.functions = ul->payload.capabilities.functions;
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

// NOTE: only for testing
result_t decode_uplink(uplink_t *out, const char* buf_in, size_t buf_size) {
  LOG_DBG("Decoding uplink\n");
  pb_istream_t istream = pb_istream_from_buffer(buf_in, buf_size);

  protocol_v1_uplink_t pb_ul = PROTOCOL_V1_UPLINK_INIT_ZERO;

  if (!pb_decode(&istream, &protocol_v1_uplink_t_msg, &pb_ul)) {
    return ERROR;
  }

  out->payload_type = pb_ul.which_payload;
  switch (out->payload_type) {
    case UPLINK_PAYLOAD_TYPE_REPORT:
      out->payload.report.sensor_id = pb_ul.payload.report.sensor_id;
      out->payload.report.which_value = pb_ul.payload.report.which_value;
      switch (out->payload.report.which_value) {
        case REPORT_VALUE_INT:
          out->payload.report.value.int_val = pb_ul.payload.report.value.int_val;
          break;
        case REPORT_VALUE_FLOAT:
          out->payload.report.value.float_val = pb_ul.payload.report.value.float_val;
          break;
        case REPORT_VALUE_STRING:
          str_move(&out->payload.report.value.str_val, &pb_ul.payload.report.value.str_val);
          break;
        case REPORT_VALUE_BOOL:
          out->payload.report.value.bool_val = pb_ul.payload.report.value.bool_val;
          break;
        default:
          LOG_WRN("Unknown or invalid report value type\n");
          return ERROR;
      }
      break;
    case UPLINK_PAYLOAD_TYPE_CAPABILITIES:
      str_move(&out->payload.capabilities.device_name, &pb_ul.payload.capabilities.device_name);
      out->payload.capabilities.sensors = pb_ul.payload.capabilities.sensors;
      out->payload.capabilities.functions = pb_ul.payload.capabilities.functions;

      // initialize it for testing purposes
      out->payload.capabilities.device_class = DEVICE_CLASS_A; 
      break;
    case UPLINK_PAYLOAD_TYPE_PING:
      out->payload.ping.type = (up_ping_type_t)pb_ul.payload.ping.type;
      break;
    default:
      LOG_WRN("Unknown or invalid uplink payload type\n");
      return ERROR;
  }

  LOG_DBG("Correctly decoded\n");
  return OK;
}

// NOTE: only for testing
void uplink_free(uplink_t* ul) {
  switch (ul->payload_type) {
    case UPLINK_PAYLOAD_TYPE_REPORT:
      if (ul->payload.report.which_value == REPORT_VALUE_STRING) {
        str_free(&ul->payload.report.value.str_val);
      }
      break;
    case UPLINK_PAYLOAD_TYPE_CAPABILITIES: {
      capabilities_t* c = &ul->payload.capabilities;
      str_free(&c->device_name);

      sys_snode_t* n;
      while ((n = sys_slist_get(&c->sensors)) != NULL) {
        sensor_t* s = CONTAINER_OF(n, sensor_t, node);
        str_free(&s->name);
        str_free(&s->description);
        str_free(&s->unit);
        k_free(s);
      }
      while ((n = sys_slist_get(&c->functions)) != NULL) {
        function_t* f = CONTAINER_OF(n, function_t, node);
        str_free(&f->name);
        str_free(&f->description);
        k_free(f);
      }
      break;
    }
    default:
      break;
  }
}


// NOTE: only for testing
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
