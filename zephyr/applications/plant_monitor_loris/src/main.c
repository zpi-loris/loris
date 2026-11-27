#include "loris/lorawan.h"
#include "loris/loris.h"
#include "loris/models/capabilities.h"
#include "loris/models/function.h"
#include "loris/models/report.h"

#include <zephyr/drivers/adc.h>
#include <zephyr/drivers/buzzer.h>
#include <zephyr/drivers/gpio.h>
#include <zephyr/drivers/led.h>
#include <zephyr/drivers/sensor.h>
#include <zephyr/drivers/sensor/w1_sensor.h>
#include <zephyr/drivers/w1.h>
#include <zephyr/logging/log.h>
#include <zephyr/lorawan/lorawan.h>

// Plant monitor
#define LORAWAN_DEV_EUI { 0x2e, 0xf2, 0xc2, 0x3b, 0x9e, 0x78, 0x97, 0x74 }
#define LORAWAN_JOIN_EUI { 0x45, 0x36, 0x31, 0x48, 0xe3, 0xc2, 0xaf, 0x73 }
#define LORAWAN_APP_KEY                                                        \
  { 0xd1, 0xdf, 0x9a, 0xc1, 0x92, 0x63, 0x12, 0x28,                            \
    0x72, 0x7b, 0x1c, 0x7d, 0xe9, 0xa4, 0x0c, 0x23 }

LOG_MODULE_REGISTER(plant_monitor, LOG_LEVEL_INF);

static const uint8_t REJOIN_RETRY_SECONDS = 5;
static const uint32_t MOISTURE_SENSOR_DRY_MV = 2500;
static const uint32_t MOISTURE_SENSOR_WET_MV = 1000;

static const uint64_t ENV_TEMP_W1_ROM_ID = 0x28cbdd83100000d0;
static const uint64_t SOIL_TEMP_W1_ROM_ID = 0x28f41d8700783460;

static const struct led_dt_spec fn_led_green =
    LED_DT_SPEC_GET(DT_ALIAS(fn_led_green));
static const struct led_dt_spec fn_led_yellow =
    LED_DT_SPEC_GET(DT_ALIAS(fn_led_yellow));
static const struct led_dt_spec fn_led_red =
    LED_DT_SPEC_GET(DT_ALIAS(fn_led_red));
static const struct led_dt_spec leds[3] = { fn_led_green,
                                            fn_led_yellow,
                                            fn_led_red };
static const size_t n_leds = sizeof(leds) / sizeof(leds[0]);

static const struct device* env_temp_sensor =
    DEVICE_DT_GET(DT_ALIAS(env_temp_sensor));
static const struct device* soil_temp_sensor =
    DEVICE_DT_GET(DT_ALIAS(soil_temp_sensor));

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

result_t report_soil_temp(report_t* out, sensor_t* sensor) {
  LOG_INF("Reporting soil temperature");

  struct sensor_value sensor_value;
  float value;
  if (!read_temp(soil_temp_sensor, &sensor_value)) {
    LOG_WRN("Reading environment temperature failed");
    return ERROR;
  }
  value = sensor_value_to_float(&sensor_value);

  LOG_INF("Read environment temp=%f", (double)value);
  *out = (report_t){ .sensor_id = sensor->id,
                     .which_value = REPORT_VALUE_FLOAT,
                     .value.float_val = value };
  return OK;
}

