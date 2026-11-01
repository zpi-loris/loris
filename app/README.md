# Overview

_loris_ client app implemented using .NET with ASP.NET core framework

# Development

## Requirements

You need to have dotnet SDK which also includes the runtime. For installation
follow https://dotnet.microsoft.com/en-us/download

## Building and running

To build and run the app use

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

#### Chirpstack integration

For chirpstack integration operation there are currently 4 environmental variables defined for client configuration:

## Running tests

To run test use:

```bash
# from app/ directory point of view
dotnet test test/lorisTests
```
