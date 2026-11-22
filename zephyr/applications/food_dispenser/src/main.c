#include "loris/lorawan.h"
#include "loris/loris.h"
#include "loris/models/capabilities.h"
#include "loris/models/function.h"
#include "loris/models/report.h"
#include "loris/models/sensor.h"
#include "loris/result.h"
#include "zephyr/sys/printk.h"

#include <zephyr/logging/log.h>
#include <zephyr/lorawan/lorawan.h>

#define LORAWAN_DEV_EUI { 0x53, 0xB9, 0xFB, 0xD4, 0x29, 0x1B, 0x9E, 0x36 }
#define LORAWAN_JOIN_EUI { 0x5C, 0xE1, 0xC1, 0x6C, 0x81, 0x69, 0xFC, 0xBA }
#define LORAWAN_APP_KEY                                                        \
  { 0xF9, 0xC6, 0xC2, 0xA7, 0x30, 0xF3, 0xA6, 0xA8,                            \
    0xC4, 0xB2, 0xBE, 0x10, 0xD3, 0x0D, 0x91, 0x45 }

LOG_MODULE_REGISTER(lorawan_main_test, LOG_LEVEL_INF);

#define REJOIN_RETRY_SECONDS 30

result_t force_sensitivity_report(report_t* out, sensor_t* sensor) {
  *out = sensor->read_func;

  return OK;
}

result_t dispense_food(void* unused) {
  printk("Hello world! \n");
  return OK;
};

int main() {
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

  lr_lw_set_adr_mode(false);
  if (lr_lw_set_datarate(LORAWAN_DR_3) != LR_LW_OK) {
    LOG_ERR("Failed to set datarate!");
  }
  lr_lw_set_adr_mode(true);

  capabilities_t capabilities;

  capabilities_init(&capabilities, "food_dispenser", DEVICE_CLASS_A);
  capabilities_add_sensor(
      &capabilities,
      "Bowl weight",
      "The weight of the feeding bowl",
      SENSOR_VALUE_TYPE_FLOAT,
      SENSOR_SEMANTICS_WEIGHT,
      "g",
      &force_sensitivity_report,
      30
  );

  capabilities_add_function(
      &capabilities,
      "Food dispenser",
      "Dispenses more food",
      FUNC_ARG_TYPE_VOID,
      &dispense_food,
      30
  );

  lr_start(&capabilities);

  return 0;
}
