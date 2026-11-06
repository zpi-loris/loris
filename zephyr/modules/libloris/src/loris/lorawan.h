#ifndef LORIS_LORAWAN_H
#define LORIS_LORAWAN_H

#include <zephyr/device.h>
#include <zephyr/lorawan/lorawan.h>

#define LORA_DEVICE_ALIAS lora0

// TODO: consider splitting into multiple result types
typedef enum lr_lw_result {
  LR_LW_OK = 0,

  // join related
  LR_LW_ERR_DEV_NOT_READY = 1,
  LR_LW_ERR_START_FAILED = 2,
  LR_LW_ERR_JOIN_FAILED = 3,
  LR_LW_ERR_SET_REGION_FAILED = 4,

  // send related
  LR_LW_ERR_SEND_FAILED = 5,
  LR_LW_ERR_DUTY_CYCLE_EXCEEDED = 6,
} lr_lw_result_t;

typedef struct lr_lw_config {
  enum lorawan_region region;
} lr_lw_config_t;

typedef struct lr_lw_join_otaa_config {
  uint8_t* dev_eui;
  uint8_t* join_eui;
  uint8_t* app_key;
  uint8_t* nwk_key;
} lr_lw_join_otaa_config_t;

typedef struct lr_lw_join_abp_config {
  uint8_t* dev_eui;
  uint32_t dev_addr;
  uint8_t* app_skey;
  uint8_t* nwk_skey;
  uint8_t* app_eui;
} lr_lw_join_abp_config_t;

lr_lw_result_t lr_lw_join_otaa(
    const lr_lw_join_otaa_config_t* config,
    enum lorawan_region region
);
lr_lw_result_t lr_lw_join_abp(
    const lr_lw_join_abp_config_t* cfg,
    enum lorawan_region region
);

lr_lw_result_t lr_lw_send(
    uint8_t port,
    uint8_t* data,
    size_t size,
    enum lorawan_message_type type
);
void lr_lw_request_link_check(bool force_now);

void lr_lw_register_downlink_callback(struct lorawan_downlink_cb* cb);
void lr_lw_register_battery_lvl_callback(uint8_t (*cb)(void));
void lr_lw_register_dr_changed_callback(void (*cb)(enum lorawan_datarate dr));
void lr_lw_link_check_ans_callback(
    void (*cb)(uint8_t demod_margin, uint8_t nb_gateways)
);

#endif
