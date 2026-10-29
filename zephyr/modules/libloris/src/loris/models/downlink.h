#ifndef LORIS_DOWNLINK_H
#define LORIS_DOWNLINK_H

#include "loris/models/call.h"
#include "loris/protobuf/protocol.pb.h"

typedef enum down_ping_type {
  DOWN_PING_TYPE_UNSPECIFIED = PROTOCOL_V1_DOWN_PING_TYPE_DOWN_PING_TYPE_UNSPECIFIED,
  DOWN_PING_TYPE_HELLO       = PROTOCOL_V1_DOWN_PING_TYPE_DOWN_PING_TYPE_HELLO,
  DOWN_PING_TYPE_DISCONNECT  = PROTOCOL_V1_DOWN_PING_TYPE_DOWN_PING_TYPE_DISCONNECT
} down_ping_type_t;

BUILD_ASSERT((int)DOWN_PING_TYPE_UNSPECIFIED == (int)_PROTOCOL_V1_DOWN_PING_TYPE_MIN, "Enums don't match, possible missing members");
BUILD_ASSERT((int)DOWN_PING_TYPE_DISCONNECT  == (int)_PROTOCOL_V1_DOWN_PING_TYPE_MAX, "Enums don't match, possible missing members");

typedef struct down_ping {
  down_ping_type_t type;
} down_ping_t;

typedef enum downlink_payload_type {
  DOWNLINK_PAYLOAD_TYPE_CALL = PROTOCOL_V1_DOWNLINK_CALL_TAG,
  DOWNLINK_PAYLOAD_TYPE_PING = PROTOCOL_V1_DOWNLINK_PING_TAG,
} downlink_payload_type_t;

typedef struct downlink {
  downlink_payload_type_t payload_type;
  union {
    call_t call;
    down_ping_t ping;
  } payload;
} downlink_t;

#endif
