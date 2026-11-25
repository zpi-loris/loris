#include <stdio.h>
#include <zephyr/drivers/adc.h>
#include <zephyr/drivers/buzzer.h>
#include <zephyr/drivers/gpio.h>
#include <zephyr/drivers/led.h>
#include <zephyr/drivers/sensor.h>
#include <zephyr/drivers/sensor/w1_sensor.h>
#include <zephyr/drivers/w1.h>
#include <zephyr/kernel.h>
#include <zephyr/logging/log.h>

LOG_MODULE_REGISTER(mvp_pure);

#define MAIN_LOOP_SLEEP_MS 1000
#define FN_LED_TOGGLE_DELAY 200
#define INFO_LED_TOGGLE_DELAY 1000
#define BUZZ_TIME_MS 1000

#define MOISTURE_SENSOR_DRY_MV 2500
#define MOISTURE_SENSOR_WET_MV 1000

static const uint64_t ENV_TEMP_W1_ROM_ID = 0x28cbdd83100000d0;
static const uint64_t SOIL_TEMP_W1_ROM_ID = 0x28f41d8700783460;

// #define SOIL_TEMP_SENSOR(soil_temp_sensor)

static const struct led_dt_spec fn_led_green =
    LED_DT_SPEC_GET(DT_ALIAS(fn_led_green));
static const struct led_dt_spec fn_led_yellow =
    LED_DT_SPEC_GET(DT_ALIAS(fn_led_yellow));
static const struct led_dt_spec fn_led_red =
    LED_DT_SPEC_GET(DT_ALIAS(fn_led_red));

static const struct led_dt_spec info_led_red =
    LED_DT_SPEC_GET(DT_ALIAS(info_led_red));
static const struct led_dt_spec info_led_green =
    LED_DT_SPEC_GET(DT_ALIAS(info_led_green));
static const struct led_dt_spec info_led_blue =
    LED_DT_SPEC_GET(DT_ALIAS(info_led_blue));

static const struct device* w1_bus = DEVICE_DT_GET(DT_ALIAS(w1));

static const struct device* env_temp_sensor =
    DEVICE_DT_GET(DT_ALIAS(env_temp_sensor));
static const struct device* soil_temp_sensor =
    DEVICE_DT_GET(DT_ALIAS(soil_temp_sensor));

static const struct adc_dt_spec moisture_sensor_adc =
    ADC_DT_SPEC_GET_BY_NAME(DT_PATH(zephyr_user), moisture);

static const struct device* buzzer = DEVICE_DT_GET(DT_ALIAS(buzzer));

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

void w1_search_callback(struct w1_rom rom, void* user_data) {
  printk(
      "Found 1Wire slave: family=%#x, full_rom=0x%016llx\n",
      rom.family,
      w1_rom_to_uint64(&rom)
  );
}

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

