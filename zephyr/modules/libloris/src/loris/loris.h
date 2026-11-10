/*
 * Copyright (c) 2026 Jan Kubów
 *
 * SPDX-License-Identifier: MIT
 */

#include "loris/lorawan.h"
#include "loris/models/capabilities.h"

#ifndef LIBLORIS_H
#define LIBLORIS_H

typedef union act_cfg {
  lr_lw_join_otaa_config_t otaa;
  lr_lw_join_abp_config_t abp;
} act_cfg_u;

/*
 * @brief Start the device. Gather data from sensors and send it and/or respond
 * to the incoming function calls
 *
 *
 * @param capabilities Capabilities of the device
 */
void loris_start(capabilities_t* capabilties);

#endif
