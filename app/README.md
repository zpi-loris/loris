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

> [!IMPORTANT]
>
> To run tests you need to have a configured podmand/docker environment. The
> integration tests will spin up a temporary timescaledb container and run the
> tests against it. The containers will be created for each test class and will
> be removed after the tests are finished. The tests will fail if the container
> cannot be created.
>
> **It is highly recommended to use podman instead of the docker, since podman
> allows to run rootless, hence we can run the tests as a regular user**.
>
> If you are running linux,
> [here](https://podman.io/docs/installation#installing-on-linux) is the
> podman's documentation on installation. Remember that you need to have a
> regisries configured, in order to successfuly resolve unqualified image names
> referenced in the test environment, as well as be authenticated to request the
> images from such registries. There are guides on how to configure registries
> [from podman desktop guide](https://podman-desktop.io/docs/containers/registries)
> or a more recommended
> [blog on oneuptime](https://oneuptime.com/blog/post/2026-01-27-podman-registry/view).
>
> Generally you should be good to go after creating a file in:
>
> ```bash
> /etc/containers/registries.conf # Globally OR
> ~/.config/containers/registries.conf # for a currently logged in user
>
> ```
>
> With given content:
>
> ```conf
> # Specify registries to search when pulling unqualified images
> # (images without a registry prefix like "nginx" instead of "docker.io/library/nginx")
> unqualified-search-registries = ["docker.io", "quay.io", "ghcr.io"]
>
> # Short name mode controls how Podman handles unqualified image names
> # "enforcing" - prompt when multiple registries are configured and a TTY is available; error in non-interactive ambiguous cases
> # "permissive" - prompt like enforcing when possible; otherwise try all configured registries
> # "disabled" - try all configured registries without prompting
> short-name-mode = "permisive"
> ```
>
> After you create these, you can authenticate to each of the registry involed,
> but you should bee good to go, just authenticated to `docker.io`. Use your
> regular docker registry credentials you used during docker installation
> (assuming you used docker before), or sign up in docker hub.

```bash
# run these from app/ directory

# IF YOU WANT TO USE PODMAN, BUT HAVE DOCKER INSTALLED, YOU NEED TO SET THE ENVIRONMENT VARIABLE
export DOCKER_HOST=unix://${XDG_RUNTIME_DIR}/podman/podman.sock && \
dotnet test && \
unset DOCKER_HOST

# IF YOU WANT TO USE DOCKER, AND YOU HAVE REQUIREMENTS FOR DOCKER, YOU CAN JUST RUN
dotnet test
```
