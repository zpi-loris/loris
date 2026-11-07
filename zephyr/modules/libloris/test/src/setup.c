/* GLOBAL TESTS SETUP
 *
 * Needed because FFF DEFINE_FFF_GLOBALS macro needs to be always called,
 * because we're using a global LoRa fake driver
 */

#include <zephyr/fff.h>

DEFINE_FFF_GLOBALS;
