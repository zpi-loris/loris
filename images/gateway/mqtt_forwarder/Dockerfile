FROM alpine:3.24.3

ARG MQTT_FORWARDER_VERSION="4.6.1"

WORKDIR /chirpstack

# RUN apt update && apt install -y \
#   netcat-openbsd \
#   gettext-base && rm -rf /var/cache/apt/archives /var/lib/apt/lists/*

RUN apk add --no-cache \
  netcat-openbsd \
  bash \
  gettext

ADD --unpack=true "https://artifacts.chirpstack.io/downloads/chirpstack-mqtt-forwarder/chirpstack-mqtt-forwarder_${MQTT_FORWARDER_VERSION}_linux_arm64.tar.gz" .

COPY ./forwarder.template.toml .
COPY ./start .

RUN chmod +x ./start

RUN addgroup -g 1000 chirpstack && \
  adduser -u 1000 -G chirpstack -H -D chirpstack

RUN chown chirpstack:chirpstack --recursive .

USER chirpstack

CMD ["./start"]

