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

### Environmental variables

> [!CAUTION]
>
> Keep in mind that for dotnet it is **double underscore (\__)** for nested
> environmental variables

#### Chirpstack integration

For chirpstack integration operation there are currently 4 environmental
variables defined for client configuration:

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

## Running tests

To run test use:

```bash
# from app/ directory point of view
dotnet test test/lorisTests
```
