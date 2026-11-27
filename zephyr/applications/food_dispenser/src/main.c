#include "loris/lorawan.h"
#include "loris/loris.h"
#include "loris/models/capabilities.h"
#include "loris/models/function.h"
#include "loris/models/report.h"
#include "loris/models/sensor.h"
#include "loris/result.h"
#include "zephyr/drivers/adc.h"
#include "zephyr/dt-bindings/pwm/pwm.h"
#include "zephyr/kernel.h"
#include "zephyr/sleep.h"

#include <zephyr/device.h>
#include <zephyr/drivers/pwm.h>
#include <zephyr/logging/log.h>
#include <zephyr/lorawan/lorawan.h>
#include <zephyr/sys/printk.h>

#define LORAWAN_DEV_EUI { 0x53, 0xB9, 0xFB, 0xD4, 0x29, 0x1B, 0x9E, 0x36 }
#define LORAWAN_JOIN_EUI { 0x5C, 0xE1, 0xC1, 0x6C, 0x81, 0x69, 0xFC, 0xBA }
#define LORAWAN_APP_KEY                                                        \
  { 0xF9, 0xC6, 0xC2, 0xA7, 0x30, 0xF3, 0xA6, 0xA8,                            \
    0xC4, 0xB2, 0xBE, 0x10, 0xD3, 0x0D, 0x91, 0x45 }

LOG_MODULE_REGISTER(lorawan_main_test, LOG_LEVEL_INF);

#define REJOIN_RETRY_SECONDS 30

#define STEP PWM_USEC(100)

static const struct pwm_dt_spec servo = PWM_DT_SPEC_GET(DT_NODELABEL(servo));
static const uint32_t min_pulse = DT_PROP(DT_NODELABEL(servo), min_pulse);
static const uint32_t max_pulse = DT_PROP(DT_NODELABEL(servo), max_pulse);
static const struct adc_dt_spec weight_sensor_adc =
    ADC_DT_SPEC_GET_BY_NAME(DT_PATH(zephyr_user), weight);

static int32_t weight_sensor_adc_buf[1] = { 0 };
static struct adc_sequence weight_sensor_adc_seq = {
  .buffer = &weight_sensor_adc_buf,
  .buffer_size = sizeof(weight_sensor_adc_buf),
};

bool read_weight(
    const struct adc_dt_spec* adc,
    const struct adc_sequence* adc_seq,
    int32_t* adc_buf,
    float* out
) {
  if (adc_read_dt(adc, adc_seq) < 0) {
    LOG_ERR("Error reading weight raw");
    return false;
  }

  LOG_INF("Weight raw=%d", adc_buf[0]);

  int32_t weight_mv = adc_buf[0];
  if (adc_raw_to_millivolts_dt(adc, &weight_mv) != 0) {
    LOG_WRN("Couldn't convert raw weight to mV");
    return false;
  }

  LOG_INF("Weight as voltage U=%dmV", weight_mv);

  *out = weight_mv;

  return true;
}

result_t report_weight(report_t* out, sensor_t* sensor) {
  LOG_INF("Reporting weight");

  float weight;
  if (!read_weight(
          &weight_sensor_adc,
          &weight_sensor_adc_seq,
          weight_sensor_adc_buf,
          &weight
      )) {
    LOG_WRN("Reading moisture failed");
    return ERROR;
  }

  *out = (report_t){
    .sensor_id = sensor->id,
    .which_value = REPORT_VALUE_FLOAT,
    .value.float_val = weight,
  };

  return OK;
}

result_t dispense_food(void* duration) {
  int* dispenser_duration = duration;

  if (!pwm_is_ready_dt(&servo)) {
    LOG_ERR("PWM servo device not ready");
    return ERROR;
  }

  int ret = pwm_set_pulse_dt(&servo, max_pulse);
  if (ret < 0) {
    LOG_ERR("pwm_set_pulse_dt failed: %d", ret);
    return ERROR;
  }

  k_sleep(K_SECONDS(*dispenser_duration));

  ret = pwm_set_pulse_dt(&servo, min_pulse);
  return OK;
};

bool setup_devices(void) {
  LOG_INF("Setting up devices");

  LOG_INF("Setting up servo");
  if (!pwm_is_ready_dt(&servo)) {
    LOG_ERR("Servo is not ready!");
    return false;
  }
  if (adc_channel_setup_dt(&weight_sensor_adc)) {
    LOG_ERR("Error configuring weight sensor ADC!");
    return false;
  }
  adc_sequence_init_dt(&weight_sensor_adc, &weight_sensor_adc_seq);

  LOG_INF("Devices setup finished successfully");
  return true;
}

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
      &report_weight,
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

  if (!setup_devices()) {
    return -1;
  }

  lr_start(&capabilities);

  return 0;
}
