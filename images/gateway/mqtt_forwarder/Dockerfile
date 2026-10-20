FROM debian:13.7

ARG MQTT_FORWARDER_VERSION="4.6.1"

WORKDIR /chirpstack

# NOTE: move into a single layer
RUN apt-get update && apt-get install -y \
  netcat-traditional \
  gettext-base && rm -rf /var/cache/apt/archives /var/lib/apt/lists/*

ADD --unpack=true "https://artifacts.chirpstack.io/downloads/chirpstack-mqtt-forwarder/chirpstack-mqtt-forwarder_${MQTT_FORWARDER_VERSION}_linux_arm64.tar.gz" .

COPY ./forwarder.template.toml .
COPY ./start .

RUN chmod +x ./start

RUN useradd chirpstack
RUN chown chirpstack:chirpstack --recursive .

USER chirpstack

CMD ["./start"]

