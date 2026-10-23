#!/usr/bin/env bash

# Use this script to activate zephyr environment
ZEPHYR_DIR="/home/kyloof/Projects/zephyr_project"
VENV="$ZEPHYR_DIR/.env/bin/activate"
ZEPHYR_ENV="$ZEPHYR_DIR/zephyr/zephyr-env.sh"

source "$VENV" && source "$ZEPHYR_ENV"
