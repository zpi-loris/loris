#include "loris/lorawan.h"
#include "loris/loris.h"
#include "loris/models/capabilities.h"
#include "loris/models/function.h"
#include "loris/models/report.h"
#include "zephyr/sys/printk.h"

#include <zephyr/drivers/adc.h>
#include <zephyr/drivers/buzzer.h>
#include <zephyr/drivers/gpio.h>
#include <zephyr/drivers/led.h>
#include <zephyr/drivers/sensor.h>
#include <zephyr/drivers/w1.h>
#include <zephyr/logging/log.h>
#include <zephyr/lorawan/lorawan.h>

#define LORAWAN_DEV_EUI { 0x21, 0x10, 0x6e, 0xd4, 0x4b, 0xc5, 0x9e, 0x32 }
#define LORAWAN_JOIN_EUI { 0xcc, 0xfb, 0x24, 0xc9, 0x56, 0x15, 0xab, 0xb1 }
#define LORAWAN_APP_KEY                                                        \
  { 0x60, 0x3f, 0xb4, 0xec, 0x9b, 0x21, 0x53, 0xc6,                            \
    0x97, 0x81, 0x99, 0x86, 0x80, 0x11, 0xd4, 0xa1 }

LOG_MODULE_REGISTER(mvp_dev, LOG_LEVEL_INF);

#define REJOIN_RETRY_SECONDS 5
#define MOISTURE_SENSOR_DRY_MV 2500
#define MOISTURE_SENSOR_WET_MV 1000

report_t temp_report(sensor_t* sensor) {
  report_t sample_report = { .sensor_id = 1,
                             .value.int_val = 22,
                             .which_value = REPORT_VALUE_INT };
  return sample_report;
}

void hello_world(void* unused) { printk("Hello world! \n"); };

static const struct led_dt_spec led_green =
    LED_DT_SPEC_GET(DT_ALIAS(led_green));
static const struct led_dt_spec led_yellow =
    LED_DT_SPEC_GET(DT_ALIAS(led_yellow));
static const struct led_dt_spec led_red = LED_DT_SPEC_GET(DT_ALIAS(led_red));
static const struct led_dt_spec leds[3] = { led_green, led_yellow, led_red };
static const size_t n_leds = sizeof(leds) / sizeof(leds[0]);

static const struct device* env_temp_sensor =
    DEVICE_DT_GET(DT_ALIAS(env_temp_sensor));

static const struct adc_dt_spec moisture_sensor_adc =
    ADC_DT_SPEC_GET_BY_NAME(DT_PATH(zephyr_user), moisture);
static int32_t moisture_sensor_adc_buf[1] = { 0 };
static struct adc_sequence moisture_sensor_adc_seq = {
  .buffer = &moisture_sensor_adc_buf,
  .buffer_size = sizeof(moisture_sensor_adc_buf),
};

static const struct device* buzzer = DEVICE_DT_GET(DT_ALIAS(buzzer));

// NOTE: === REPORTS ===
bool read_temp(const struct device* dev, struct sensor_value* out) {
  if (sensor_sample_fetch_chan(dev, SENSOR_CHAN_AMBIENT_TEMP) != 0) {
    LOG_WRN("Failed to fetch sensor temperature channel");
    return false;
  }

  if (sensor_channel_get(dev, SENSOR_CHAN_AMBIENT_TEMP, out) != 0) {
    LOG_WRN("Failed to get temperature data from buffer");
    return false;
  }

  return true;
}

report_t report_env_temp(sensor_t* sensor) {
  LOG_INF("Reporting environment temperature");

  struct sensor_value sensor_value;
  float value;
  if (!read_temp(env_temp_sensor, &sensor_value)) {
    LOG_WRN("Reading environment temperature failed");
    value = -1.0;
  } else {
    value = sensor_value_to_float(&sensor_value);
    LOG_INF("Read environment temp=%f", (double)value);
  }
  report_t env_temp_report = { .sensor_id = sensor->id,
                               .which_value = REPORT_VALUE_FLOAT,
                               .value.float_val = value };
  return env_temp_report;
}

// returns a percentage
bool read_moisture(
    const struct adc_dt_spec* adc,
    const struct adc_sequence* adc_seq,
    int32_t* adc_buf,
    float* out
) {
  if (adc_read_dt(adc, adc_seq) < 0) {
    LOG_ERR("Error reading moisture raw");
    return false;
  }

  LOG_INF("Moisture raw=%d", adc_buf[0]);

  int32_t moisture_mv = adc_buf[0];
  if (adc_raw_to_millivolts_dt(adc, &moisture_mv) != 0) {
    LOG_WRN("Couldn't convert raw moisture to mV");
    return false;
  }

  LOG_INF("Moisture as voltage U=%dmV", moisture_mv);

  if (moisture_mv < MOISTURE_SENSOR_WET_MV) {
    LOG_WRN(
        "Moisture mV smaller than WET_MV=%d, this could indicate no physical "
        "connection",
        MOISTURE_SENSOR_WET_MV
    );
    return false;
  }

  float moisture_perc =
      ((float)(MOISTURE_SENSOR_DRY_MV - moisture_mv) /
       (float)(MOISTURE_SENSOR_DRY_MV - MOISTURE_SENSOR_WET_MV)) *
      100;

  *out = CLAMP(moisture_perc, 0, 100);
  return true;
}

