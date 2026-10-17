#!/bin/bash

set -e
mkdir -p build
cd build
cmake -G "CodeBlocks - Unix Makefiles" -DCMAKE_EXPORT_COMPILE_COMMANDS=ON ..
make
cd ..
size build/rpi-pingpong
