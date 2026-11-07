#include <errno.h>
#include <string.h>
#include <zephyr/fff.h>
#include <zephyr/ztest.h>

#include "loris/lorawan.h"

FAKE_VALUE_FUNC(int, lorawan_set_region, enum lorawan_region);
FAKE_VALUE_FUNC(int, lorawan_start);
FAKE_VALUE_FUNC(int, lorawan_join, const struct lorawan_join_config*);
FAKE_VALUE_FUNC(
    int,
    lorawan_send,
    uint8_t,
    uint8_t*,
    uint8_t,
    enum lorawan_message_type
);
FAKE_VALUE_FUNC(int, lorawan_request_link_check, bool);
FAKE_VOID_FUNC(lorawan_register_downlink_callback, struct lorawan_downlink_cb*);

typedef uint8_t (*battery_cb_t)(void);
typedef void (*dr_changed_cb_t)(enum lorawan_datarate);
typedef void (*link_check_ans_cb_t)(uint8_t, uint8_t);

FAKE_VOID_FUNC(lorawan_register_battery_level_callback, battery_cb_t);
FAKE_VOID_FUNC(lorawan_register_dr_changed_callback, dr_changed_cb_t);
FAKE_VOID_FUNC(lorawan_register_link_check_ans_callback, link_check_ans_cb_t);
FAKE_VOID_FUNC(lorawan_enable_adr, bool);
FAKE_VALUE_FUNC(int, lorawan_set_datarate, enum lorawan_datarate);

static struct lorawan_join_config captured_join_cfg;

static int custom_fake_lorawan_join(const struct lorawan_join_config* cfg) {
  if (cfg != NULL) {
    captured_join_cfg = *cfg;
  }
  return lorawan_join_fake.return_val;
}

static void before_test(void* fixture) {
  RESET_FAKE(lorawan_set_region);
  RESET_FAKE(lorawan_start);
  RESET_FAKE(lorawan_join);
  RESET_FAKE(lorawan_send);
  RESET_FAKE(lorawan_request_link_check);
  RESET_FAKE(lorawan_register_downlink_callback);
  RESET_FAKE(lorawan_register_battery_level_callback);
  RESET_FAKE(lorawan_register_dr_changed_callback);
  RESET_FAKE(lorawan_register_link_check_ans_callback);
  RESET_FAKE(lorawan_enable_adr);
  RESET_FAKE(lorawan_set_datarate);

  FFF_RESET_HISTORY();

  memset(&captured_join_cfg, 0, sizeof(captured_join_cfg));

  lorawan_set_region_fake.return_val = 0;
  lorawan_start_fake.return_val = 0;
  lorawan_join_fake.return_val = 0;
  lorawan_join_fake.custom_fake = custom_fake_lorawan_join;
  lorawan_request_link_check_fake.return_val = 0;
}

ZTEST_SUITE(lorawan_suite, NULL, NULL, before_test, NULL, NULL);

ZTEST(lorawan_suite, test_otaa_join_increments_devnonce) {
  uint8_t dev_eui[8] = {0x01};
  uint8_t join_eui[8] = {0x02};
  uint8_t app_key[16] = {0x03};
  uint8_t nwk_key[16] = {0x04};

  lr_lw_join_otaa_config_t cfg = {
      .dev_eui = dev_eui,
      .join_eui = join_eui,
      .app_key = app_key,
      .nwk_key = nwk_key,
  };

  zassert_equal(lr_lw_join_otaa(&cfg, LORAWAN_REGION_EU868), LR_LW_OK);
  uint16_t first_nonce = captured_join_cfg.otaa.dev_nonce;

  zassert_equal(lr_lw_join_otaa(&cfg, LORAWAN_REGION_EU868), LR_LW_OK);
  uint16_t second_nonce = captured_join_cfg.otaa.dev_nonce;

  zassert_equal(second_nonce, (uint16_t)(first_nonce + 1));
}

typedef struct join_failure_case {
  int set_region_ret;
  int start_ret;
  int join_ret;
  lr_lw_result_t expected_res;
} join_failure_case_t;