result_t report_env_temp(report_t* out, sensor_t* sensor) {
  LOG_INF("Reporting environment temperature");

  struct sensor_value sensor_value;
  float value;
  if (!read_temp(env_temp_sensor, &sensor_value)) {
    LOG_WRN("Reading environment temperature failed");
    return ERROR;
  }
  value = sensor_value_to_float(&sensor_value);

  LOG_INF("Read environment temp=%f", (double)value);
  *out = (report_t){ .sensor_id = sensor->id,
                     .which_value = REPORT_VALUE_FLOAT,
                     .value.float_val = value };
  return OK;
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

result_t report_moisture(report_t* out, sensor_t* sensor) {
  LOG_INF("Reporting moisture");

  float moisture;
  if (!read_moisture(
          &moisture_sensor_adc,
          &moisture_sensor_adc_seq,
          moisture_sensor_adc_buf,
          &moisture
      )) {
    LOG_WRN("Reading moisture failed");
    return ERROR;
  }

  *out = (report_t){
    .sensor_id = sensor->id,
    .which_value = REPORT_VALUE_FLOAT,
    .value.float_val = moisture,
  };

  return OK;
}

// NOTE: === FUNCTIONS ===
result_t function_buzz_beep(void* v_msecs) {
  int32_t msecs = *(int32_t*)v_msecs;
  if (msecs < 0) {
    LOG_WRN("Invalid beep time: %d - smaller than 0", msecs);
    return ERROR;
  }

  buzzer_beep(buzzer, msecs);
  return OK;
}

result_t function_set_led_green(void* v_brightness) {
  int32_t brightness = *(int32_t*)v_brightness;
  brightness = CLAMP(brightness, 0, 100);
  led_set_brightness_dt(&fn_led_green, brightness);
  return OK;
}

result_t function_set_led_yellow(void* v_brightness) {
  int32_t brightness = *(int32_t*)v_brightness;
  brightness = CLAMP(brightness, 0, 100);
  led_set_brightness_dt(&fn_led_yellow, brightness);
  return OK;
}

result_t function_set_led_red(void* v_brightness) {
  int32_t brightness = *(int32_t*)v_brightness;
  brightness = CLAMP(brightness, 0, 100);
  led_set_brightness_dt(&fn_led_red, brightness);
  return OK;
}

// NOTE: === SETUP ===
static void w1_bind_rom(const struct device* dev, uint64_t rom_uint64) {
  struct w1_rom rom;
  struct sensor_value rom_val;

  w1_uint64_to_rom(rom_uint64, &rom);
  w1_rom_to_sensor_value(&rom, &rom_val);
  sensor_attr_set(
      dev,
      SENSOR_CHAN_ALL,
      (enum sensor_attribute)SENSOR_ATTR_W1_ROM,
      &rom_val
  );
}

bool setup_devices(void) {
  LOG_INF("Setting up devices");

  LOG_INF("Setting up LEDs");
  for (size_t i = 0; i < n_leds; i++) {
    if (!led_is_ready_dt(&leds[i])) {
      LOG_ERR("LED #%d is not ready!", i);
      return false;
    }
  }

  LOG_INF("Setting up temperature sensors");
  if (!device_is_ready(env_temp_sensor)) {
    LOG_ERR("Env temp sensor is not ready!");
    return false;
  }
  if (!device_is_ready(soil_temp_sensor)) {
    LOG_ERR("soil temp sensor is not ready!");
    return false;
  }
  w1_bind_rom(soil_temp_sensor, SOIL_TEMP_W1_ROM_ID);
  w1_bind_rom(env_temp_sensor, ENV_TEMP_W1_ROM_ID);

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

  capabilities_init(&capabilities, "plant-monitor", DEVICE_CLASS_A);
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
      "soil_temp_sensor",
      // "Measures the temperature of the environment",
      "",
      SENSOR_VALUE_TYPE_FLOAT,
      SENSOR_SEMANTICS_TEMPERATURE,
      "C",
      &report_soil_temp,
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
      "0-100",
      FUNC_ARG_TYPE_INT,
      &function_set_led_green,
      -1
  );

  capabilities_add_function(
      &capabilities,
      "set_led_yellow",
      "0-100",
      FUNC_ARG_TYPE_INT,
      &function_set_led_yellow,
      -1
  );

  capabilities_add_function(
      &capabilities,
      "set_led_red",
      "0-100",
      FUNC_ARG_TYPE_INT,
      &function_set_led_red,
      -1
  );

  LOG_INF("Starting main loop");
  lr_start(&capabilities);

  return 0;
}
