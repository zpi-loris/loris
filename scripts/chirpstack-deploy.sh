#!/usr/bin/env bash

TARGET_DIR="/opt/chirpstack-docker"

# exit early if any command fails
set -eo pipefail

# auto answer default values on all interactive commands
export DEBIAN_FRONTEND=noninteractive

fatal() {
    echo "[ERROR] " "$@" >&2
    exit 1
}

help() {
    cat << EOF

chirpstack-deploy [-t]

--help
    see this dialog

-t  --target-dir
    Specify the target directory where the script will download the chirpstack repository.
    Default directory is /opt/chirpstack-docker

EOF
}

parse-args() {
    while [[ $# -gt 0 ]]; do
        case $1 in
            --help)
                help
                exit 0
                ;;
            -t|--target-dir)
                if [[ $# -lt 2 ]]; then
                    fatal "$1 requires an argument"
                fi
                TARGET_DIR="$2"
                shift 2
                ;;
            *)
                fatal "The script does not accept positional arguments"
                shift
                ;;
        esac
    done;
}

main() {
    parse-args "$@"

    if [[ "$(id -u)" -ne 0 ]]; then
        fatal "This script must be run as root"
    fi

    # remove docker colliding dependencies
    echo "Cleaning up conflicting docker packages"
    apt-get remove -y $(dpkg --get-selections docker.io docker-compose docker-doc docker-buildx podman-docker containerd runc | cut -f1) 2>/dev/null || true

    echo "Installing required packages... and vim ofc"
    apt-get update -qq
    apt-get install -y -qq ca-certificates curl yq git vim

    # configure docker Apt repo
    install -m 0755 -d /etc/apt/keyrings
    curl -fsSL https://download.docker.com/linux/debian/gpg -o /etc/apt/keyrings/docker.asc
    chmod a+r /etc/apt/keyrings/docker.asc

    tee /etc/apt/sources.list.d/docker.sources <<EOF
Types: deb
URIs: https://download.docker.com/linux/debian
Suites: $(. /etc/os-release && echo "$VERSION_CODENAME")
Components: stable
Architectures: $(dpkg --print-architecture)
Signed-By: /etc/apt/keyrings/docker.asc
EOF

    # install docker
    echo "Installing docker"
    apt-get update -qq
    apt-get install -y -qq docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin

    # start docker daemon
    systemctl enable --now docker

    # download chirpstack
    if [ ! -d "$TARGET_DIR" ]; then
        git clone -q https://github.com/chirpstack/chirpstack-docker.git "$TARGET_DIR"
    else
        echo "$TARGET_DIR already exists!"
    fi

    cd $TARGET_DIR

    # check if

    # remove gateway bridge components from compose
    cat << EOF
Removing services from docker-compose.yml file:
- chirpstack-gateway-bridge
- chirpstack-gateway-bridge-basicstation
are not needed.

Original file will be under ./docker-compose.yml.bak
EOF
    mv -n \
        docker-compose.yml \
        docker-compose.yml.bak \
        && yq \
        --yaml-output \
        'del(.services | .["chirpstack-gateway-bridge"], .["chirpstack-gateway-bridge-basicstation"])' \
        docker-compose.yml.bak \
        > docker-compose.yml

    echo "Starting services"
    docker compose up -d --quiet-pull --quiet-build

    echo -e "\n\nActive services are:"
    docker ps --filter "status=running" --filter "name=chirpstack*" --format "{{.Names}}\nPORTS: {{.Ports}}\n"

}

main "$@"