// clang-format off
static const join_failure_case_t join_failure_cases[] = {
    {.set_region_ret = -1, .start_ret = 0, .join_ret = 0, .expected_res = LR_LW_ERR_SET_REGION_FAILED},
    {.set_region_ret = 0, .start_ret = -1, .join_ret = 0, .expected_res = LR_LW_ERR_START_FAILED},
    {.set_region_ret = 0, .start_ret = 0, .join_ret = -1, .expected_res = LR_LW_ERR_JOIN_FAILED},
};
// clang-format on

ZTEST_DEFINE_PARAM_VALUES_ARRAY(join_failure_configs, join_failure_cases);
ZTEST_INSTANTIATE_TEST_SUITE_P(
    join_failures,
    lorawan_suite,
    test_join_failures,
    join_failure_configs
);

ZTEST_P(lorawan_suite, test_join_failures) {
  join_failure_case_t tc = ZTEST_GET_PARAM(join_failure_case_t);

  lorawan_set_region_fake.return_val = tc.set_region_ret;
  lorawan_start_fake.return_val = tc.start_ret;
  lorawan_join_fake.return_val = tc.join_ret;

  uint8_t dev_eui[8] = {0};
  uint8_t join_eui[8] = {0};
  uint8_t app_key[16] = {0};
  uint8_t nwk_key[16] = {0};

  lr_lw_join_otaa_config_t cfg = {
      .dev_eui = dev_eui,
      .join_eui = join_eui,
      .app_key = app_key,
      .nwk_key = nwk_key,
  };

  lr_lw_result_t res = lr_lw_join_otaa(&cfg, (enum lorawan_region)0);
  zassert_equal(res, tc.expected_res, "res = %i", res);
}

ZTEST(lorawan_suite, test_abp_join_success) {
  uint8_t dev_eui[8] = {0x01};
  uint8_t app_skey[16] = {0x02};
  uint8_t nwk_skey[16] = {0x03};
  uint8_t app_eui[8] = {0x04};

  lr_lw_join_abp_config_t cfg = {
      .dev_eui = dev_eui,
      .dev_addr = 0x12345678,
      .app_skey = app_skey,
      .nwk_skey = nwk_skey,
      .app_eui = app_eui,
  };

  zassert_equal(lr_lw_join_abp(&cfg, (enum lorawan_region)0), LR_LW_OK);
  zassert_equal(lorawan_join_fake.call_count, 1);
  zassert_equal(captured_join_cfg.mode, LORAWAN_ACT_ABP);
  zassert_equal(captured_join_cfg.dev_eui, dev_eui);
  zassert_equal(captured_join_cfg.abp.dev_addr, cfg.dev_addr);
  zassert_equal(captured_join_cfg.abp.app_skey, app_skey);
  zassert_equal(captured_join_cfg.abp.nwk_skey, nwk_skey);
  zassert_equal(captured_join_cfg.abp.app_eui, app_eui);
}

typedef struct send_test_case {
  int lorawan_ret;
  lr_lw_result_t expected_res;
} send_test_case_t;

// clang-format off
static const send_test_case_t send_cases[] = {
    {.lorawan_ret = 0, .expected_res = LR_LW_OK},
    {.lorawan_ret = -EAGAIN, .expected_res = LR_LW_ERR_DUTY_CYCLE_EXCEEDED},
    {.lorawan_ret = -ECONNREFUSED, .expected_res = LR_LW_ERR_DUTY_CYCLE_EXCEEDED },
    {.lorawan_ret = -EIO, .expected_res = LR_LW_ERR_SEND_FAILED},
};
// clang-format on

ZTEST_DEFINE_PARAM_VALUES_ARRAY(send_configs, send_cases);
ZTEST_INSTANTIATE_TEST_SUITE_P(
    send_suite,
    lorawan_suite,
    test_lr_lw_send_status,
    send_configs
);

ZTEST_P(lorawan_suite, test_lr_lw_send_status) {
  send_test_case_t tc = ZTEST_GET_PARAM(send_test_case_t);
  lorawan_send_fake.return_val = tc.lorawan_ret;

  uint8_t payload[] = {0xDE, 0xAD, 0xBE, 0xEF};
  zassert_equal(
      lr_lw_send(10, payload, sizeof(payload), LORAWAN_MSG_UNCONFIRMED),
      tc.expected_res
  );
  zassert_equal(lorawan_send_fake.arg0_val, 10);
  zassert_equal(lorawan_send_fake.arg1_val, payload);
  zassert_equal(lorawan_send_fake.arg2_val, sizeof(payload));
  zassert_equal(lorawan_send_fake.arg3_val, LORAWAN_MSG_UNCONFIRMED);
}

