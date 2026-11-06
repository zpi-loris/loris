#include <zephyr/device.h>
#include <zephyr/logging/log.h>
#include <zephyr/lorawan/lorawan.h>

#include "loris/lorawan.h"
#include "loris/nvs.h"

LOG_MODULE_REGISTER(loris_lw, LOG_LEVEL_DBG);

static lr_lw_result_t join(
    const struct lorawan_join_config* cfg,
    enum lorawan_region region
) {
  LOG_INF("Joining LoRaWAN network");

  const struct device* lora_dev = DEVICE_DT_GET(DT_ALIAS(LORA_DEVICE_ALIAS));

  LOG_INF("Using LoRa device '%s'", lora_dev->name);

  if (!device_is_ready(lora_dev)) {
    LOG_WRN("Device '%s' not ready", lora_dev->name);
    return LR_LW_ERR_DEV_NOT_READY;
  }

  int ret = lorawan_set_region(region);
  if (lorawan_set_region(region) < 0) {
    LOG_WRN("Couldn't set LoRaWAN region: %d", ret);
    return LR_LW_ERR_SET_REGION_FAILED;
  }

  ret = lorawan_start();
  if (ret < 0) {
    LOG_WRN("Error starting LoRaWAN stack: %d", ret);
    return LR_LW_ERR_START_FAILED;
  }

  ret = lorawan_join(cfg);
  if (ret < 0) {
    LOG_WRN("Joining failed: %d", ret);
    return LR_LW_ERR_JOIN_FAILED;
  }

  LOG_INF("Successfuly joined LoRaWAN network");
  return LR_LW_OK;
}

static bool get_last_used_devnonce(uint16_t* out) {
  LOG_DBG("Getting last used devnonce");
  if (lr_nvs_init() != LR_NVS_OK) {
    LOG_WRN("NVS init failed");
    return false;
  }

  lr_nvs_result_t ret =
      lr_nvs_read(LR_NVS_ID_LW_LAST_USED_DEVNONCE, out, sizeof(uint16_t), NULL);

  if (ret == LR_NVS_ERR_NO_VALUE) { // first time reading devnonce
    *out = 0;
    return true;
  }

  if (ret != LR_NVS_OK) {
    LOG_WRN("NVS read devnonce failed");
    return false;
  }

  return true;
}

static bool save_last_used_devnonce(uint16_t devnonce) {
  LOG_DBG("Saving last used devnonce: %u", devnonce);
  if (lr_nvs_init() != LR_NVS_OK) {
    LOG_WRN("NVS init failed");
    return false;
  }

  lr_nvs_result_t ret = lr_nvs_write(
      LR_NVS_ID_LW_LAST_USED_DEVNONCE,
      &devnonce,
      sizeof(devnonce),
      NULL
  );

  if (ret != LR_NVS_OK) {
    LOG_WRN("NVS write devnonce failed");
    return false;
  }

  return true;
}

lr_lw_result_t lr_lw_join_otaa(
    const lr_lw_join_otaa_config_t* cfg,
    enum lorawan_region region
) {
  LOG_DBG("Joining LoRaWAN using OTAA");

  uint16_t devnonce;
  get_last_used_devnonce(&devnonce);
  LOG_DBG("Retrieved last used devnonce=%u", devnonce);

  save_last_used_devnonce(++devnonce);
  LOG_DBG("Saved last used devnonce=%u", devnonce);

  struct lorawan_join_otaa otaa_cfg = {
      .join_eui = cfg->join_eui,
      .nwk_key = cfg->nwk_key,
      .app_key = cfg->app_key,
      .dev_nonce = devnonce,
  };

  struct lorawan_join_config join_cfg = {
      .otaa = otaa_cfg,
      .dev_eui = cfg->dev_eui,
      .mode = LORAWAN_ACT_OTAA,
  };

  return join(&join_cfg, region);
}

lr_lw_result_t lr_lw_join_abp(
    const lr_lw_join_abp_config_t* cfg,
    enum lorawan_region region
) {
  LOG_DBG("Joining LoRaWAN using ABP");

  struct lorawan_join_abp abp_cfg = {
      .dev_addr = cfg->dev_addr,
      .app_skey = cfg->app_skey,
      .nwk_skey = cfg->nwk_skey,
      .app_eui = cfg->app_eui
  };

  struct lorawan_join_config join_cfg = {
      .abp = abp_cfg,
      .dev_eui = cfg->dev_eui,
      .mode = LORAWAN_ACT_ABP,
  };

  return join(&join_cfg, region);
}

lr_lw_result_t lr_lw_send(
    uint8_t port,
    uint8_t* data,
    size_t size,
    enum lorawan_message_type type
) {
  LOG_DBG("Sending data");
  int ret = lorawan_send(port, data, size, type);

  if (ret == -EAGAIN || ret == -ECONNREFUSED) {
    LOG_WRN("Send failed, LoRaWAN duty cycle exceeded");
    return LR_LW_ERR_DUTY_CYCLE_EXCEEDED;
  };

  if (ret < 0) {
    LOG_WRN("Send failed: %d", ret);
    return LR_LW_ERR_SEND_FAILED;
  }

  LOG_DBG("Sent data");
  return LR_LW_OK;
}

void lr_lw_request_link_check(bool force_now) {
  LOG_INF("Requesting link check with force_now=%b", force_now);
  lorawan_request_link_check(force_now);
}

void lr_lw_register_downlink_callback(
    struct lorawan_downlink_cb* dummy,
    void (*cb)(
        uint8_t port,
        uint8_t flags,
        int16_t rssi,
        int8_t snr,
        uint8_t len,
        const uint8_t* data
    )
) {
  dummy->port = LW_RECV_PORT_ANY;
  dummy->cb = cb;
  LOG_INF("Registering a new downlink callback");
  lorawan_register_downlink_callback(dummy);
}

void lr_lw_register_battery_lvl_callback(uint8_t (*cb)(void)) {
  LOG_INF("Registering a new battery level callback");
  lorawan_register_battery_level_callback(cb);
}

void lr_lw_register_dr_changed_callback(void (*cb)(enum lorawan_datarate dr)) {
  LOG_INF("Registering a new datarate changed callback");
  lorawan_register_dr_changed_callback(cb);
}

void lr_lw_link_check_ans_callback(
    void (*cb)(uint8_t demod_margin, uint8_t nb_gateways)
) {
  LOG_INF("Registering a new link check answer callback");
  lorawan_register_link_check_ans_callback(cb);
}

void lr_lw_set_adr_mode(bool enabled) {
  LOG_INF("Setting ADR to: %s", enabled ? "enabled" : "disabled");
  lorawan_enable_adr(enabled);
}

lr_lw_result_t lr_lw_set_datarate(enum lorawan_datarate dr) {
  LOG_INF("Setting datarate to: %i", dr);
  int ret = lorawan_set_datarate(dr);

  if (ret == -EINVAL) {
    LOG_WRN("Setting datarate failed"
            " - invalid datarate for the region or ADR enabled");
    return LR_LW_INVALID_DATARATE_OR_ADR_ENABLED;
  }

  if (ret < 0) {
    LOG_WRN("Setting datarate failed: rc=%d", ret);
    return LR_LW_ERR_UNKNOWN;
  }

  LOG_INF("Set datarate");
  return LR_LW_OK;
}
