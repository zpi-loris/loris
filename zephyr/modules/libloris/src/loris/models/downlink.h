#ifndef LORIS_DOWNLINK_H
#define LORIS_DOWNLINK_H

#include "loris/models/call.h"

// TODO: substitute for ping 
#include "loris/models/uplink.h"

typedef enum downlink_payload_type {
  DOWNLINK_PAYLOAD_TYPE_CALL = 1,
  DOWNLINK_PAYLOAD_TYPE_HELLO = 2,
  DOWNLINK_PAYLOAD_TYPE_DISCONNECT = 3,
} downlink_payload_type_t;

typedef struct downlink {
  downlink_payload_type_t payload_type;
  union {
    call_t call;
    hello_t hello;
    disconnect_t disconnect;
  } payload;
} downlink_t;

#endif
