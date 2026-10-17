#include "src/RadioLib/src/RadioLib.h"
#include <OneWire.h>
#include <Arduino.h>
#include <DallasTemperature.h>

const int MOISTURE_PIN = 32;

const uint16_t TEMP_REQUEST_MILLIS = 750;
const uint16_t INTERVAL_MILLIS = 5000;
const uint16_t SLEEP_MILLIS = INTERVAL_MILLIS - TEMP_REQUEST_MILLIS;

//
// CS(SS) = 5
// DIO0 = 16
// RESET = 13
// DIO1 = 17
SX1278 radio = new Module(5, 16, 13, 17);

OneWire one_wire(33);
DallasTemperature temp_sensors(&one_wire);

// non blocking versino - do we really need that now?
// volatile boolean done_transmitting = true;
// void set_done_transmitting() {
//   done_transmitting = true;
// }

struct SensorReadings {
  int moisture;
  float soil_temperature;
};

boolean transmit(SensorReadings &readings) {
  char data[256];
  sprintf(data, "MOISTURE=%d;SOIL_TEMP=%.2f", readings.moisture, readings.soil_temperature);
  Serial.println("Transmitting: '" + String(data) + "'");
  int state = radio.transmit(data);

  if (state != RADIOLIB_ERR_NONE) {
    return false;
  }

  return true;
}

void setup() {
  Serial.begin(115200);
  temp_sensors.begin();

  Serial.println("Initializing...");
  int state = radio.begin();
  if (state != RADIOLIB_ERR_NONE) {
    Serial.println("Error initializing, code=" + state);
    while (true) { delay(1000); }
  }
}
void loop() {
  temp_sensors.requestTemperatures();
  delay(TEMP_REQUEST_MILLIS);

  float soil_temp = temp_sensors.getTempCByIndex(0);
  int moisture = analogRead(MOISTURE_PIN);

  SensorReadings readings = { moisture, soil_temp };
  transmit(readings);
  delay(SLEEP_MILLIS);
}
