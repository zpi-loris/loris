# SPDX-License-Identifier: Apache-2.0

cmake_minimum_required(VERSION 3.28.0)

# list(APPEND EXTRA_ZEPHYR_MODULES
#   ${CMAKE_CURRENT_SOURCE_DIR}/../../common
# )

find_package(Zephyr REQUIRED HINTS $ENV{ZEPHYR_BASE})
project(blinky)


target_sources(app PRIVATE src/main.c)
