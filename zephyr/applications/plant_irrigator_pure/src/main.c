#include <zephyr/drivers/adc.h>
#include <zephyr/drivers/buzzer.h>
#include <zephyr/drivers/gpio.h>
#include <zephyr/drivers/led.h>
#include <zephyr/drivers/pwm.h>
#include <zephyr/drivers/sensor.h>
#include <zephyr/drivers/w1.h>
#include <zephyr/kernel.h>
#include <zephyr/logging/log.h>

LOG_MODULE_REGISTER(mvp_pure);

#define ACTIVE_SECS 60
#define INACTIVE_SECS 5

#define RAMP_UP_DELAY_MSEC 10
#define RAMP_DOWN_DELAY_MSEC 10

#define CLEANUP_RETRY_SECS 1

static const struct pwm_dt_spec motor = PWM_DT_SPEC_GET(DT_ALIAS(motor));

bool set_power_duty_cycle(const struct pwm_dt_spec* motor, int32_t perc) {
  uint8_t perc_clamped = CLAMP(perc, 0, 100);

  // pulse = period * duty_cycle / 100
  //
  // by inverting the order of operations below we avoid floating point
  // arithmetic
  uint32_t pulse = motor->period / 100 * perc_clamped;

  return pwm_set_pulse_dt(motor, pulse) == 0 ? true : false;
}

bool ramp_up(const struct pwm_dt_spec* motor) {
  for (int i = 0; i <= 100; i++) {
    LOG_INF("Setting power to %d", i);

    if (!set_power_duty_cycle(motor, i)) {
      LOG_WRN("Failed to set power");
      return false;
    };

    k_msleep(RAMP_UP_DELAY_MSEC);
  }

  return true;
}

bool ramp_down(const struct pwm_dt_spec* motor) {
  for (int i = 100; i >= 0; i--) {
    LOG_INF("Setting power to %d", i);

    if (!set_power_duty_cycle(motor, i)) {
      LOG_WRN("Failed to set power");
      return false;
    }

    k_msleep(RAMP_DOWN_DELAY_MSEC);
  }

  return true;
}

void cleanup(void) {
  LOG_INF("Running cleanup function");

  while (true) {
    if (set_power_duty_cycle(&motor, 0)) {
      LOG_INF("Cleaned up! Spinning forever...");

      while (true) {
        k_msleep(1000);
      }
    }

    LOG_ERR("Couldn't clean up, retrying in %d seconds...", CLEANUP_RETRY_SECS);

    k_sleep(K_SECONDS(CLEANUP_RETRY_SECS));
  }
}

int main(void) {
  if (!pwm_is_ready_dt(&motor)) {
    LOG_ERR("Motor is not ready!");
    return 1;
  }

  while (1) {
    LOG_INF("Turning the motor on...");

    LOG_INF("Motor will be on for %d seconds", ACTIVE_SECS);
    if (!ramp_up(&motor)) {
      LOG_ERR("Failed to ramp up the motor");
      cleanup();
      return 1;
    };

    k_sleep(K_SECONDS(ACTIVE_SECS));
    LOG_INF("Turning the motor off...");
    if (!ramp_down(&motor)) {
      LOG_ERR("Failed to ramp down the motor");
      cleanup();
      return 1;
    }

    LOG_INF("Motor will be off for %d seconds", INACTIVE_SECS);
    k_sleep(K_SECONDS(INACTIVE_SECS));
  }

  return 0;
}
