FROM debian:13.7

ARG MQTT_FORWARDER_VERSION="4.6.1"
ARG CONCENTRATORD_VERSION="4.7.1"

WORKDIR /chirpstack

ADD --unpack=true "https://artifacts.chirpstack.io/downloads/chirpstack-concentratord/chirpstack-concentratord-sx1302_${CONCENTRATORD_VERSION}_linux_arm64.tar.gz" .

COPY ./concentratord.toml .

RUN groupadd --gid 1000 chirpstack \
  && useradd --uid 1000 --gid 1000 --no-create-home chirpstack

RUN chown chirpstack:chirpstack --recursive .

USER chirpstack

CMD ["./chirpstack-concentratord-sx1302", "-c", "./concentratord.toml"]

