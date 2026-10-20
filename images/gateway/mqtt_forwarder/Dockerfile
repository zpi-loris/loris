FROM debian:13.7

ARG MQTT_FORWARDER_VERSION="4.6.1"

WORKDIR /chirpstack

# NOTE: move into a single layer
RUN apt-get update
RUN apt-get install -y \
  git \
  gcc \
  make \
  bash \
  gettext-base \
  vim

ADD --unpack=true "https://artifacts.chirpstack.io/downloads/chirpstack-mqtt-forwarder/chirpstack-mqtt-forwarder_${MQTT_FORWARDER_VERSION}_linux_arm64.tar.gz" .

COPY ./forwarder.template.toml .
COPY ./start .

RUN chmod +x ./start

# RUN useradd chirpstack
# RUN chown chirpstack:chirpstack --recursive .
#
# USER chirpstack

CMD ["./start"]