report_t report_moisture(sensor_t* sensor) {
  LOG_INF("Reporting moisture");

  float moisture;
  if (!read_moisture(
          &moisture_sensor_adc,
          &moisture_sensor_adc_seq,
          moisture_sensor_adc_buf,
          &moisture
      )) {
    LOG_WRN("Reading moisture failed");
    moisture = -1;
  }

  report_t moisture_report = {
    .sensor_id = sensor->id,
    .which_value = REPORT_VALUE_FLOAT,
    .value.float_val = moisture,
  };

  return moisture_report;
}

// NOTE: === FUNCTIONS ===
void function_buzz_beep(void* v_msecs) {
  int32_t msecs = *(int32_t*)v_msecs;
  if (msecs < 0) {
    LOG_WRN("Invalid beep time: %d - smaller than 0", msecs);
    return;
  }

  buzzer_beep(buzzer, msecs);
}

void function_set_led_green(void* v_brightness) {
  int32_t brightness = *(int32_t*)v_brightness;
  brightness = CLAMP(brightness, 0, 100);
  led_set_brightness_dt(&led_green, brightness);
}

void function_set_led_yellow(void* v_brightness) {
  int32_t brightness = *(int32_t*)v_brightness;
  brightness = CLAMP(brightness, 0, 100);
  led_set_brightness_dt(&led_yellow, brightness);
}

void function_set_led_red(void* v_brightness) {
  int32_t brightness = *(int32_t*)v_brightness;
  brightness = CLAMP(brightness, 0, 100);
  led_set_brightness_dt(&led_red, brightness);
}

// NOTE: === SETUP ===
bool setup_devices(void) {
  LOG_INF("Setting up devices");

  LOG_INF("Setting up LEDs");
  for (size_t i = 0; i < n_leds; i++) {
    if (!led_is_ready_dt(&leds[i])) {
      LOG_ERR("LED #%d is not ready!", i);
      return false;
    }
  }

  LOG_INF("Setting up temperature sensor");
  if (!device_is_ready(env_temp_sensor)) {
    LOG_ERR("Env temp sensor is not ready!");
    return false;
  }

  LOG_INF("Setting up ADC moisture sensor");
  if (!adc_is_ready_dt(&moisture_sensor_adc)) {
    LOG_ERR("Moisture sensor ADC is not ready!");
    return false;
  }
  if (adc_channel_setup_dt(&moisture_sensor_adc)) {
    LOG_ERR("Error configuring moisture sensor ADC!");
    return false;
  }
  adc_sequence_init_dt(&moisture_sensor_adc, &moisture_sensor_adc_seq);

  LOG_INF("Setting up buzzer");
  if (!device_is_ready(buzzer)) {
    LOG_ERR("Buzzer is not ready!");
    return false;
  }

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
  if (lr_lw_set_datarate(LORAWAN_DR_5) != LR_LW_OK) {
    LOG_ERR("Failed to set datarate!");
  }
  // lr_lw_set_adr_mode(true);

  setup_devices();

  capabilities_t capabilities;

  capabilities_init(&capabilities, "loris_sample", DEVICE_CLASS_A);
  capabilities_add_sensor(
      &capabilities,
      "env_temp_sensor",
      // "Measures the temperature of the environment",
      "",
      SENSOR_VALUE_TYPE_FLOAT,
      SENSOR_SEMANTICS_TEMPERATURE,
      "C",
      &report_env_temp,
      10
  );
  capabilities_add_sensor(
      &capabilities,
      "moisture_sensor",
      // "Measures the soil moisture in the greenhouse",
      "",
      SENSOR_VALUE_TYPE_FLOAT,
      SENSOR_SEMANTICS_MOISTURE,
      "%",
      &report_moisture,
      15
  );

  capabilities_add_function(
      &capabilities,
      "beep_msecs",
      "Beep for n msecs",
      FUNC_ARG_TYPE_INT,
      &function_buzz_beep,
      -1
  );

  capabilities_add_function(
      &capabilities,
      "set_led_green",
      "Brightness: 0-100",
      FUNC_ARG_TYPE_INT,
      &function_set_led_green,
      -1
  );

  capabilities_add_function(
      &capabilities,
      "set_led_yellow",
      "Brightness: 0-100",
      FUNC_ARG_TYPE_INT,
      &function_set_led_yellow,
      -1
  );

  capabilities_add_function(
      &capabilities,
      "set_led_red",
      "Brightness: 0-100",
      FUNC_ARG_TYPE_INT,
      &function_set_led_red,
      -1
  );

  LOG_INF("Starting main loop");
  lr_start(&capabilities);

  return 0;
}
