#ifndef LORIS_LORAWAN_H
#define LORIS_LORAWAN_H

#include <zephyr/device.h>
#include <zephyr/lorawan/lorawan.h>

#define LORA_DEVICE_ALIAS lora0

// TODO: consider splitting into multiple result types
typedef enum loris_lw_result {
  LORIS_LW_OK = 0,

  // join related
  LORIS_LW_ERR_DEV_NOT_READY = 1,
  LORIS_LW_ERR_START_FAILED = 2,
  LORIS_LW_ERR_JOIN_FAILED = 3,
  LORIS_LW_ERR_SET_REGION_FAILED = 4,

  // send related
  LORIS_LW_ERR_SEND_FAILED = 5,
  LORIS_LW_ERR_DUTY_CYCLE_EXCEEDED = 6,
} loris_lw_result_t;

typedef struct loris_lw_config {
  enum lorawan_region region;
} loris_lw_config_t;

typedef struct loris_lw_join_otaa_config {
  uint8_t* dev_eui;
  uint8_t* join_eui;
  uint8_t* app_key;
  uint8_t* nwk_key;
} loris_lw_join_otaa_config_t;

typedef struct loris_lw_join_abp_config {
  uint8_t* dev_eui;
  uint32_t dev_addr;
  uint8_t* app_skey;
  uint8_t* nwk_skey;
  uint8_t* app_eui;
} loris_lw_join_abp_config_t;

loris_lw_result_t loris_lw_join_otaa(
    const loris_lw_join_otaa_config_t* config,
    enum lorawan_region region
);
loris_lw_result_t loris_lw_join_abp(
    const loris_lw_join_abp_config_t* cfg,
    enum lorawan_region region
);

loris_lw_result_t loris_lw_send(
    uint8_t port,
    uint8_t* data,
    size_t size,
    enum lorawan_message_type type
);
void loris_lw_request_link_check(bool force_now);

void loris_lw_register_downlink_callback(struct lorawan_downlink_cb* cb);
void loris_lw_register_battery_lvl_callback(uint8_t (*cb)(void));
void loris_lw_register_dr_changed_callback(void (*cb)(enum lorawan_datarate dr)
);
void loris_lw_link_check_ans_callback(
    void (*cb)(uint8_t demod_margin, uint8_t nb_gateways)
);

#endif
