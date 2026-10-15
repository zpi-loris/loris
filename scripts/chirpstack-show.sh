#!/usr/bin/env bash

docker ps --filter "status=running" --filter "name=chirpstack*" --format "{{.Names}}\nPORTS: {{.Ports}}\n"
