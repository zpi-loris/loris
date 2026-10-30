#ifndef LORIS_UPLINK_H
#define LORIS_UPLINK_H

#include "loris/models/capabilities.h"
#include "loris/models/report.h"

typedef enum up_ping_type {
  UP_PING_TYPE_UNSPECIFIED = 0,
  UP_PING_TYPE_HELLO = 1
} up_ping_type_t;

typedef struct up_ping {
  up_ping_type_t type;
} up_ping_t;

typedef enum uplink_payload_type {
  UPLINK_PAYLOAD_TYPE_REPORT = 1,
  UPLINK_PAYLOAD_TYPE_CAPABILITIES = 2,
  UPLINK_PAYLOAD_TYPE_PING = 3,
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
