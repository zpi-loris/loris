#include <zephyr/logging/log.h>
#include <zephyr/lorawan/lorawan.h>

#include "loris/lorawan.h"
#include "loris/loris.h"
#include "loris/models/capabilities.h"
#include "zephyr/logging/log_core.h"

#define LORAWAN_DEV_EUI { 0x5b, 0xd1, 0x3f, 0x6a, 0xd9, 0x5e, 0xe2, 0x1c }
#define LORAWAN_JOIN_EUI { 0x52, 0xf1, 0xcc, 0xb8, 0x84, 0xf6, 0xdc, 0x7c }
#define LORAWAN_APP_KEY                                                        \
  { 0xd9, 0xc7, 0xae, 0xd4, 0x2c, 0x8e, 0x86, 0xed,                            \
    0x21, 0x8e, 0x13, 0xf3, 0x3f, 0x86, 0xeb, 0x38 }

LOG_MODULE_REGISTER(loris_main_test, LOG_LEVEL_DBG);

#define REJOIN_RETRY_SECONDS 30

static void dl_callback(
    uint8_t port,
    uint8_t flags,
    int16_t rssi,
    int8_t snr,
    uint8_t len,
    const uint8_t* hex_data
) {
  LOG_INF("Port %d, RSSI %ddB, SNR %ddBm", port, rssi, snr);

  if (flags & LORAWAN_TIME_UPDATED) {
    /* The stack has synced the network time from a DeviceTimeAns. */
    LOG_INF("Network time updated");
  }

  if (flags & LORAWAN_DATA_PENDING) {
    /* The network server has more downlinks queued.  An application
     * that wants to drain them promptly can schedule another uplink
     * (e.g. an empty one on any port) here.
     */
    LOG_INF("Network reports more data pending (FPending set)");
  }

  if (hex_data) {
    LOG_HEXDUMP_INF(hex_data, len, "Payload: ");
  }
}

report_t temp_report(sensor_t* sensor) {
  report_t sample_report = { .sensor_id = 1,
                             .value.float_val = 22.5,
                             .which_value = REPORT_VALUE_FLOAT };
  return sample_report;
}

int main() {
  struct lorawan_downlink_cb dl_cb = { 0 };
  lr_lw_register_downlink_callback(&dl_cb, &dl_callback);
  lr_init_loris();

  uint8_t dev_eui[] = LORAWAN_DEV_EUI;
  uint8_t join_eui[] = LORAWAN_JOIN_EUI;
  uint8_t app_key[] = LORAWAN_APP_KEY;

  lr_lw_join_otaa_config_t cfg = {
    .dev_eui = dev_eui,
    .join_eui = join_eui,
    .app_key = app_key,
    .nwk_key = app_key,
  };

  while (lr_lw_join_otaa(&cfg, LORAWAN_REGION_EU868) != LR_LW_OK) {
    LOG_ERR("Joining LoRaWAN failed. Trying again in %i", REJOIN_RETRY_SECONDS);
    k_sleep(K_SECONDS(REJOIN_RETRY_SECONDS));
  }

  k_msleep(500);

  lr_lw_set_adr_mode(false);
  if (lr_lw_set_datarate(LORAWAN_DR_3) != LR_LW_OK) {
    LOG_ERR("Failed to set datarate!");
  }
  lr_lw_set_adr_mode(true);

  capabilities_t capabilities;

  capabilities_init(&capabilities, "loris_sample", DEVICE_CLASS_A);
  capabilities_add_sensor(
      &capabilities,
      "sample_sensor",
      "Measures temperature in the garden",
      VALUE_TYPE_TEMPERATURE,
      "C",
      &temp_report,
      10
  );

  LOG_INF("Starting main loop");
  lr_start(&capabilities);

  return 0;
}
