/*
   RadioLib Non-Arduino Raspberry Pi Example

   This example shows how to use RadioLib without Arduino.
   In this case, a Raspberry Pi with WaveShare SX1302 LoRaWAN Hat
   using the lgpio library
   https://abyz.me.uk/lg/lgpio.html

   Can be used as a starting point to port RadioLib to any platform!
   See this API reference page for details on the RadioLib hardware abstraction
   https://jgromes.github.io/RadioLib/class_hal.html

   For full API reference, see the GitHub Pages
   https://jgromes.github.io/RadioLib/
*/

// include the library
#include <RadioLib.h>

// include the hardware abstraction layer
#include "hal/RPi/PiHal.h"

// the SX1278 CS is connected to CE0
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

// the entry point for the program
int main(int argc, char** argv) {
  // initialize just like with Arduino
  printf("[SX1278] Initializing ... ");
  int state = radio.begin();
  if (state != RADIOLIB_ERR_NONE) {
    printf("failed, code %d\n", state);
    return(1);
  }
  printf("success!\n");

  // loop forever
  int count = 0;
  for(;;) {
    // send a packet
    printf("[SX1278] Transmitting packet ... ");
    char str[64];
    sprintf(str, "Hello World! #%d", count++);
    state = radio.transmit(str);
    if(state == RADIOLIB_ERR_NONE) {
      // the packet was successfully transmitted
      printf("success!\n");

      // wait for a second before transmitting again
      hal->delay(1000);

    } else {
      printf("failed, code %d\n", state);

    }

  }

  return(0);
}
