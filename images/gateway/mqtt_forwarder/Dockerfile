FROM debian:13.7

ARG MQTT_FORWARDER_VERSION="4.6.1"

WORKDIR /chirpstack

RUN apt-get update && apt-get install -y \
  netcat-openbsd \
  gettext-base && rm -rf /var/cache/apt/archives /var/lib/apt/lists/*

ADD --unpack=true "https://artifacts.chirpstack.io/downloads/chirpstack-mqtt-forwarder/chirpstack-mqtt-forwarder_${MQTT_FORWARDER_VERSION}_linux_arm64.tar.gz" .

COPY ./forwarder.template.toml .
COPY ./start .

RUN chmod +x ./start

RUN groupadd --gid 1000 chirpstack \
  && useradd --uid 1000 --gid 1000 --no-create-home chirpstack

RUN chown chirpstack:chirpstack --recursive .

USER chirpstack

CMD ["./start"]

