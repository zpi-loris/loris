#include <zephyr/ztest.h>

#include "loris/codec.h"

ZTEST_SUITE(codec_downlink, NULL, NULL, NULL, NULL, NULL );

void assert_downlinks_equal(const downlink_t* d1, const downlink_t* d2) {
  zassert_equal(d1->payload_type, d2->payload_type);
  switch (d1->payload_type) {
    case DOWNLINK_PAYLOAD_TYPE_CALL:
      zassert_equal(d1->payload.call.function_id, d2->payload.call.function_id);
      zassert_equal(d1->payload.call.has_wake_after_secs, d2->payload.call.has_wake_after_secs);

      if (d1->payload.call.has_wake_after_secs) {
        zassert_equal(d1->payload.call.wake_after_secs, d2->payload.call.wake_after_secs);
      }

      zassert_equal(d1->payload.call.which_value, d2->payload.call.which_value);

      switch (d1->payload.call.which_value) {
        case CALL_VALUE_INT:
          zassert_equal(d1->payload.call.value.int_val, d2->payload.call.value.int_val);
          break;
        case CALL_VALUE_FLOAT:
          zassert_equal(d1->payload.call.value.float_val, d2->payload.call.value.float_val);
          break;
        case CALL_VALUE_STRING:
          zassert_str_equal(d1->payload.call.value.str_val.ptr, d2->payload.call.value.str_val.ptr);
          break;
        case CALL_VALUE_BOOL:
          zassert_equal(d1->payload.call.value.bool_val, d2->payload.call.value.bool_val);
          break;
      }
      break;
    case DOWNLINK_PAYLOAD_TYPE_PING:
      zassert_equal(d1->payload.ping.type, d2->payload.ping.type);
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


typedef struct call_downlink_conf {
    call_value_type_t type;
    union {
        int int_val;
        float float_val;
        str_t str_val;
        bool bool_val;
    } value;
} call_downlink_conf_t;

static const call_downlink_conf_t call_confs[] = {
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

ZTEST_DEFINE_PARAM_VALUES_ARRAY(call_downlink_confs, call_confs);
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
  assert_downlinks_equal(&dl, &decoded);
}

static const down_ping_type_t downping_types[] = {
  DOWN_PING_TYPE_UNSPECIFIED,
  DOWN_PING_TYPE_DISCONNECT,
  DOWN_PING_TYPE_HELLO,
};

ZTEST_DEFINE_PARAM_VALUES_ARRAY(down_ping_types, downping_types);
ZTEST_INSTANTIATE_TEST_SUITE_P(downpingtypes, codec_downlink, test_encodes_and_decodes_downping, down_ping_types);
ZTEST_P(codec_downlink, test_encodes_and_decodes_downping) {
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
  zassert_true(bytes_written > 0);

  downlink_t decoded = {0};
  zassert_equal(decode_downlink(&decoded, buf, bytes_written), OK);
  assert_downlinks_equal(&dl, &decoded);
}

ZTEST_SUITE(codec_uplink, NULL, NULL, NULL, NULL, NULL);

void assert_sensors_equal(const sensor_t* s1, const sensor_t* s2) {
  zassert_equal(s1->id, s2->id);
  zassert_str_equal(s1->name.ptr, s2->name.ptr);
  zassert_str_equal(s1->description.ptr, s2->description.ptr);
  zassert_equal(s1->value_type, s2->value_type);
  zassert_equal(s1->has_unit, s2->has_unit);
  zassert_str_equal(s1->unit.ptr, s2->unit.ptr);
  zassert_equal(s1->wake_interval_sec, s2->wake_interval_sec);
}

void assert_functions_equal(const function_t* f1, const function_t* f2) {
  zassert_equal(f1->id, f2->id);
  zassert_str_equal(f1->name.ptr, f2->name.ptr);
  zassert_str_equal(f1->description.ptr, f2->description.ptr);
  zassert_equal(f1->arg_type, f2->arg_type);
  zassert_equal(f1->wake_interval_sec, f2->wake_interval_sec);
}

void assert_capabilities_equal(const capabilities_t* c1, const capabilities_t* c2) {
  zassert_str_equal(c1->device_name.ptr, c2->device_name.ptr);
  zassert_equal(c1->device_class, c2->device_class);

  // sensors
  zassert_equal(sys_slist_len(&c1->sensors), sys_slist_len(&c2->sensors));

  sys_snode_t *node1 = sys_slist_peek_head(&c1->sensors);
  sys_snode_t *node2 = sys_slist_peek_head(&c2->sensors);

  while (node1 != NULL && node2 != NULL) {
    sensor_t *s1 = CONTAINER_OF(node1, sensor_t, node);
    sensor_t *s2 = CONTAINER_OF(node2, sensor_t, node);

    assert_sensors_equal(s1, s2);

    node1 = sys_slist_peek_next(node1);
    node2 = sys_slist_peek_next(node2);
  }

  // functions
  zassert_equal(sys_slist_len(&c1->functions), sys_slist_len(&c2->functions));

  node1 = sys_slist_peek_head(&c1->functions);
  node2 = sys_slist_peek_head(&c2->functions);

  while (node1 != NULL && node2 != NULL) {
    function_t *f1 = CONTAINER_OF(node1, function_t, node);
    function_t *f2 = CONTAINER_OF(node2, function_t, node);

    assert_functions_equal(f1, f2);

    node1 = sys_slist_peek_next(node1);
    node2 = sys_slist_peek_next(node2);
  }

}

void assert_uplinks_equal(const uplink_t* u1, const uplink_t* u2) {
  zassert_equal(u1->payload_type, u2->payload_type);
  switch (u1->payload_type) {
    case UPLINK_PAYLOAD_TYPE_REPORT:
      zassert_equal(u1->payload.report.sensor_id, u2->payload.report.sensor_id);
      zassert_equal(u1->payload.report.which_value, u2->payload.report.which_value);
      switch (u1->payload.report.which_value) {
        case REPORT_VALUE_INT:
          zassert_equal(u1->payload.report.value.int_val, u2->payload.report.value.int_val);
          break;
        case REPORT_VALUE_FLOAT:
          zassert_equal(u1->payload.report.value.float_val, u2->payload.report.value.float_val);
          break;
        case REPORT_VALUE_STRING:
          zassert_str_equal(u1->payload.report.value.str_val.ptr, u2->payload.report.value.str_val.ptr);
          break;
        case REPORT_VALUE_BOOL:
          zassert_equal(u1->payload.report.value.bool_val, u2->payload.report.value.bool_val);
          break;
      }
      break;
    case UPLINK_PAYLOAD_TYPE_CAPABILITIES:
      assert_capabilities_equal(&u1->payload.capabilities, &u2->payload.capabilities);
      break;
    case UPLINK_PAYLOAD_TYPE_PING:
      zassert_equal(u1->payload.ping.type, u2->payload.ping.type);
      break;
  }
}

void mk_report_uplink(uplink_t* out, report_value_type_t type, void* value) {
  out->payload_type = UPLINK_PAYLOAD_TYPE_REPORT;
  out->payload.report.sensor_id = 2;
  out->payload.report.which_value = type;
  switch (type) {
    case REPORT_VALUE_INT:
      out->payload.report.value.int_val = *(int*)value;
      break;
    case REPORT_VALUE_FLOAT:
      out->payload.report.value.float_val = *(float*)value;
      break;
    case REPORT_VALUE_STRING:
      out->payload.report.value.str_val = *(str_t*)value;
      break;
    case REPORT_VALUE_BOOL:
      out->payload.report.value.bool_val = *(bool*)value;
      break;
  }
}

static const up_ping_type_t upts[] = {
  UP_PING_TYPE_UNSPECIFIED,
  UP_PING_TYPE_HELLO,
};

ZTEST_DEFINE_PARAM_VALUES_ARRAY(upping_types, upts);
ZTEST_INSTANTIATE_TEST_SUITE_P(upping_types, codec_uplink, test_encodes_and_decodes_upping, upping_types);
ZTEST_P(codec_uplink, test_encodes_and_decodes_upping) {
  up_ping_type_t type = ZTEST_GET_PARAM(up_ping_type_t);
  uplink_t ul = {
    .payload_type = UPLINK_PAYLOAD_TYPE_PING,
    .payload.ping.type = type
  };

  char buf[1024];
  size_t bytes_written = 0;
  zassert_equal(encode_uplink(buf, sizeof(buf), &ul, &bytes_written), OK);
  zassert_not_equal(bytes_written, 0);

  uplink_t decoded = {0};
  zassert_equal(decode_uplink(&decoded, buf, bytes_written), OK);
  zassert_equal(decoded.payload_type, UPLINK_PAYLOAD_TYPE_PING);
  zassert_equal(decoded.payload.ping.type, type);
}

typedef struct report_uplink_conf {
    report_value_type_t type;
    union {
        int int_val;
        float float_val;
        str_t str_val;
        bool bool_val;
    } value;
} report_conf_t;

static const report_conf_t report_confs[] = {
    {
        .type = REPORT_VALUE_STRING,
        .value.str_val = {
            .ptr = "hehe",
            .len = 4,
            .src_type = STR_SRC_STATIC,
        }
    },
    {
        .type = REPORT_VALUE_INT,
        .value.int_val = 4,
    },
    {
        .type = REPORT_VALUE_FLOAT,
        .value.float_val = 3.14f,
    },
    {
        .type = REPORT_VALUE_BOOL,
        .value.bool_val = true,
    }
};


ZTEST_DEFINE_PARAM_VALUES_ARRAY(rep_confs, report_confs);
ZTEST_INSTANTIATE_TEST_SUITE_P(rep_configs, codec_uplink, test_encodes_and_decodes_report, rep_confs);
ZTEST_P(codec_uplink, test_encodes_and_decodes_report) {
  report_conf_t conf = ZTEST_GET_PARAM(report_conf_t);
  uplink_t ul = {0};
  mk_report_uplink(&ul, conf.type, &conf.value);

  char buf[1024];
  size_t bytes_written = 0;
  zassert_equal(encode_uplink(buf, sizeof(buf), &ul, &bytes_written), OK);
  zassert_true(bytes_written > 0);

  uplink_t decoded = {0};
  zassert_equal(decode_uplink(&decoded, buf, bytes_written), OK);
  zassert_equal(ul.payload_type, decoded.payload_type);
}

ZTEST(codec_uplink, test_encodes_and_decodes_capabilities) {
    sensor_t s1 = {
        .id = 1,
        .name = { .ptr = "Temperature", .len = 11, .src_type = STR_SRC_STATIC },
        .description = { .ptr = "Ambient temperature sensor", .len = 26, .src_type = STR_SRC_STATIC },
        .value_type = VALUE_TYPE_TEMPERATURE,
        .has_unit = true,
        .unit = { .ptr = "C", .len = 1, .src_type = STR_SRC_STATIC },
        .read_func = NULL,
        .wake_interval_sec = 60,
    };
    sensor_t s2 = {
        .id = 2,
        .name = { .ptr = "Humidity", .len = 8, .src_type = STR_SRC_STATIC },
        .description = { .ptr = "Relative humidity sensor", .len = 24, .src_type = STR_SRC_STATIC },
        .value_type = VALUE_TYPE_HUMIDITY,
        .has_unit = true,
        .unit = { .ptr = "%", .len = 1, .src_type = STR_SRC_STATIC },
        .read_func = NULL,
        .wake_interval_sec = 120,
    };
    sensor_t s3 = {
        .id = 3,
        .name = { .ptr = "CO2", .len = 3, .src_type = STR_SRC_STATIC },
        .description = { .ptr = "Carbon dioxide concentration", .len = 28, .src_type = STR_SRC_STATIC },
        .value_type = VALUE_TYPE_CO2,
        .has_unit = true,
        .unit = { .ptr = "ppm", .len = 3, .src_type = STR_SRC_STATIC },
        .read_func = NULL,
        .wake_interval_sec = 300,
    };

    function_t f1 = {
        .id = 10,
        .name = { .ptr = "Reboot", .len = 6, .src_type = STR_SRC_STATIC },
        .description = { .ptr = "Reboots the device", .len = 18, .src_type = STR_SRC_STATIC },
        .arg_type = FUNC_ARG_TYPE_VOID,
        .call_function = NULL,
        .wake_interval_sec = 0,
    };
    function_t f2 = {
        .id = 11,
        .name = { .ptr = "SetInterval", .len = 11, .src_type = STR_SRC_STATIC },
        .description = { .ptr = "Sets sensor update interval in seconds", .len = 39, .src_type = STR_SRC_STATIC },
        .arg_type = FUNC_ARG_TYPE_INT,
        .call_function = NULL,
        .wake_interval_sec = 10,
    };

    uplink_t ul = {
        .payload_type = UPLINK_PAYLOAD_TYPE_CAPABILITIES,
        .payload.capabilities = {
            .device_name = {
                .ptr = "WeatherStation_01",
                .len = 17,
                .src_type = STR_SRC_STATIC,
            },
            .device_class = 1,
        }
    };

    sys_slist_init(&ul.payload.capabilities.sensors);
    sys_slist_append(&ul.payload.capabilities.sensors, &s1.node);
    sys_slist_append(&ul.payload.capabilities.sensors, &s2.node);
    sys_slist_append(&ul.payload.capabilities.sensors, &s3.node);

    sys_slist_init(&ul.payload.capabilities.functions);
    sys_slist_append(&ul.payload.capabilities.functions, &f1.node);
    sys_slist_append(&ul.payload.capabilities.functions, &f2.node);

    char buf[1024];
    size_t bytes_written = 0;
    zassert_equal(encode_uplink(buf, sizeof(buf), &ul, &bytes_written), OK);
    zassert_true(bytes_written > 0);

    uplink_t decoded = {0};
    zassert_equal(decode_uplink(&decoded, buf, bytes_written), OK);
    assert_uplinks_equal(&ul, &decoded);
}


ZTEST(codec_uplink, test_above_cap) {
  uplink_t ul = {
    .payload_type = UPLINK_PAYLOAD_TYPE_REPORT,
    .payload.report.sensor_id = 4,
    .payload.report.which_value = REPORT_VALUE_INT,
    .payload.report.value.int_val = 10,
  };

  char buf[2]; // too small, should fail
  size_t bytes_written = 0;
  zassert_equal(encode_uplink(buf, sizeof(buf), &ul, &bytes_written), ERROR);
}

