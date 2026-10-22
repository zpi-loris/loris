# Overview

ChirpStack Concentratord and MQTT-Forwarder images to use on gateways.

# Usage

First, ensure these devices are available:
- `/dev/spidev0.0`,
- `/dev/gpiochip0`,
- `/dev/i2c-1`,
- `/dev/ttyAMA0`.

Then run:
```bash
cd <project_dir>
sudo ./setup-devices run
```

> [!WARNING]
> The `setup-devices` script by default masks a conflicting systemd service (if
> it's running or enabled) `serial-getty@ttyAMA0.service` to ensure consistent
> behavior across reboots, which may or may not be desirable. For example,
> masking it will disable the option to login via UART.
> 
> Feel free to use the '--no-mask' flag and ensure no `/dev/ttyAMA0` conflicts in
> a different way.

This should prepare the above-listed devices and generate an `.env` file that will
look like this:
```env
GPIO_GID=986
SPI_GID=989
DIALOUT_GID=20
I2C_GID=987
```

Now you can run:
```bash
docker compose up
```

# Configuration

Configuration happens by setting environment variables in the
`docker-compose.yml` file.

## ChirpStack MQTT-Forwarder
1. `MQTT_BROKER_ADDR` (MANDATORY) - full address of the MQTT broker
   (scheme://host:port), e.g. *tcp://0.0.0.0:1883*,
2. `MQTT_USERNAME` (OPTIONAL) - MQTT connection username,
3. `MQTT_PASSWORD` (OPTIONAL) - MQTT connection password.
