#include <zephyr/logging/log.h>

#include "loris/models/downlink.h"

LOG_MODULE_REGISTER(downlink);

void downlink_free(downlink_t* dl) {
  if (dl->payload_type != DOWNLINK_PAYLOAD_TYPE_CALL) {
    return;
  }

  if (dl->payload.call.which_value != CALL_VALUE_STRING) {
    return;
  }

  str_free(&dl->payload.call.value.str_val);
  LOG_DBG("freed downlink\n");
}
