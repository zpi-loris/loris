FROM debian:13.7

ARG MQTT_FORWARDER_VERSION="4.6.1"
ARG CONCENTRATORD_VERSION="4.7.1"

WORKDIR /chirpstack

ADD --unpack=true "https://artifacts.chirpstack.io/downloads/chirpstack-concentratord/chirpstack-concentratord-sx1302_${CONCENTRATORD_VERSION}_linux_arm64.tar.gz" .

COPY ./concentratord.toml .

RUN useradd chirpstack
RUN chown chirpstack:chirpstack --recursive .

USER chirpstack

CMD ["./chirpstack-concentratord-sx1302", "-c", "./concentratord.toml"]

