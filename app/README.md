# Overview

_loris_ client app implemented using .NET with ASP.NET core framework

# Development

## Requirements

You need to have dotnet SDK which also includes the runtime. For installation
follow https://dotnet.microsoft.com/en-us/download

## Building and running

To build and run the app use

> [!IMPORTANT]
>
> Remember to set **required** environmental variables before running

```bash
# from app/ directory point of view
dotnet build src/loris
dotnet src/loris/bin/Debug/net10.0

# or just use inside the src/loris directory
#
# this is for development
dotnet run
```

These will install any neede dependencies, build the project and run it
respectively

### Configuration

To configure the application you can use both **Environmental variables** or
`appsettings.Development.json`. **Keep in mind that Environmental variables will
take precedence before json file**. The options are the same across two
providers.

For the example json file, see `appsettings.Development.example.json`. You can
**copy it**, fill according to your setup, and remove the `.example` infix of
your copy.

> [!NOTE]
>
> Note that `appsettings.Development.json` is excluded from git tracking in
> _gitignore_

> [!CAUTION]
>
> Keep in mind that for dotnet it is **double underscore (\__)** for nested
> environmental variables

#### TimescaleDB connection

In order to use database connection for development, you need to provide a
complete connection string for your local database. An example setup using
docker is defined in the repository root `/images/timescaledb`. You have a
complete setup there, with pgadmin for gui access to database.

- **required**:
  - `ConnectionStrings__Timescale` - connection string for DbContext
    configuration

```bash
# example connection string, constructed from the example setup in /images/timescaledb

"Host=localhost;Port=6543;Database=loris;Username=postgres;Password=password"
```

#### Chirpstack MQTT integration

MQTT uplink and application data _coming from_ the Chirpstack

- **required**:
  - `Chirpstack__Integration__Host` - specifies chirpstack MQTT broker ip
    address or domain name
- **optional**:
  - `Chirpstack__Integration__Port` - specifies chirstpack MQTT broker port. -
    _**1883 if ommited**_
  - `Chirpstack__Integration__ApplicationId` - specifies chirstpack
    application ID for advanced loris deployments
  - `Chirpstack__Integration__ReconnectInterval` - specifies the amount of
    time (in seconds) to wait before next reconnect attempt. - _**5s if
    ommited**_
  - `Chirpstack__Integration__ConnectTimeout` - specifies the amount of time
    (in seconds) for connect timeout. It does not stop the app and works with
    ReconnectInterval - _**5s if ommited**_

#### Chirpstack device service

Devices and downlink service - data _going to_ the chirpstack

- **required**:
  - `Chirpstack__DeviceService__Host` - specifies the domain or address
    hosting api
  - `Chirpstack__DeviceService__ApiToken` - Api token used for authorization
    to interact with **chirpstack's Tenant**. Can be obtained from the
    chirpstack cli or web GUI
- **optional**:
  - `Chirpstack__DeviceService__Port` - specified api port. - _**8080 if
    ommited**_

## Running tests

To run test use:

```bash
# from app/ directory point of view
dotnet test test/lorisTests
```