int main(void) {

  struct led_dt_spec fn_leds[3] = { fn_led_green, fn_led_yellow, fn_led_red };
  struct led_dt_spec info_leds[3] = { info_led_red,
                                      info_led_green,
                                      info_led_blue };

  size_t n_fn_leds = sizeof(fn_leds) / sizeof(fn_leds[0]);
  size_t n_info_leds = sizeof(info_leds) / sizeof(info_leds[0]);

  // fn LEDs
  for (size_t i = 0; i < n_fn_leds; i++) {
    if (!led_is_ready_dt(&fn_leds[i])) {
      LOG_ERR("LED #%d is not ready!", i);
      return 1;
    }
  }

  // info LEDs
  for (size_t i = 0; i < n_info_leds; i++) {
    if (!led_is_ready_dt(&info_leds[i])) {
      LOG_ERR("LED #%d is not ready!", i);
      return 1;
    }
  }

  // 1Wire bus
  if (!device_is_ready(w1_bus)) {
    LOG_ERR("1Wire bus is not ready!");
    return 1;
  }

  printk("Searching for 1Wire slaves\n");
  w1_lock_bus(w1_bus);
  int found = w1_search_rom(w1_bus, &w1_search_callback, NULL);
  w1_unlock_bus(w1_bus);
  printk("Search finished, found %d devices\n", found);

  // temp sensors
  if (!device_is_ready(env_temp_sensor)) {
    LOG_ERR("Env temp sensor is not ready!");
    return 1;
  }

  if (!device_is_ready(soil_temp_sensor)) {
    LOG_ERR("soil temp sensor is not ready!");
    return 1;
  }

  w1_bind_rom(soil_temp_sensor, SOIL_TEMP_W1_ROM_ID);
  w1_bind_rom(env_temp_sensor, ENV_TEMP_W1_ROM_ID);

  // ADC - moisture sensor
  if (!adc_is_ready_dt(&moisture_sensor_adc)) {
    LOG_ERR("Moisture sensor ADC is not ready!");
    return 1;
  }

  if (adc_channel_setup_dt(&moisture_sensor_adc)) {
    LOG_ERR("Error configuring moisture sensor ADC!");
    return 1;
  }

  int32_t moisture_sensor_adc_buf[1] = { 0 };
  struct adc_sequence moisture_sensor_adc_seq = {
    .buffer = &moisture_sensor_adc_buf,
    .buffer_size = sizeof(moisture_sensor_adc_buf),
  };
  adc_sequence_init_dt(&moisture_sensor_adc, &moisture_sensor_adc_seq);

  // buzzer
  if (!device_is_ready(buzzer)) {
    LOG_ERR("Buzzer is not ready!");
    return 1;
  }

  // main loop
  uint8_t brightness = 100;
  while (1) {
    printk("Toggling LEDs\n");

    printk("Setting function LEDs to %u%%\n", brightness);
    for (size_t i = 0; i < n_fn_leds; i++) {
      if (led_set_brightness(fn_leds[i].dev, fn_leds[i].index, brightness) <
          0) {
        LOG_WRN("Failed to set brightness for LED #%zu", i);
      }
      k_msleep(FN_LED_TOGGLE_DELAY);
    }

    printk("Blinking info LEDs\n");
    for (size_t i = 0; i < n_info_leds; i++) {
      printk("Blinking LED %i\n", i);
      if (led_set_brightness(info_leds[i].dev, info_leds[i].index, 100) < 0) {
        LOG_WRN("Failed to set brightness to HIGH for LED #%zu", i);
      }

      k_msleep(INFO_LED_TOGGLE_DELAY);

      if (led_set_brightness(info_leds[i].dev, info_leds[i].index, 0) < 0) {
        LOG_WRN("Failed to set brightness to LOW for LED #%zu", i);
      }
    }

    brightness = (brightness > 0) ? 0 : 100;

    printk("Getting temperature\n");
    struct sensor_value temp = { 0 };
    if (!read_temp(env_temp_sensor, &temp)) {
      LOG_WRN("Failed to get environment temperature");
    } else {
      printk(
          "Current environment temperature is %d.%06d\n",
          temp.val1,
          temp.val2
      );
    }
    if (!read_temp(soil_temp_sensor, &temp)) {
      LOG_WRN("Failed to get soil temperature");
    } else {
      printk("Current soil temperature is %d.%06d\n", temp.val1, temp.val2);
    }

    printk("Getting moisture\n");
    if (adc_read_dt(&moisture_sensor_adc, &moisture_sensor_adc_seq) < 0) {
      LOG_ERR("Error reading moisture");
    } else {
      printk("Moisture raw=%d\n", moisture_sensor_adc_buf[0]);
      int32_t moisture_mv = moisture_sensor_adc_buf[0];

      if (adc_raw_to_millivolts_dt(&moisture_sensor_adc, &moisture_mv) != 0) {
        LOG_WRN("Couldn't convert raw to mV");
      } else {
        printk("Moisture as voltage U=%dmV\n", moisture_mv);
        float moisture_perc =
            ((float)(MOISTURE_SENSOR_DRY_MV - moisture_mv) /
             (float)(MOISTURE_SENSOR_DRY_MV - MOISTURE_SENSOR_WET_MV)) *
            100;
        printk("Moisture: %.2f%%\n", (double)moisture_perc);
        // NOTE: Might be a good idea to clamp it [0; 100]
      }
    }

    printk("Bzzzz...\n");
    // NOTE: commented out - too annoying
    buzzer_beep(buzzer, BUZZ_TIME_MS);
    k_msleep(BUZZ_TIME_MS);
    printk("<silence>\n");

    k_msleep(MAIN_LOOP_SLEEP_MS);
  }

  return 0;
}
