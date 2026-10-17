/* The example of ESP-IDF
 *
 * This sample code is in the public domain.
 */

#include "driver/spi_common.h"
#include "driver/spi_master.h"
#include "driver/spi_slave.h"
#include "esp_err.h"
#include "esp_log.h"
#include "freertos/FreeRTOS.h"
#include "freertos/task.h"
#include "hal/adc_types.h"
#include "hal/spi_types.h"
#include "lora.h"
#include "soc/soc.h"
#include "soc/spi_struct.h"
#include <ctype.h>
#include <esp_adc/adc_oneshot.h>
#include <inttypes.h>
#include <stdio.h>
#include <string.h>

#if CONFIG_TRANSMITTER

// SPI GPIO Pins
#define GPIO_HANDSHAKE 2
#define GPIO_MOSI 23
#define GPIO_MISO 19
#define GPIO_SCLK 18
#define GPIO_CS 5

// ADC Pins
#define ADC_PIN ADC_CHANNEL_4        // GPIO 32
#define ADC_UNIT ADC_UNIT_1          // ADC1
#define ADC_BITWIDTH ADC_BITWIDTH_12 // 12-bit resolution (0-4095)
#define ADC_ATTEN ADC_ATTEN_DB_12    // Attenuation

#define TIMEOUT 100

spi_device_handle_t handle;
adc_oneshot_unit_handle_t adc_handle;

// Initialize SPI for ESP and LoRa
// NOT NEEDED NOR USED but maybe in the future
/*
static void spi_init() {
  esp_err_t ret;

  // Master setup
  spi_bus_config_t buscfg = {.mosi_io_num = GPIO_MOSI,
                             .miso_io_num = GPIO_MISO,
                             .sclk_io_num = GPIO_SCLK,
                             .quadwp_io_num = -1,
                             .quadhd_io_num = -1};

  ret = spi_bus_initialize(SPI3_HOST, &buscfg, SPI_DMA_CH_AUTO);
  ESP_ERROR_CHECK(ret);

  // Slave setup
  spi_device_interface_config_t slvcfg = {
      .clock_speed_hz = 1000000, // 1 MHz
      .mode = 0,                 // SPI mode 0
      .spics_io_num = GPIO_CS,
      .queue_size = 1,
      .flags = SPI_DEVICE_HALFDUPLEX,
      .pre_cb = NULL,
      .post_cb = NULL,
  };

  ESP_ERROR_CHECK(spi_bus_add_device(SPI3_HOST, &slvcfg, &handle));
}
*/

void adc_init() {
  adc_oneshot_unit_init_cfg_t init_config = {
      .unit_id = ADC_UNIT,
      .clk_src = ADC_RTC_CLK_SRC_DEFAULT,
  };
  ESP_ERROR_CHECK(adc_oneshot_new_unit(&init_config, &adc_handle));
  adc_oneshot_chan_cfg_t config = {
      .bitwidth = ADC_BITWIDTH,
      .atten = ADC_ATTEN,
  };
  ESP_ERROR_CHECK(adc_oneshot_config_channel(adc_handle, ADC_PIN, &config));

  ESP_LOGI(pcTaskGetName(NULL), "ADC Configuration succesful!");
}

// Main loop - get measurements from ADC
void transmit(void *pvParameters) {
  ESP_LOGI(pcTaskGetName(NULL), "Start");
  adc_init();

  // Read moisture rate
  int moisture;

  while (1) {
    // Read ADC value with Oneshot
    ESP_ERROR_CHECK(adc_oneshot_read(adc_handle, ADC_PIN, &moisture));
    // Print ADC value
    ESP_LOGI("ADC Value", "%d", moisture);
    // Delay 1 second
    vTaskDelay(1000 / portTICK_PERIOD_MS);

    uint8_t buf[255]; // Maximum Payload size of SX1276/77/78/79 is 255
    TickType_t nowTick = xTaskGetTickCount();
    int send_len =
        sprintf((char *)buf, "Moisture level: %d, %" PRIu32, moisture, nowTick);

    // Send packet
    lora_send_packet(buf, send_len);
    ESP_LOGI(pcTaskGetName(NULL), "%d byte packet sent:[%.*s]", send_len,
             send_len, buf);

    vTaskDelay(pdMS_TO_TICKS(5000));

  } // end while
  // never reach here
  vTaskDelete(NULL);
}
#endif // CONFIG_TRANSMITTER

