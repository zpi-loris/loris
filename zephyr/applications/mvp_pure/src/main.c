#include <stdio.h>
#include <zephyr/drivers/adc.h>
#include <zephyr/drivers/buzzer.h>
#include <zephyr/drivers/gpio.h>
#include <zephyr/drivers/sensor.h>
#include <zephyr/drivers/uart.h>
#include <zephyr/drivers/w1.h>
#include <zephyr/kernel.h>
#include <zephyr/logging/log.h>

LOG_MODULE_REGISTER(mvp_pure);

#define MAIN_LOOP_SLEEP_MS 10000
#define LED_TOGGLE_DELAY_MS 200
#define BUZZ_TIME_MS 1000

#define MOISTURE_SENSOR_DRY_MV 2500
#define MOISTURE_SENSOR_WET_MV 1000

// #define SOIL_TEMP_SENSOR(soil_temp_sensor)

static const struct gpio_dt_spec led_green =
    GPIO_DT_SPEC_GET(DT_ALIAS(led_green), gpios);
static const struct gpio_dt_spec led_yellow =
    GPIO_DT_SPEC_GET(DT_ALIAS(led_yellow), gpios);
static const struct gpio_dt_spec led_red =
    GPIO_DT_SPEC_GET(DT_ALIAS(led_red), gpios);

static const struct device* w1_bus = DEVICE_DT_GET(DT_ALIAS(w1));

static const struct device* env_temp_sensor =
    DEVICE_DT_GET(DT_ALIAS(env_temp_sensor));

static const struct adc_dt_spec moisture_sensor_adc =
    ADC_DT_SPEC_GET_BY_NAME(DT_PATH(zephyr_user), moisture);

static const struct device* buzzer = DEVICE_DT_GET(DT_ALIAS(buzzer));

void w1_search_callback(struct w1_rom rom, void* user_data) {
  printk(
      "Found 1Wire slave: family=%#x, serial=%#02x%02x%02x%02x%02x%02x\n",
      rom.family,
      rom.serial[0],
      rom.serial[1],
      rom.serial[2],
      rom.serial[3],
      rom.serial[4],
      rom.serial[5]
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

  struct gpio_dt_spec leds[3] = { led_green, led_yellow, led_red };
  size_t n_leds = sizeof(leds) / sizeof(leds[0]);

  // LEDs
  for (size_t i = 0; i < n_leds; i++) {
    if (!gpio_is_ready_dt(&leds[i])) {
      LOG_ERR("LED #%d is not ready!", i);
      return 1;
    }

    if (gpio_pin_configure_dt(&leds[i], GPIO_OUTPUT_ACTIVE) < 0) {
      LOG_ERR("Faield to setup LED #%d", i);
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
  while (1) {
    printk("Toggling LEDs\n");
    for (size_t i = 0; i < n_leds; i++) {
      if (gpio_pin_toggle_dt(&leds[i]) < 0) {
        LOG_WRN("Failed to toggle LED #%d", i);
      }
      k_msleep(LED_TOGGLE_DELAY_MS);
    }

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
    buzzer_beep(buzzer, BUZZ_TIME_MS);
    k_msleep(BUZZ_TIME_MS);
    printk("<silence>\n");

    k_msleep(MAIN_LOOP_SLEEP_MS);
  }

  return 0;
}
