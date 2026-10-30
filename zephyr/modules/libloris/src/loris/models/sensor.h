#ifndef LORIS_SENSOR_H
#define LORIS_SENSOR_H

#include <inttypes.h>
#include <stdbool.h>
#include <zephyr/sys/slist.h>

#include "loris/models/report.h"
#include "loris/str.h"

/*
 * Value type used in capabilities.sensor
 */
typedef enum value_type {
  /* General */
  VALUE_TYPE_UNSPECIFIED = 0,
  VALUE_TYPE_NUMERIC = 1,
  VALUE_TYPE_TEXT = 2,
  VALUE_TYPE_BOOL = 3,
  /* Common */
  VALUE_TYPE_TEMPERATURE = 4,
  VALUE_TYPE_PRESSURE = 5,
  VALUE_TYPE_FREQUENCY = 6,
  VALUE_TYPE_MOISTURE = 7,
  VALUE_TYPE_UPTIME = 8,
  VALUE_TYPE_AREA = 9,
  VALUE_TYPE_DISTANCE = 10,
  VALUE_TYPE_DURATION = 11,
  VALUE_TYPE_ILLUMINANCE = 12,
  VALUE_TYPE_WATER = 13,
  VALUE_TYPE_WEIGHT = 14,
  /* Air index */
  VALUE_TYPE_AQI = 15,
  VALUE_TYPE_CO2 = 16,
  VALUE_TYPE_PM1 = 17,
  VALUE_TYPE_PM25 = 18,
  VALUE_TYPE_PM4 = 19,
  VALUE_TYPE_PM10 = 20,
  VALUE_TYPE_HUMIDITY = 21,
  /* Wind */
  VALUE_TYPE_WIND_SPEED = 22,
  /* Electricity */
  VALUE_TYPE_BATTERY = 23,
  VALUE_TYPE_ENERGY = 24,
  VALUE_TYPE_VOLTAGE = 25,
  VALUE_TYPE_CURRENT = 26,
  /* Transmission */
  VALUE_TYPE_DATA_RATE = 27,
  VALUE_TYPE_DATA_SIZE = 28,
  VALUE_TYPE_SIGNAL_STRENGTH = 29,
  /* Earth */
  VALUE_TYPE_PH = 30,
  VALUE_TYPE_POTASSIUM = 31,
  VALUE_TYPE_PHOSPHORUS = 32,
  VALUE_TYPE_NITROGEN = 33
} value_type_t;

typedef struct sensor {
  int32_t id;
  str_t name;
  str_t description;
  value_type_t value_type;

  // Determines whether 'unit' field is present
  bool has_unit;
  // Unit is optional, if not present should always be set to LORIS_STR_NONE
  str_t unit;
  report_t (*read_func)(struct sensor *sensor);
  // wake_interval_sec is optional, if not present should always be set to 0
  int32_t wake_interval_sec;
  sys_snode_t node;
} sensor_t;

#endif
