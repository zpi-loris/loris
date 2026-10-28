/*
 * Copyright (c) 2016 Intel Corporation
 *
 * SPDX-License-Identifier: Apache-2.0
 */

#include "loris/models.h"
#include "zephyr/sys/slist.h"
#include <loris/capabilities.h>
#include <zephyr/devicetree.h>
#include <zephyr/drivers/adc.h>
#include <zephyr/kernel.h>

static const struct adc_dt_spec adc_channel =
    ADC_DT_SPEC_GET(DT_PATH(zephyr_user));

int16_t sample_buffer[1];

struct adc_sequence sequence = {
    .buffer = sample_buffer,
    .buffer_size = sizeof(sample_buffer),
    // Optional: resolution, etc. Usually taken from DT
};

int main(void) {
  sys_slist_t sensor_list;
  sys_slist_t function_list;
  add_sensor(&sensor_list, "Temp Sensor", "Measures temperature",
             VALUE_TYPE_TEMPERATURE, "C");

  int err;

  if (!adc_is_ready_dt(&adc_channel)) {
    printk("ADC is not ready\n");
    return 0;
  }

  /* 1. Configure the channel (Gain, Reference, Acquisition time) */
  /* Zephyr configures this automatically if we use ADC_DT_SPEC_GET
     and define it correctly in the overlay, or we can do it manually: */
  err = adc_channel_setup_dt(&adc_channel);
  if (err < 0) {
    printk("Error configuring channel (%d)\n", err);
    return 0;
  }

  /* 2. Prepare the sequence */
  /* We tell it which specific channel to read using the Devicetree info */
  err = adc_sequence_init_dt(&adc_channel, &sequence);
  if (err < 0) {
    printk("Error initializing sequence\n");
    return 0;
  }

  while (1) {
    /* 3. Read (Blocking) */
    err = adc_read(adc_channel.dev, &sequence);
    if (err < 0) {
      printk("Error reading ADC (%d)\n", err);
    } else {
      /* 4. Convert to millivolts (Optional but useful) */
      int32_t val_mv = sample_buffer[0];

      /* This function uses the reference and gain configuration
         to give us real millivolts */
      if (adc_raw_to_millivolts_dt(&adc_channel, &val_mv) == 0) {
        printk("Raw reading: %d, voltage: %d mV\n", sample_buffer[0], val_mv);
      } else {
        printk("Raw reading: %d; reference not convertible to mV\n",
               sample_buffer[0]);
      }
    }

    k_msleep(1000);
  }
  return 0;
}
