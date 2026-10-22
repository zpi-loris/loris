FROM alpine:3.24.3

ARG MQTT_FORWARDER_VERSION="4.6.1"
ARG CONCENTRATORD_VERSION="4.7.1"

WORKDIR /chirpstack

ADD --unpack=true "https://artifacts.chirpstack.io/downloads/chirpstack-concentratord/chirpstack-concentratord-sx1302_${CONCENTRATORD_VERSION}_linux_arm64.tar.gz" .

COPY ./concentratord.toml .

RUN addgroup -g 1000 chirpstack && \
  adduser -u 1000 -G chirpstack -H -D chirpstack

RUN chown chirpstack:chirpstack --recursive .

USER chirpstack

CMD ["./chirpstack-concentratord-sx1302", "-c", "./concentratord.toml"]

