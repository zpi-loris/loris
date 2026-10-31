#ifndef LORIS_UPLINK_H
#define LORIS_UPLINK_H

#include "loris/models/report.h"
#include "loris/models/capabilities.h"
#include "loris/protobuf/protocol.pb.h"

typedef enum up_ping_type {
  UP_PING_TYPE_UNSPECIFIED = PROTOCOL_V1_UP_PING_TYPE_UP_PING_TYPE_UNSPECIFIED,
  UP_PING_TYPE_HELLO       = PROTOCOL_V1_UP_PING_TYPE_UP_PING_TYPE_HELLO
} up_ping_type_t;

BUILD_ASSERT((int)UP_PING_TYPE_UNSPECIFIED == (int)_PROTOCOL_V1_UP_PING_TYPE_MIN, "Enums don't match, possible missing members");
BUILD_ASSERT((int)UP_PING_TYPE_HELLO       == (int)_PROTOCOL_V1_UP_PING_TYPE_MAX, "Enums don't match, possible missing members");

typedef struct up_ping {
  up_ping_type_t type;
} up_ping_t;

typedef enum uplink_payload_type {
  UPLINK_PAYLOAD_TYPE_REPORT       = PROTOCOL_V1_UPLINK_REPORT_TAG,
  UPLINK_PAYLOAD_TYPE_CAPABILITIES = PROTOCOL_V1_UPLINK_CAPABILITIES_TAG,
  UPLINK_PAYLOAD_TYPE_PING         = PROTOCOL_V1_UPLINK_PING_TAG,
} uplink_payload_type_t;

typedef struct uplink {
  uplink_payload_type_t payload_type;
  union {
    report_t report;
    capabilities_t capabilities;
    up_ping_t ping;
  } payload;
} uplink_t;

#endif
