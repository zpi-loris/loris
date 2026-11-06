#include <zephyr/device.h>
#include <zephyr/logging/log.h>
#include <zephyr/lorawan/lorawan.h>

#include "loris/lorawan.h"

LOG_MODULE_REGISTER(loris_lw);

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

static uint16_t get_last_used_devnonce() {
  // TODO: ===
  return 0;
}

static void save_last_used_devnonce(uint16_t devnonce) {
  // TODO: ===
}

lr_lw_result_t lr_lw_join_otaa(
    const lr_lw_join_otaa_config_t* cfg,
    enum lorawan_region region
) {
  uint16_t dev_nonce = get_last_used_devnonce();
  save_last_used_devnonce(++dev_nonce);

  struct lorawan_join_otaa otaa_cfg = {
      .join_eui = cfg->join_eui,
      .nwk_key = cfg->nwk_key,
      .app_key = cfg->app_key,
      .dev_nonce = dev_nonce,
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

  if (ret == -EAGAIN) {
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

/* TODO: docs!!!
 *
 * Registers given callback to listen on ANY port
 */
void lr_lw_register_downlink_callback(struct lorawan_downlink_cb* cb) {
  LOG_INF("Registering a new downlink callback");
  lorawan_register_downlink_callback(cb);
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
