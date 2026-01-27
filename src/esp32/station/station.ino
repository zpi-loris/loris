#include "src/RadioLib/src/RadioLib.h"
#include <Arduino.h>

const int MOISTURE_PIN = 32;
const uint16_t SLEPP_MILLIS = 5000;

//
// CS(SS) = 5
// DIO0 = 16
// RESET = 13
// DIO1 = 17
SX1278 radio = new Module(5, 16, 13, 17);

// non blocking versino - do we really need that now?
// volatile boolean done_transmitting = true;
// void set_done_transmitting() {
//   done_transmitting = true;
// }
//
boolean transmit(int moisture) {
  char data[256];
  sprintf(data, "MOISTURE=%d", moisture);
  Serial.println("Transmitting: '" + String(data) + "'");
  int state = radio.transmit(data);

  if (state != RADIOLIB_ERR_NONE) {
    return false;
  }

  return true;
}

void setup() {
  Serial.begin(115200);

  Serial.println("Initializing...");
  int state = radio.begin();
  if (state != RADIOLIB_ERR_NONE) {
    Serial.println("Error initializing, code=" + state);
    while (true) { delay(1000); }
  }
}
void loop() {
  int moisture = analogRead(MOISTURE_PIN);
  transmit(moisture);
  delay(SLEPP_MILLIS);
}
