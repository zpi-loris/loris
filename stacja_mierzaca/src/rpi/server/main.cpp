#include <chrono>
#include <cstddef>
#include <cstdint>
#include <cstdio>

#include <RadioLib.h>
#include <cstdlib>
#include <thread>
#include "Module.h"
#include "TypeDef.h"
#include "hal/RPi/PiHal.h"
#include "modules/SX127x/SX1278.h"

#ifndef RISING
#define RISING 3
#endif

char SPINNER_CHARS[4] = {'/', '-', '\\', '|'};

PiHal* hal = new PiHal(0);
// NSS: 8
// DIO0: 22
// RESET: 5
// DIO1: 27
SX1278 radio = new Module(hal, 8, 22, 5, 27);

volatile bool received_packet = false;
void set_received_packet() {
  received_packet = true;
}

void listen() {
  // printf("Listening...\n");
  received_packet = false;
  int state = radio.startReceive();
  if (state != RADIOLIB_ERR_NONE) {
    printf("Listening failed, code: %d\n", state);
    exit(1);
  }
}

int main(int argc, char** argv) {
  printf("Initializing...\n");
  int state = radio.begin();
  if (state != RADIOLIB_ERR_NONE) {
    printf("Initialization failed, code: %d\n", state);
    return 1;
  }

  // Set the ISR to call when a packet is received (or transmitted, but
  // here we're only receivign)
  radio.setDio0Action(set_received_packet, RISING);

  listen();
  int spinner_char_index = 0;
  for (;;) {
    if (!received_packet) {
      printf("Listening [%c]\r", SPINNER_CHARS[spinner_char_index]);
      fflush(stdout);

      spinner_char_index = (spinner_char_index + 1) % (sizeof(SPINNER_CHARS) / sizeof(SPINNER_CHARS[0]));
      std::this_thread::sleep_for(std::chrono::milliseconds(250));
      continue;
    }
    printf("Received a packet\n");

    uint8_t str[256];
    int state = radio.readData(str, 255);

    if (state != RADIOLIB_ERR_NONE) {
      printf("Error reading data, code: %d\n",  state);
      listen();
      continue;
    }

    size_t length = radio.getPacketLength();
    str[length] = '\0';

    printf("\tReceived data: %s\n", (char*)str);
    printf("\tRSSI: %.2f\n", radio.getRSSI()); // Received Signal Strength Indicator
    printf("\tSNR: %.2f\n", radio.getSNR()); // Signal to Noise Ratio
    listen();
  }
}
