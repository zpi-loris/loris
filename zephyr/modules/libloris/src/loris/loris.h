/*
 * Copyright (c) 2026 Jan Kubów
 *
 * SPDX-License-Identifier: MIT
 */

#include "loris/models/capabilities.h"

#ifndef LIBLORIS_H
#define LIBLORIS_H

/*
 * @brief Start the device. Gather data from sensors and send it and/or respond
 * to the incoming function calls
 *
 *
 * @param capabilities Capabilities of the device
 */
void loris_start(capabilities_t* capabilties);

#endif
