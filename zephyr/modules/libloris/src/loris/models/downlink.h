#ifndef LORIS_DOWNLINK_H
#define LORIS_DOWNLINK_H

#include "loris/models/call.h"

typedef enum down_ping_type {
  DOWN_PING_TYPE_UNSPECIFIED = 0,
  DOWN_PING_TYPE_HELLO = 1,
  DOWN_PING_TYPE_DISCONNECT = 2
} down_ping_type_t;

typedef struct down_ping {
  down_ping_type_t type;
} down_ping_t;

typedef enum downlink_payload_type {
  DOWNLINK_PAYLOAD_TYPE_CALL = 1,
  DOWNLINK_PAYLOAD_TYPE_PING = 2,
} downlink_payload_type_t;

typedef struct downlink {
  downlink_payload_type_t payload_type;
  union {
    call_t call;
    down_ping_t ping;
  } payload;
} downlink_t;

#endif