typedef struct dr_test_case {
  int lorawan_ret;
  lr_lw_result_t expected_res;
} dr_test_case_t;

// clang-format off
static const dr_test_case_t dr_cases[] = {
    {.lorawan_ret = 0, .expected_res = LR_LW_OK},
    {.lorawan_ret = -EINVAL, .expected_res = LR_LW_INVALID_DATARATE_OR_ADR_ENABLED},
    {.lorawan_ret = -50, .expected_res = LR_LW_ERR_UNKNOWN},
};
// clang-format on

ZTEST_DEFINE_PARAM_VALUES_ARRAY(dr_configs, dr_cases);
ZTEST_INSTANTIATE_TEST_SUITE_P(
    dr_suite,
    lorawan_suite,
    test_lr_lw_set_datarate_status,
    dr_configs
);

ZTEST_P(lorawan_suite, test_lr_lw_set_datarate_status) {
  dr_test_case_t tc = ZTEST_GET_PARAM(dr_test_case_t);
  lorawan_set_datarate_fake.return_val = tc.lorawan_ret;

  zassert_equal(lr_lw_set_datarate((enum lorawan_datarate)2), tc.expected_res);
  zassert_equal(lorawan_set_datarate_fake.arg0_val, (enum lorawan_datarate)2);
}

ZTEST(lorawan_suite, test_request_link_check) {
  lr_lw_request_link_check(true);
  zassert_equal(lorawan_request_link_check_fake.call_count, 1);
  zassert_equal(lorawan_request_link_check_fake.arg0_val, true);

  lr_lw_request_link_check(false);
  zassert_equal(lorawan_request_link_check_fake.call_count, 2);
  zassert_equal(lorawan_request_link_check_fake.arg0_val, false);
}

static void mock_downlink_cb(
    uint8_t port,
    uint8_t flags,
    int16_t rssi,
    int8_t snr,
    uint8_t len,
    const uint8_t* data
) {}
static uint8_t mock_battery_cb(void) { return 254; }
static void mock_dr_changed_cb(enum lorawan_datarate dr) {}
static void mock_link_check_ans_cb(uint8_t demod_margin, uint8_t nb_gateways) {}

ZTEST(lorawan_suite, test_register_downlink_callback) {
  struct lorawan_downlink_cb dummy = {0};
  lr_lw_register_downlink_callback(&dummy, mock_downlink_cb);

  zassert_equal(lorawan_register_downlink_callback_fake.call_count, 1);
  zassert_equal(dummy.port, LW_RECV_PORT_ANY);
  zassert_equal(dummy.cb, mock_downlink_cb);
}

ZTEST(lorawan_suite, test_register_other_callbacks) {
  lr_lw_register_battery_lvl_callback(mock_battery_cb);
  zassert_equal(lorawan_register_battery_level_callback_fake.call_count, 1);
  zassert_equal(
      lorawan_register_battery_level_callback_fake.arg0_val,
      mock_battery_cb
  );

  lr_lw_register_dr_changed_callback(mock_dr_changed_cb);
  zassert_equal(lorawan_register_dr_changed_callback_fake.call_count, 1);
  zassert_equal(
      lorawan_register_dr_changed_callback_fake.arg0_val,
      mock_dr_changed_cb
  );

  lr_lw_register_link_check_ans_callback(mock_link_check_ans_cb);
  zassert_equal(lorawan_register_link_check_ans_callback_fake.call_count, 1);
  zassert_equal(
      lorawan_register_link_check_ans_callback_fake.arg0_val,
      mock_link_check_ans_cb
  );
}

ZTEST(lorawan_suite, test_set_adr) {
  lr_lw_set_adr_mode(true);
  zassert_equal(lorawan_enable_adr_fake.call_count, 1);
  zassert_equal(lorawan_enable_adr_fake.arg0_val, true);

  lr_lw_set_adr_mode(false);
  zassert_equal(lorawan_enable_adr_fake.call_count, 2);
  zassert_equal(lorawan_enable_adr_fake.arg0_val, false);
}
