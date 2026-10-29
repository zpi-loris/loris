#include <zephyr/ztest.h>

#include "loris/codec.h"

void assert_calls_equal(const call_t* c1, const call_t* c2) {
  zassert_equal(c1->function_id, c2->function_id);
  zassert_equal(c1->has_wake_after_secs, c2->has_wake_after_secs);

  if (c1->has_wake_after_secs) {
    zassert_equal(c1->wake_after_secs, c2->wake_after_secs);
  }

  zassert_equal(c1->which_value, c2->which_value);

  switch (c1->which_value) {
    case CALL_VALUE_INT:
      zassert_equal(c1->value.int_val, c2->value.int_val);
      break;
    case CALL_VALUE_FLOAT:
      zassert_equal(c1->value.float_val, c2->value.float_val);
      break;
    case CALL_VALUE_STRING:
      zassert_str_equal(c1->value.str_val.ptr, c2->value.str_val.ptr);
      break;
    case CALL_VALUE_BOOL:
      zassert_equal(c1->value.bool_val, c2->value.bool_val);
      break;
  }
}

void mk_call_downlink(downlink_t* out, call_value_type_t type, void* value) {
  out->payload_type = DOWNLINK_PAYLOAD_TYPE_CALL;
  out->payload.call.function_id = 2;
  out->payload.call.which_value = type;
  switch (type) {
    case CALL_VALUE_INT:
      out->payload.call.value.int_val = *(int*)value;
      break;
    case CALL_VALUE_FLOAT:
      out->payload.call.value.float_val = *(float*)value;
      break;
    case CALL_VALUE_STRING:
      out->payload.call.value.str_val = *(str_t*)value;
      break;
    case CALL_VALUE_BOOL:
      out->payload.call.value.bool_val = *(bool*)value;
      break;
  }
}

ZTEST_SUITE(codec_downlink, NULL, NULL, NULL, NULL, NULL );

typedef struct call_downlink_conf {
    call_value_type_t type;
    union {
        int int_val;
        float float_val;
        str_t str_val;
        bool bool_val;
    } value;
} call_downlink_conf_t;

static const call_downlink_conf_t confs[] = {
    {
        .type = CALL_VALUE_STRING,
        .value.str_val = {
            .ptr = "hehe",
            .len = 4,
            .src_type = STR_SRC_STATIC,
        }
    },
    {
        .type = CALL_VALUE_INT,
        .value.int_val = 4,
    },
    {
        .type = CALL_VALUE_FLOAT,
        .value.float_val = 3.14f,
    },
    {
        .type = CALL_VALUE_BOOL,
        .value.bool_val = true,
    }
};

ZTEST_DEFINE_PARAM_VALUES_ARRAY(call_downlink_confs, confs);
ZTEST_INSTANTIATE_TEST_SUITE_P(calls, codec_downlink, test_encodes_and_decodes_call, call_downlink_confs);
ZTEST_P(codec_downlink, test_encodes_and_decodes_call) {
  downlink_t dl = {0};
  call_downlink_conf_t conf = ZTEST_GET_PARAM(call_downlink_conf_t);
  mk_call_downlink(&dl, conf.type, &conf.value);

  char buf[1024];
  size_t bytes_written = 0;
  zassert_equal(encode_downlink(buf, sizeof(buf), &dl, &bytes_written), OK);
  zassert_not_equal(bytes_written, 0);

  downlink_t decoded = {0};
  zassert_equal(decode_downlink(&decoded, buf, bytes_written), OK);

  zassert_equal(dl.payload_type, decoded.payload_type);
  assert_calls_equal(&dl.payload.call, &decoded.payload.call);
}

static const down_ping_type_t downping_types[] = {
  DOWN_PING_TYPE_UNSPECIFIED,
  DOWN_PING_TYPE_DISCONNECT,
  DOWN_PING_TYPE_HELLO,
};

ZTEST_DEFINE_PARAM_VALUES_ARRAY(down_ping_types, downping_types);
ZTEST_INSTANTIATE_TEST_SUITE_P(downpingtypes, codec_downlink, test_encodes_and_decodes_ping, down_ping_types);
ZTEST_P(codec_downlink, test_encodes_and_decodes_ping) {
  down_ping_type_t type = ZTEST_GET_PARAM(down_ping_type_t);
  downlink_t dl = {
    .payload_type = DOWNLINK_PAYLOAD_TYPE_PING,
    .payload.ping = {
      .type = type,
    }
  };

  char buf[1024];
  size_t bytes_written = 0;
  zassert_equal(encode_downlink(buf, sizeof(buf), &dl, &bytes_written), OK);
  zassert_not_equal(bytes_written, 0);

  downlink_t decoded = {0};
  zassert_equal(decode_downlink(&decoded, buf, bytes_written), OK);
  zassert_equal(decoded.payload_type, DOWNLINK_PAYLOAD_TYPE_PING);
  zassert_equal(decoded.payload.ping.type, type);
}

// ZTEST_SUITE(uplink, NULL, NULL, NULL, NULL, NULL);
//
// ZTEST(uplink, test_encodes_and_decodes_report) {
//   uplink_t ul = {
//     .payload_type = UPLINK_PAYLOAD_TYPE_REPORT,
//     .payload.report = (report_t){
//       .sensor_id = 2,
//       .which_value = REPORT_VALUE_STRING,
//       .value.str_val = (str_t){
//         .ptr = "hello",
//         .len = 5,
//         .src_type = STR_SRC_STATIC
//       }
//     }
//   };
//
//   char buf[1024];
//   size_t bytes_written = 0;
//   zassert_equal(encode_uplink(buf, &ul, 1024, &bytes_written), OK);
//   // TODO: how do i even test this shit
// }
//
// ZTEST(uplink, test_above_cap) {
//
// }
//
