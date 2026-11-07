/*
 * Copyright (c) 2026 Tomasz Gadziński
 *
 * SPDX-License-Identifier: MIT
 */
#include <zephyr/logging/log.h>
#include <zephyr/lorawan/lorawan.h>

#include "loris/lorawan.h"

#define LORAWAN_DEV_EUI {0x52, 0xa6, 0x13, 0x65, 0x7c, 0xb5, 0x29, 0xee}
#define LORAWAN_JOIN_EUI {0xb1, 0xaa, 0x7f, 0xb2, 0xd5, 0x24, 0x69, 0xab}
#define LORAWAN_APP_KEY                                                        \
  {0x36,                                                                       \
   0xc7,                                                                       \
   0x98,                                                                       \
   0xaa,                                                                       \
   0x69,                                                                       \
   0xcf,                                                                       \
   0xc9,                                                                       \
   0xab,                                                                       \
   0xdf,                                                                       \
   0x49,                                                                       \
   0xd2,                                                                       \
   0xcf,                                                                       \
   0xd6,                                                                       \
   0x71,                                                                       \
   0xd7,                                                                       \
   0x15}

LOG_MODULE_REGISTER(lorawan_main_test);

#define DUTYCYCLE_REST_SECONDS 120
#define REJOIN_RETRY_SECONDS 5
#define MSG_TIMER_SECONDS 360

char data[] = "Hello from the loris LoRaWAN test!";

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

static void datarate_changed_cb(enum lorawan_datarate dr) {
  uint8_t unused, max_size;

  lorawan_get_payload_sizes(&unused, &max_size);
  LOG_INF("New Datarate: DR_%d, Max Payload %d", dr, max_size);
}

static void link_check_ans_cb(uint8_t demod_margin, uint8_t nb_gateways) {
  LOG_INF("Link check: margin %u dB, %u gateway(s)", demod_margin, nb_gateways);
}

void send_countdown(int countdown_secs) {
  char spinner[] = {'|', '/', '-', '\\'};
  for (int i = 0; i < countdown_secs; i++) {
    size_t idx = i % (sizeof(spinner) / sizeof(spinner[0]));
    printf(
        "[%c] Sending another message in %3i seconds\r",
        spinner[idx],
        countdown_secs - i
    );
    k_sleep(K_SECONDS(1));
  }
}

int main() {
  struct lorawan_downlink_cb dl_cb = {0};
  lr_lw_register_downlink_callback(&dl_cb, &dl_callback);
  lr_lw_register_dr_changed_callback(&datarate_changed_cb);
  lr_lw_link_check_ans_callback(&link_check_ans_cb);

  uint8_t dev_eui[] = LORAWAN_DEV_EUI;
  uint8_t join_eui[] = LORAWAN_JOIN_EUI;
  uint8_t app_key[] = LORAWAN_APP_KEY;

  lr_lw_join_otaa_config_t cfg = {
      .dev_eui = dev_eui,
      .join_eui = join_eui,
      .app_key = app_key,
      .nwk_key = app_key,
  };

  if (lr_lw_set_datarate(LORAWAN_DR_3) != LR_LW_OK) {
    LOG_ERR("Failed to set datarate!");
  }

  lr_lw_set_adr_mode(true);

  while (lr_lw_join_otaa(&cfg, LORAWAN_REGION_EU868) != LR_LW_OK) {
    LOG_ERR("Joining LoRaWAN failed. Trying again in %i", REJOIN_RETRY_SECONDS);
    k_sleep(K_SECONDS(REJOIN_RETRY_SECONDS));
  }

  while (true) {
    LOG_INF("Sending packet!");
    lr_lw_result_t ret =
        lr_lw_send(1, data, sizeof(data), LORAWAN_MSG_CONFIRMED);

    if (ret == LR_LW_ERR_DUTY_CYCLE_EXCEEDED) {
      LOG_ERR("Sending message failed: duty cycle exceeded!");
      LOG_INF(
          "Will retry after a longer cooldown of %i seconds",
          DUTYCYCLE_REST_SECONDS
      );
      send_countdown(DUTYCYCLE_REST_SECONDS);
    }

    if (ret != LR_LW_OK) {
      LOG_ERR("Sending message failed!");
    }

    LOG_INF("Next message will be sent after %i seconds", MSG_TIMER_SECONDS);
    send_countdown(MSG_TIMER_SECONDS);
  }

  return 0;
}
