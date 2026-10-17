/*
  RadioLib SX127x Ping-Pong Example

  This example is intended to run on two SX126x radios,
  and send packets between the two.

  For default module settings, see the wiki page
  https://github.com/jgromes/RadioLib/wiki/Default-configuration#sx127xrfm9x---lora-modem

  For full API reference, see the GitHub Pages
  https://jgromes.github.io/RadioLib/
*/

// include the library
#include <RadioLib.h>
#include <cstddef>
#include <sys/types.h>
#include <chrono>
#include <thread>

// include the hardware abstraction layer
#include "hal/RPi/PiHal.h"

#ifndef RISING
#define RISING 3
#endif

// uncomment the following only on one
// of the nodes to initiate the pings
//#define INITIATING_NODE
//
PiHal* hal = new PiHal(0);

// -- CUSTOM --
// SX1278 has the following connections:
// NSS pin:   8
// DIO0 pin:  22
// RESET pin: 5
// DIO1 pin:  27
//
// To make the NSS pin work I had to change the SPI0 dtoverlay 
// to spi0-0cs, which basically tells the RPi to enable SPI0, but claim no
// Chip Select pins, which leaves them free for me to configure in software (use
// them as regular Chip Select software-controlled pins). dtoverlay = Device
// Tree overlay. Listing all available dtoverlays: dtoverlay -a. You change
// the overlay in /boot/firmware/config.txt. dtoverlay is a pretty useful command
SX1278 radio = new Module(hal, 8, 22, 5, 27);

// or detect the pinout automatically using RadioBoards
// https://github.com/radiolib-org/RadioBoards
/*
#define RADIO_BOARD_AUTO
#include <RadioBoards.h>
Radio radio = new RadioModule();
*/

// save transmission states between loops
int transmissionState = RADIOLIB_ERR_NONE;

// flag to indicate transmission or reception state
bool transmitFlag = false;

// flag to indicate that a packet was sent or received
volatile bool operationDone = false;

// this function is called when a complete packet
// is transmitted or received by the module
// IMPORTANT: this function MUST be 'void' type
//            and MUST NOT have any arguments!
#if defined(ESP8266) || defined(ESP32)
  ICACHE_RAM_ATTR
#endif
void setFlag(void) {
  // we sent or received  packet, set the flag
  operationDone = true;
}

int main(int argc, char** argv) {

  // initialize SX1278 with default settings
  printf("[SX1278] Initializing ...\n");
  int state = radio.begin();
  if (state == RADIOLIB_ERR_NONE) {
    printf("success!\n");
  } else {
    printf("failed, code %d", state);
    return 1;
  }

  // set the function that will be called
  // when new packet is received
  radio.setDio0Action(setFlag, RISING);

  #if defined(INITIATING_NODE)
    // send the first packet on this node
    Serial.print(F("[SX1278] Sending first packet ... "));
    transmissionState = radio.startTransmit("Hello World!");
    transmitFlag = true;
  #else
    // start listening for LoRa packets on this node
    printf("[SX1278] Starting to listen ... ");
    state = radio.startReceive();
    if (state == RADIOLIB_ERR_NONE) {
      printf("success!\n");
    } else {
      printf("failed, code %d", state);
      return 1;
    }
  #endif

  for (;;) {
    // check if the previous operation finished
    if(operationDone) {
      // reset flag
      operationDone = false;

      if(transmitFlag) {
        // the previous operation was transmission, listen for response
        // print the result
        if (transmissionState == RADIOLIB_ERR_NONE) {
          // packet was successfully sent
          printf("transmission finished!\n");

        } else {
          printf("failed, code %d\n", transmissionState);
          return 1;
        }

        // listen for response
        radio.startReceive(); transmitFlag = false;
        std::this_thread::sleep_for(std::chrono::milliseconds(1));
      } else {
        // the previous operation was reception
        // print data and send another packet
        u_int8_t str[256];
        int state = radio.readData(str, 255);

        if (state == RADIOLIB_ERR_NONE) {
          // packet was successfully received
          printf("[SX1278] Received packet!\n");

          size_t length = radio.getPacketLength();
          str[length] = '\0';

          // print data of the packet
          printf("[SX1278] Data:\t\t%s\n", (char*)str);

          // print RSSI (Received Signal Strength Indicator)
          printf("[SX1278] RSSI:\t\t%.2f dBm\n", radio.getRSSI());

          // print SNR (Signal-to-Noise Ratio)
          printf("[SX1278] SNR:\t\t%.2f dB\n", radio.getSNR());

        }

        // wait a second before transmitting again
        std::chrono::milliseconds second(1000);
        std::this_thread::sleep_for(second);

        // send another one
        printf("[SX1278] Sending another packet ... \n");
        transmissionState = radio.startTransmit("Pong!");
        transmitFlag = true;
      }
    }
  }
}