#if CONFIG_RECEIVER
void receive(void *pvParameters) {
  ESP_LOGI(pcTaskGetName(NULL), "Start");
  uint8_t buf[255]; // Maximum Payload size of SX1276/77/78/79 is 255
  while (1) {
    lora_receive(); // put into receive mode
    if (lora_received()) {
      int rxLen = lora_receive_packet(buf, sizeof(buf));
      ESP_LOGI(pcTaskGetName(NULL), "%d byte packet received:[%.*s]", rxLen,
               rxLen, buf);
      for (int i = 0; i < rxLen; i++) {
        if (isupper(buf[i])) {
          buf[i] = tolower(buf[i]);
        } else {
          buf[i] = toupper(buf[i]);
        }
      }
    }
    vTaskDelay(1); // Avoid WatchDog alerts
  }

  // never reach here
  vTaskDelete(NULL);
}
#endif // CONFIG_RECEIVER

void app_main() {
  // Initialize LoRa
  if (lora_init() == 0) {
    ESP_LOGE(pcTaskGetName(NULL), "Does not recognize the module");
    while (1) {
      vTaskDelay(1);
    }
  }

#if CONFIG_433MHZ
  ESP_LOGI(pcTaskGetName(NULL), "Frequency is 433MHz");
  lora_set_frequency(433e6); // 433MHz
#elif CONFIG_866MHZ
  ESP_LOGI(pcTaskGetName(NULL), "Frequency is 866MHz");
  lora_set_frequency(866e6); // 866MHz
#elif CONFIG_915MHZ
  ESP_LOGI(pcTaskGetName(NULL), "Frequency is 915MHz");
  lora_set_frequency(915e6); // 915MHz
#elif CONFIG_OTHER
  ESP_LOGI(pcTaskGetName(NULL), "Frequency is %dMHz", CONFIG_OTHER_FREQUENCY);
  long frequency = CONFIG_OTHER_FREQUENCY * 1000000;
  lora_set_frequency(frequency);
#endif

  lora_enable_crc();

  int cr = 1;
  int bw = 7;
  int sf = 7;
#if CONFIG_ADVANCED
  cr = CONFIG_CODING_RATE;
  bw = CONFIG_BANDWIDTH;
  sf = CONFIG_SF_RATE;
#endif

  lora_set_coding_rate(cr);
  // lora_set_coding_rate(CONFIG_CODING_RATE);
  // cr = lora_get_coding_rate();
  ESP_LOGI(pcTaskGetName(NULL), "coding_rate=%d", cr);

  lora_set_bandwidth(bw);
  // lora_set_bandwidth(CONFIG_BANDWIDTH);
  // int bw = lora_get_bandwidth();
  ESP_LOGI(pcTaskGetName(NULL), "bandwidth=%d", bw);

  lora_set_spreading_factor(sf);
  // lora_set_spreading_factor(CONFIG_SF_RATE);
  // int sf = lora_get_spreading_factor();
  ESP_LOGI(pcTaskGetName(NULL), "spreading_factor=%d", sf);

#if CONFIG_TRANSMITTER
  xTaskCreate(&transmit, "TRANSMIT", 1024 * 3, NULL, 5, NULL);
#endif
#if CONFIG_RECEIVER
  xTaskCreate(&receive, "RECEIVE", 1024 * 3, NULL, 5, NULL);
#endif
}
