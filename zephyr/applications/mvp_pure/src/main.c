#include <stdio.h>
#include <zephyr/drivers/gpio.h>
#include <zephyr/drivers/sensor.h>
#include <zephyr/drivers/uart.h>
#include <zephyr/drivers/w1.h>
#include <zephyr/kernel.h>
#include <zephyr/logging/log.h>

LOG_MODULE_REGISTER(mvp_pure);

#define MAIN_LOOP_SLEEP_MS 1000
#define LED_TOGGLE_DELAY_MS 200

#define LED_GREEN_NODE DT_ALIAS(led_green)
#define LED_YELLOW_NODE DT_ALIAS(led_yellow)
#define LED_RED_NODE DT_ALIAS(led_red)

#define W1_BUS DT_ALIAS(w1)

#define ENV_TEMP_SENSOR DT_ALIAS(env_temp_sensor)
// #define SOIL_TEMP_SENSOR(soil_temp_sensor)

static const struct gpio_dt_spec led_green =
    GPIO_DT_SPEC_GET(LED_GREEN_NODE, gpios);
static const struct gpio_dt_spec led_yellow =
    GPIO_DT_SPEC_GET(LED_YELLOW_NODE, gpios);
static const struct gpio_dt_spec led_red =
    GPIO_DT_SPEC_GET(LED_RED_NODE, gpios);

static const struct device* w1_bus = DEVICE_DT_GET(W1_BUS);

static const struct device* env_temp_sensor = DEVICE_DT_GET(ENV_TEMP_SENSOR);

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

  LOG_INF("Searching for 1Wire slaves");
  w1_lock_bus(w1_bus);
  int found = w1_search_rom(w1_bus, &w1_search_callback, NULL);
  w1_unlock_bus(w1_bus);
  LOG_INF("Search finished, found %d devices", found);

  // temp sensors
  if (!device_is_ready(env_temp_sensor)) {
    LOG_ERR("Env temp sensor is not ready!");
    return 1;
  }
  LOG_INF("Found env temp sensor: %s", env_temp_sensor->name);

  while (1) {
    LOG_INF("Toggling LEDs");
    for (size_t i = 0; i < n_leds; i++) {
      if (gpio_pin_toggle_dt(&leds[i]) < 0) {
        LOG_WRN("Failed to toggle LED #%d", i);
      }
      k_msleep(LED_TOGGLE_DELAY_MS);
    }

    LOG_INF("Getting temperature");
    struct sensor_value temp = { 0 };
    if (read_temp(env_temp_sensor, &temp)) {
      printk(
          "Current environment temperature is %d.%06d\n",
          temp.val1,
          temp.val2
      );
    } else {
      LOG_WRN("Failed to get environment temperature");
    }

    k_msleep(MAIN_LOOP_SLEEP_MS);
  }

  return 0;
}
