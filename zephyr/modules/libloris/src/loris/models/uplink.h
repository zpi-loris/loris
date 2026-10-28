#ifndef LORIS_UPLINK_H
#define LORIS_UPLINK_H

#include "loris/models/report.h"
#include "loris/models/capabilities.h"
#include "loris/models/capabilities.h"

// TODO: substitute with new ping message
typedef struct hello {
  char dummy_field;
} hello_t;

typedef struct disconnect {
  char dummy_field;
} disconnect_t;

typedef enum uplink_payload_type {
  UPLINK_PAYLOAD_TYPE_REPORT = 1,
  UPLINK_PAYLOAD_TYPE_CAPABILITIES = 2,
  UPLINK_PAYLOAD_TYPE_HELLO = 3,
} uplink_payload_type_t;

typedef struct uplink {
  uplink_payload_type_t payload_type;
  union {
    report_t report;
    capabilities_t capabilities;
    hello_t hello;
  } payload;
} uplink_t;

#endif
