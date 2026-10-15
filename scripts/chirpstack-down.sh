#!/bin/bash

# in case compose is lost, easy way to stop all
for container in $(docker ps --format "{{.ID}}" --filter "name=chirpstack*"); do
    docker stop $container | xargs docker rm
done
