using System.Net;
using System.Text.Json;
using Loris.Data;
using Loris.Dtos.Device;
using loris.IntegrationTests.Fakes;
using Loris.Models;
using Loris.Protocol;
using Loris.Results;
using loris.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Options;

[assembly: CaptureConsole]

namespace loris.IntegrationTests.Controllers;

[Collection(TimescaleDbApplicationFactory.CollectionKey)]
public sealed class DevicesControllerTests(
    TimescaleDbApplicationFactory timescaleDbApplicationFactory
) : IAsyncLifetime
{
    private readonly TimescaleDbApplicationFactory _appFactory = timescaleDbApplicationFactory;

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public async ValueTask DisposeAsync()
    {
        await _appFactory.ResetDatabaseAsync();
        ((FakeStateCache)_appFactory.Services.GetRequiredService<IStateCache>()).Clear();
    }

    private WebApplicationFactory<Program> WithChirpstackService<TFake>()
        where TFake : class, IChirpstackService =>
        _appFactory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
                services.AddTransient<IChirpstackService, TFake>()
            )
        );

    private static Device CreateDevice(
        DeviceStatus status = DeviceStatus.ACTIVE,
        string devEui = "1122AABBCCDDEEFF",
        string friendlyName = "loris_friendly",
        string deviceName = "loris_sample",
        bool withCapabilities = false
    )
    {
        var device = new Device
        {
            DeviceName = deviceName,
            DevEui = DevEui.Parse(devEui),
            FriendlyName = friendlyName,
            LastSeen = DateTime.UtcNow,
            DeviceStatus = status,
        };

        if (withCapabilities)
        {
            device.Sensors.Add(
                new Loris.Models.Sensor
                {
                    DeviceSensorId = 1,
                    Name = "env_temp_sensor",
                    Description = string.Empty,
                    ValueType = SensorValueType.Float,
                    Semantics = SensorSemantics.Temperature,
                    Unit = "C",
                }
            );
            device.Functions.Add(
                new Loris.Models.Function
                {
                    DeviceFunctionId = 2,
                    Name = "set_led_green",
                    Description = "Brightness: 0-100",
                    ArgType = FuncArgType.Int,
                }
            );
        }

        return device;
    }

    private static async Task SeedDevicesAsync(
        WebApplicationFactory<Program> factory,
        params Device[] devices
    )
    {
        using var scope = factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<LorisDbContext>();
        ctx.Devices.AddRange(devices);
        await ctx.SaveChangesAsync(CancellationToken.None);
    }

    private static JsonSerializerOptions GetJsonOptions(WebApplicationFactory<Program> factory) =>
        factory.Services.GetRequiredService<IOptions<JsonOptions>>().Value.JsonSerializerOptions;

    [Fact]
    public async Task Get_GetDevices_ReturnsDeviceDetailsResponseListWhenMoreThanOneExists()
    {
        // Arrange
        var device = CreateDevice(withCapabilities: true);
        await SeedDevicesAsync(_appFactory, device);

        var jsonOptions = GetJsonOptions(_appFactory);
        var expectedResponse = DeviceDetailsResponse.FromModel(device);
        var client = _appFactory.CreateClient();

        // Act
        var httpResponse = await client.GetAsync("/api/devices/", CancellationToken.None);
        var devicesResponse = await httpResponse.Content.ReadFromJsonAsync<
            IEnumerable<DeviceDetailsResponse>
        >(jsonOptions, CancellationToken.None);

        // Assert
        httpResponse.EnsureSuccessStatusCode();
        var firstDevice = devicesResponse!.FirstOrDefault();
        Assert.NotNull(firstDevice);
        Assert.EquivalentWithExclusions(expectedResponse, firstDevice, [d => d.LastSeen]); // exclude datetime since postgress looses precision
    }

    [Theory]
    [InlineData(DeviceStatus.ACTIVE)]
    [InlineData(DeviceStatus.PENDING)]
    [InlineData(DeviceStatus.PENDING, DeviceStatus.IGNORED)]
    public async Task Get_GetDevices_ReturnsDeviceDetailsResponseListFilteredByStatus(
        params DeviceStatus[] status
    )
    {
        // Arrange
        var device1 = CreateDevice(
            status: DeviceStatus.ACTIVE,
            devEui: "1122AABBCCDDEEFF",
            friendlyName: "loris_friendly_1",
            deviceName: "loris_sample_1"
        );
        device1.Sensors.Add(
            new Loris.Models.Sensor
            {
                DeviceSensorId = 1,
                Name = "env_temp_sensor",
                Description = string.Empty,
                ValueType = SensorValueType.Float,
                Semantics = SensorSemantics.Temperature,
                Unit = "C",
            }
        );
        var device2 = CreateDevice(
            status: DeviceStatus.PENDING,
            devEui: "1122AABBCCDDEEF0",
            friendlyName: "loris_friendly_2",
            deviceName: "loris_sample_2"
        );
        var device3 = CreateDevice(
            status: DeviceStatus.IGNORED,
            devEui: "1122AABBCCDDEEF1",
            friendlyName: "loris_friendly_3",
            deviceName: "loris_sample_3"
        );
        await SeedDevicesAsync(_appFactory, device1, device2, device3);

        var jsonOptions = GetJsonOptions(_appFactory);
        var expectedResponse = new List<DeviceDetailsResponse>();
        foreach (var device in new[] { device1, device2, device3 })
        {
            if (status.Contains(device.DeviceStatus))
            {
                expectedResponse.Add(DeviceDetailsResponse.FromModel(device));
            }
        }
        var client = _appFactory.CreateClient();

        // Act
        var httpResponse = await client.GetAsync(
            $"/api/devices/?status={string.Join("&status=", status)}",
            CancellationToken.None
        );
        var devicesResponse = await httpResponse.Content.ReadFromJsonAsync<
            IEnumerable<DeviceDetailsResponse>
        >(jsonOptions, CancellationToken.None);

        // Assert
        httpResponse.EnsureSuccessStatusCode();
        Assert.Equal(expectedResponse.Count, devicesResponse!.Count());
        foreach (var expectedDevice in expectedResponse)
        {
            var actualDevice = devicesResponse.FirstOrDefault(d =>
                d.DevEui == expectedDevice.DevEui
            );
            Assert.NotNull(actualDevice);
            Assert.EquivalentWithExclusions(expectedDevice, actualDevice, [d => d.LastSeen]); // exclude datetime since postgress looses precision
        }
    }

    [Fact]
    public async Task Get_GetById_ReturnsDeviceDetailsResponseWhenDeviceExists()
    {
        // Arrange
        var device = CreateDevice(withCapabilities: true);
        await SeedDevicesAsync(_appFactory, device);

        var jsonOptions = GetJsonOptions(_appFactory);
        var expectedResponse = DeviceDetailsResponse.FromModel(device);
        var client = _appFactory.CreateClient();

        // Act
        var httpResponse = await client.GetAsync(
            $"/api/devices/by-id/{device.DeviceId}",
            CancellationToken.None
        );
        var deviceResponse = await httpResponse.Content.ReadFromJsonAsync<DeviceDetailsResponse>(
            jsonOptions,
            CancellationToken.None
        );

        // Assert
        httpResponse.EnsureSuccessStatusCode();
        Assert.NotNull(deviceResponse);
        Assert.EquivalentWithExclusions(expectedResponse, deviceResponse, [d => d.LastSeen]);
    }

    [Fact]
    public async Task Get_GetById_ReturnsNotFoundWhenDeviceDoesNotExist()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        var jsonOptions = GetJsonOptions(_appFactory);
        var client = _appFactory.CreateClient();

        // Act
        var httpResponse = await client.GetAsync(
            $"/api/devices/by-id/{nonExistentId}",
            CancellationToken.None
        );
        var problem = await httpResponse.Content.ReadFromJsonAsync<ResultProblemDetails>(
            jsonOptions,
            CancellationToken.None
        );

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, httpResponse.StatusCode);
        Assert.NotNull(problem);
        Assert.Equal((int)HttpStatusCode.NotFound, problem.Status);
    }

    [Fact]
    public async Task Get_GetByEui_ReturnsDeviceDetailsResponseWhenDeviceExists()
    {
        // Arrange
        var device = CreateDevice(withCapabilities: true);
        await SeedDevicesAsync(_appFactory, device);

        var jsonOptions = GetJsonOptions(_appFactory);
        var expectedResponse = DeviceDetailsResponse.FromModel(device);
        var client = _appFactory.CreateClient();

        // Act
        var httpResponse = await client.GetAsync(
            $"/api/devices/by-eui/{device.DevEui}",
            CancellationToken.None
        );
        var deviceResponse = await httpResponse.Content.ReadFromJsonAsync<DeviceDetailsResponse>(
            jsonOptions,
            CancellationToken.None
        );

        // Assert
        httpResponse.EnsureSuccessStatusCode();
        Assert.NotNull(deviceResponse);
        Assert.EquivalentWithExclusions(expectedResponse, deviceResponse, [d => d.LastSeen]);
    }

    [Fact]
    public async Task Get_GetByEui_ReturnsNotFoundWhenDeviceDoesNotExist()
    {
        // Arrange
        var nonExistentEui = DevEui.Parse("FFEEDDCCBBAA2211");
        var jsonOptions = GetJsonOptions(_appFactory);
        var client = _appFactory.CreateClient();

        // Act
        var httpResponse = await client.GetAsync(
            $"/api/devices/by-eui/{nonExistentEui}",
            CancellationToken.None
        );
        var problem = await httpResponse.Content.ReadFromJsonAsync<ResultProblemDetails>(
            jsonOptions,
            CancellationToken.None
        );

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, httpResponse.StatusCode);
        Assert.NotNull(problem);
        Assert.Equal((int)HttpStatusCode.NotFound, problem.Status);
    }

    [Fact]
    public async Task Get_GetByName_ReturnsDeviceDetailsResponseWhenDeviceExists()
    {
        // Arrange
        var device = CreateDevice(withCapabilities: true);
        await SeedDevicesAsync(_appFactory, device);

        var jsonOptions = GetJsonOptions(_appFactory);
        var expectedResponse = DeviceDetailsResponse.FromModel(device);
        var client = _appFactory.CreateClient();

        // Act
        var httpResponse = await client.GetAsync(
            $"/api/devices/by-name/{device.FriendlyName}",
            CancellationToken.None
        );
        var deviceResponse = await httpResponse.Content.ReadFromJsonAsync<DeviceDetailsResponse>(
            jsonOptions,
            CancellationToken.None
        );

        // Assert
        httpResponse.EnsureSuccessStatusCode();
        Assert.NotNull(deviceResponse);
        Assert.EquivalentWithExclusions(expectedResponse, deviceResponse, [d => d.LastSeen]);
    }

    [Fact]
    public async Task Get_GetByName_ReturnsNotFoundWhenDeviceDoesNotExist()
    {
        // Arrange
        var jsonOptions = GetJsonOptions(_appFactory);
        var client = _appFactory.CreateClient();

        // Act
        var httpResponse = await client.GetAsync(
            "/api/devices/by-name/non_existent_device",
            CancellationToken.None
        );
        var problem = await httpResponse.Content.ReadFromJsonAsync<ResultProblemDetails>(
            jsonOptions,
            CancellationToken.None
        );

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, httpResponse.StatusCode);
        Assert.NotNull(problem);
        Assert.Equal((int)HttpStatusCode.NotFound, problem.Status);
    }

    [Fact]
    public async Task Patch_RenameById_UpdatesFriendlyNameAndStateCacheWhenValid()
    {
        // Arrange
        var device = CreateDevice();
        await SeedDevicesAsync(_appFactory, device);

        var request = new RenameDeviceRequest { NewFriendlyName = "loris_renamed" };
        var jsonOptions = GetJsonOptions(_appFactory);
        var stateCache = _appFactory.Services.GetRequiredService<IStateCache>();
        var client = _appFactory.CreateClient();

        // Act
        var httpResponse = await client.PatchAsJsonAsync(
            $"/api/devices/by-id/{device.DeviceId}",
            request,
            jsonOptions,
            CancellationToken.None
        );
        var deviceResponse = await httpResponse.Content.ReadFromJsonAsync<DeviceResponse>(
            jsonOptions,
            CancellationToken.None
        );

        // Assert
        httpResponse.EnsureSuccessStatusCode();
        Assert.NotNull(deviceResponse);
        Assert.Equal("loris_renamed", deviceResponse.FriendlyName);
        Assert.True(stateCache.TryGetDevice(device.DevEui, out var cachedDevice));
        Assert.Equal("loris_renamed", cachedDevice.FriendlyName);
    }

    [Fact]
    public async Task Patch_RenameById_ReturnsNotFoundWhenDeviceDoesNotExist()
    {
        // Arrange
        var request = new RenameDeviceRequest { NewFriendlyName = "loris_renamed" };
        var jsonOptions = GetJsonOptions(_appFactory);
        var client = _appFactory.CreateClient();

        // Act
        var httpResponse = await client.PatchAsJsonAsync(
            $"/api/devices/by-id/{Guid.NewGuid()}",
            request,
            jsonOptions,
            CancellationToken.None
        );
        var problem = await httpResponse.Content.ReadFromJsonAsync<ResultProblemDetails>(
            jsonOptions,
            CancellationToken.None
        );

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, httpResponse.StatusCode);
        Assert.NotNull(problem);
        Assert.Equal((int)HttpStatusCode.NotFound, problem.Status);
    }

    [Fact]
    public async Task Patch_RenameById_ReturnsConflictWhenFriendlyNameAlreadyTaken()
    {
        // Arrange
        var device1 = CreateDevice(
            devEui: "1122AABBCCDDEEFF",
            friendlyName: "loris_friendly_1",
            deviceName: "loris_sample_1"
        );
        var device2 = CreateDevice(
            devEui: "1122AABBCCDDEEF0",
            friendlyName: "loris_friendly_2",
            deviceName: "loris_sample_2"
        );
        await SeedDevicesAsync(_appFactory, device1, device2);

        var request = new RenameDeviceRequest { NewFriendlyName = device2.FriendlyName };
        var jsonOptions = GetJsonOptions(_appFactory);
        var client = _appFactory.CreateClient();

        // Act
        var httpResponse = await client.PatchAsJsonAsync(
            $"/api/devices/by-id/{device1.DeviceId}",
            request,
            jsonOptions,
            CancellationToken.None
        );
        var problem = await httpResponse.Content.ReadFromJsonAsync<ResultProblemDetails>(
            jsonOptions,
            CancellationToken.None
        );

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, httpResponse.StatusCode);
        Assert.NotNull(problem);
        Assert.Equal((int)HttpStatusCode.Conflict, problem.Status);
    }

    [Fact]
    public async Task Patch_RenameByEui_UpdatesFriendlyNameAndStateCacheWhenValid()
    {
        // Arrange
        var device = CreateDevice();
        await SeedDevicesAsync(_appFactory, device);

        var request = new RenameDeviceRequest { NewFriendlyName = "loris_renamed" };
        var jsonOptions = GetJsonOptions(_appFactory);
        var stateCache = _appFactory.Services.GetRequiredService<IStateCache>();
        var client = _appFactory.CreateClient();

        // Act
        var httpResponse = await client.PatchAsJsonAsync(
            $"/api/devices/by-eui/{device.DevEui}",
            request,
            jsonOptions,
            CancellationToken.None
        );
        var deviceResponse = await httpResponse.Content.ReadFromJsonAsync<DeviceResponse>(
            jsonOptions,
            CancellationToken.None
        );

        // Assert
        httpResponse.EnsureSuccessStatusCode();
        Assert.NotNull(deviceResponse);
        Assert.Equal("loris_renamed", deviceResponse.FriendlyName);
        Assert.True(stateCache.TryGetDevice(device.DevEui, out var cachedDevice));
        Assert.Equal("loris_renamed", cachedDevice.FriendlyName);
    }

    [Fact]
    public async Task Patch_RenameByEui_ReturnsNotFoundWhenDeviceDoesNotExist()
    {
        // Arrange
        var nonExistentEui = DevEui.Parse("FFEEDDCCBBAA2211");
        var request = new RenameDeviceRequest { NewFriendlyName = "loris_renamed" };
        var jsonOptions = GetJsonOptions(_appFactory);
        var client = _appFactory.CreateClient();

        // Act
        var httpResponse = await client.PatchAsJsonAsync(
            $"/api/devices/by-eui/{nonExistentEui}",
            request,
            jsonOptions,
            CancellationToken.None
        );
        var problem = await httpResponse.Content.ReadFromJsonAsync<ResultProblemDetails>(
            jsonOptions,
            CancellationToken.None
        );

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, httpResponse.StatusCode);
        Assert.NotNull(problem);
        Assert.Equal((int)HttpStatusCode.NotFound, problem.Status);
    }

    [Fact]
    public async Task Patch_RenameByEui_ReturnsConflictWhenFriendlyNameAlreadyTaken()
    {
        // Arrange
        var device1 = CreateDevice(
            devEui: "1122AABBCCDDEEFF",
            friendlyName: "loris_friendly_1",
            deviceName: "loris_sample_1"
        );
        var device2 = CreateDevice(
            devEui: "1122AABBCCDDEEF0",
            friendlyName: "loris_friendly_2",
            deviceName: "loris_sample_2"
        );
        await SeedDevicesAsync(_appFactory, device1, device2);

        var request = new RenameDeviceRequest { NewFriendlyName = device2.FriendlyName };
        var jsonOptions = GetJsonOptions(_appFactory);
        var client = _appFactory.CreateClient();

        // Act
        var httpResponse = await client.PatchAsJsonAsync(
            $"/api/devices/by-eui/{device1.DevEui}",
            request,
            jsonOptions,
            CancellationToken.None
        );
        var problem = await httpResponse.Content.ReadFromJsonAsync<ResultProblemDetails>(
            jsonOptions,
            CancellationToken.None
        );

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, httpResponse.StatusCode);
        Assert.NotNull(problem);
        Assert.Equal((int)HttpStatusCode.Conflict, problem.Status);
    }

    [Fact]
    public async Task Patch_RenameByName_UpdatesFriendlyNameAndStateCacheWhenValid()
    {
        // Arrange
        var device = CreateDevice();
        await SeedDevicesAsync(_appFactory, device);

        var request = new RenameDeviceRequest { NewFriendlyName = "loris_renamed" };
        var jsonOptions = GetJsonOptions(_appFactory);
        var stateCache = _appFactory.Services.GetRequiredService<IStateCache>();
        var client = _appFactory.CreateClient();

        // Act
        var httpResponse = await client.PatchAsJsonAsync(
            $"/api/devices/by-name/{device.FriendlyName}",
            request,
            jsonOptions,
            CancellationToken.None
        );
        var deviceResponse = await httpResponse.Content.ReadFromJsonAsync<DeviceResponse>(
            jsonOptions,
            CancellationToken.None
        );

        // Assert
        httpResponse.EnsureSuccessStatusCode();
        Assert.NotNull(deviceResponse);
        Assert.Equal("loris_renamed", deviceResponse.FriendlyName);
        Assert.True(stateCache.TryGetDevice(device.DevEui, out var cachedDevice));
        Assert.Equal("loris_renamed", cachedDevice.FriendlyName);
    }

    [Fact]
    public async Task Patch_RenameByName_ReturnsNotFoundWhenDeviceDoesNotExist()
    {
        // Arrange
        var request = new RenameDeviceRequest { NewFriendlyName = "loris_renamed" };
        var jsonOptions = GetJsonOptions(_appFactory);
        var client = _appFactory.CreateClient();

        // Act
        var httpResponse = await client.PatchAsJsonAsync(
            "/api/devices/by-name/non_existent_device",
            request,
            jsonOptions,
            CancellationToken.None
        );
        var problem = await httpResponse.Content.ReadFromJsonAsync<ResultProblemDetails>(
            jsonOptions,
            CancellationToken.None
        );

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, httpResponse.StatusCode);
        Assert.NotNull(problem);
        Assert.Equal((int)HttpStatusCode.NotFound, problem.Status);
    }

    [Fact]
    public async Task Patch_RenameByName_ReturnsConflictWhenFriendlyNameAlreadyTaken()
    {
        // Arrange
        var device1 = CreateDevice(
            devEui: "1122AABBCCDDEEFF",
            friendlyName: "loris_friendly_1",
            deviceName: "loris_sample_1"
        );
        var device2 = CreateDevice(
            devEui: "1122AABBCCDDEEF0",
            friendlyName: "loris_friendly_2",
            deviceName: "loris_sample_2"
        );
        await SeedDevicesAsync(_appFactory, device1, device2);

        var request = new RenameDeviceRequest { NewFriendlyName = device2.FriendlyName };
        var jsonOptions = GetJsonOptions(_appFactory);
        var client = _appFactory.CreateClient();

        // Act
        var httpResponse = await client.PatchAsJsonAsync(
            $"/api/devices/by-name/{device1.FriendlyName}",
            request,
            jsonOptions,
            CancellationToken.None
        );
        var problem = await httpResponse.Content.ReadFromJsonAsync<ResultProblemDetails>(
            jsonOptions,
            CancellationToken.None
        );

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, httpResponse.StatusCode);
        Assert.NotNull(problem);
        Assert.Equal((int)HttpStatusCode.Conflict, problem.Status);
    }

    [Theory]
    [InlineData(DeviceStatus.PENDING, false, DeviceStatus.IGNORED)]
    [InlineData(DeviceStatus.IGNORED, true, DeviceStatus.PENDING)]
    public async Task Post_IgnoreById_UpdatesStatusAndStateCacheForValidTransitions(
        DeviceStatus initialStatus,
        bool revert,
        DeviceStatus expectedStatus
    )
    {
        // Arrange
        var device = CreateDevice(status: initialStatus);
        await SeedDevicesAsync(_appFactory, device);

        var jsonOptions = GetJsonOptions(_appFactory);
        var stateCache = _appFactory.Services.GetRequiredService<IStateCache>();
        var client = _appFactory.CreateClient();

        // Act
        var httpResponse = await client.PostAsync(
            $"/api/devices/by-id/{device.DeviceId}/ignore?revert={revert}",
            null,
            CancellationToken.None
        );
        var deviceResponse = await httpResponse.Content.ReadFromJsonAsync<DeviceResponse>(
            jsonOptions,
            CancellationToken.None
        );

        // Assert
        httpResponse.EnsureSuccessStatusCode();
        Assert.NotNull(deviceResponse);
        Assert.Equal(expectedStatus, deviceResponse.DeviceStatus);
        Assert.True(stateCache.TryGetDevice(device.DevEui, out var cachedDevice));
        Assert.Equal(expectedStatus, cachedDevice.DeviceStatus);
    }

    [Theory]
    [InlineData(DeviceStatus.IGNORED, false)]
    [InlineData(DeviceStatus.PENDING, true)]
    [InlineData(DeviceStatus.ACTIVE, false)]
    [InlineData(DeviceStatus.ACTIVE, true)]
    [InlineData(DeviceStatus.DISCONNECTED, false)]
    [InlineData(DeviceStatus.DISCONNECTED, true)]
    public async Task Post_IgnoreById_ReturnsConflictForInvalidStatusTransitions(
        DeviceStatus initialStatus,
        bool revert
    )
    {
        // Arrange
        var device = CreateDevice(status: initialStatus);
        await SeedDevicesAsync(_appFactory, device);

        var jsonOptions = GetJsonOptions(_appFactory);
        var client = _appFactory.CreateClient();

        // Act
        var httpResponse = await client.PostAsync(
            $"/api/devices/by-id/{device.DeviceId}/ignore?revert={revert}",
            null,
            CancellationToken.None
        );
        var problem = await httpResponse.Content.ReadFromJsonAsync<ResultProblemDetails>(
            jsonOptions,
            CancellationToken.None
        );

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, httpResponse.StatusCode);
        Assert.NotNull(problem);
        Assert.Equal((int)HttpStatusCode.Conflict, problem.Status);
    }

    [Fact]
    public async Task Post_IgnoreById_ReturnsNotFoundWhenDeviceDoesNotExist()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        var jsonOptions = GetJsonOptions(_appFactory);
        var client = _appFactory.CreateClient();

        // Act
        var httpResponse = await client.PostAsync(
            $"/api/devices/by-id/{nonExistentId}/ignore?revert=false",
            null,
            CancellationToken.None
        );
        var problem = await httpResponse.Content.ReadFromJsonAsync<ResultProblemDetails>(
            jsonOptions,
            CancellationToken.None
        );

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, httpResponse.StatusCode);
        Assert.NotNull(problem);
        Assert.Equal((int)HttpStatusCode.NotFound, problem.Status);
    }

    [Theory]
    [InlineData(DeviceStatus.PENDING, false, DeviceStatus.IGNORED)]
    [InlineData(DeviceStatus.IGNORED, true, DeviceStatus.PENDING)]
    public async Task Post_IgnoreByName_UpdatesStatusAndStateCacheForValidTransitions(
        DeviceStatus initialStatus,
        bool revert,
        DeviceStatus expectedStatus
    )
    {
        // Arrange
        var device = CreateDevice(status: initialStatus);
        await SeedDevicesAsync(_appFactory, device);

        var jsonOptions = GetJsonOptions(_appFactory);
        var stateCache = _appFactory.Services.GetRequiredService<IStateCache>();
        var client = _appFactory.CreateClient();

        // Act
        var httpResponse = await client.PostAsync(
            $"/api/devices/by-name/{device.FriendlyName}/ignore?revert={revert}",
            null,
            CancellationToken.None
        );
        var deviceResponse = await httpResponse.Content.ReadFromJsonAsync<DeviceResponse>(
            jsonOptions,
            CancellationToken.None
        );

        // Assert
        httpResponse.EnsureSuccessStatusCode();
        Assert.NotNull(deviceResponse);
        Assert.Equal(expectedStatus, deviceResponse.DeviceStatus);
        Assert.True(stateCache.TryGetDevice(device.DevEui, out var cachedDevice));
        Assert.Equal(expectedStatus, cachedDevice.DeviceStatus);
    }

    [Theory]
    [InlineData(DeviceStatus.IGNORED, false)]
    [InlineData(DeviceStatus.PENDING, true)]
    [InlineData(DeviceStatus.ACTIVE, false)]
    [InlineData(DeviceStatus.ACTIVE, true)]
    [InlineData(DeviceStatus.DISCONNECTED, false)]
    [InlineData(DeviceStatus.DISCONNECTED, true)]
    public async Task Post_IgnoreByName_ReturnsConflictForInvalidStatusTransitions(
        DeviceStatus initialStatus,
        bool revert
    )
    {
        // Arrange
        var device = CreateDevice(status: initialStatus);
        await SeedDevicesAsync(_appFactory, device);

        var jsonOptions = GetJsonOptions(_appFactory);
        var client = _appFactory.CreateClient();

        // Act
        var httpResponse = await client.PostAsync(
            $"/api/devices/by-name/{device.FriendlyName}/ignore?revert={revert}",
            null,
            CancellationToken.None
        );
        var problem = await httpResponse.Content.ReadFromJsonAsync<ResultProblemDetails>(
            jsonOptions,
            CancellationToken.None
        );

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, httpResponse.StatusCode);
        Assert.NotNull(problem);
        Assert.Equal((int)HttpStatusCode.Conflict, problem.Status);
    }

    [Fact]
    public async Task Post_IgnoreByName_ReturnsNotFoundWhenDeviceDoesNotExist()
    {
        // Arrange
        var jsonOptions = GetJsonOptions(_appFactory);
        var client = _appFactory.CreateClient();

        // Act
        var httpResponse = await client.PostAsync(
            "/api/devices/by-name/non_existent_device/ignore?revert=false",
            null,
            CancellationToken.None
        );
        var problem = await httpResponse.Content.ReadFromJsonAsync<ResultProblemDetails>(
            jsonOptions,
            CancellationToken.None
        );

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, httpResponse.StatusCode);
        Assert.NotNull(problem);
        Assert.Equal((int)HttpStatusCode.NotFound, problem.Status);
    }

    [Theory]
    [InlineData(DeviceStatus.PENDING, false, DeviceStatus.IGNORED)]
    [InlineData(DeviceStatus.IGNORED, true, DeviceStatus.PENDING)]
    public async Task Post_IgnoreByEui_UpdatesStatusAndStateCacheForValidTransitions(
        DeviceStatus initialStatus,
        bool revert,
        DeviceStatus expectedStatus
    )
    {
        // Arrange
        var device = CreateDevice(status: initialStatus);
        await SeedDevicesAsync(_appFactory, device);

        var jsonOptions = GetJsonOptions(_appFactory);
        var stateCache = _appFactory.Services.GetRequiredService<IStateCache>();
        var client = _appFactory.CreateClient();

        // Act
        var httpResponse = await client.PostAsync(
            $"/api/devices/by-eui/{device.DevEui}/ignore?revert={revert}",
            null,
            CancellationToken.None
        );
        var deviceResponse = await httpResponse.Content.ReadFromJsonAsync<DeviceResponse>(
            jsonOptions,
            CancellationToken.None
        );

        // Assert
        httpResponse.EnsureSuccessStatusCode();
        Assert.NotNull(deviceResponse);
        Assert.Equal(expectedStatus, deviceResponse.DeviceStatus);
        Assert.True(stateCache.TryGetDevice(device.DevEui, out var cachedDevice));
        Assert.Equal(expectedStatus, cachedDevice.DeviceStatus);
    }

    [Theory]
    [InlineData(DeviceStatus.IGNORED, false)]
    [InlineData(DeviceStatus.PENDING, true)]
    [InlineData(DeviceStatus.ACTIVE, false)]
    [InlineData(DeviceStatus.ACTIVE, true)]
    [InlineData(DeviceStatus.DISCONNECTED, false)]
    [InlineData(DeviceStatus.DISCONNECTED, true)]
    public async Task Post_IgnoreByEui_ReturnsConflictForInvalidStatusTransitions(
        DeviceStatus initialStatus,
        bool revert
    )
    {
        // Arrange
        var device = CreateDevice(status: initialStatus);
        await SeedDevicesAsync(_appFactory, device);

        var jsonOptions = GetJsonOptions(_appFactory);
        var client = _appFactory.CreateClient();

        // Act
        var httpResponse = await client.PostAsync(
            $"/api/devices/by-eui/{device.DevEui}/ignore?revert={revert}",
            null,
            CancellationToken.None
        );
        var problem = await httpResponse.Content.ReadFromJsonAsync<ResultProblemDetails>(
            jsonOptions,
            CancellationToken.None
        );

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, httpResponse.StatusCode);
        Assert.NotNull(problem);
        Assert.Equal((int)HttpStatusCode.Conflict, problem.Status);
    }

    [Fact]
    public async Task Post_IgnoreByEui_ReturnsNotFoundWhenDeviceDoesNotExist()
    {
        // Arrange
        var nonExistentEui = DevEui.Parse("FFEEDDCCBBAA2211");
        var jsonOptions = GetJsonOptions(_appFactory);
        var client = _appFactory.CreateClient();

        // Act
        var httpResponse = await client.PostAsync(
            $"/api/devices/by-eui/{nonExistentEui}/ignore?revert=false",
            null,
            CancellationToken.None
        );
        var problem = await httpResponse.Content.ReadFromJsonAsync<ResultProblemDetails>(
            jsonOptions,
            CancellationToken.None
        );

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, httpResponse.StatusCode);
        Assert.NotNull(problem);
        Assert.Equal((int)HttpStatusCode.NotFound, problem.Status);
    }

    [Fact]
    public async Task Post_AcceptById_UpdatesStatusAndStateCacheWhenChirpstackSucceeds()
    {
        // Arrange
        using var appFactory = WithChirpstackService<FakeChirpstackServiceWithSuccess>();
        var device = CreateDevice(status: DeviceStatus.PENDING);
        await SeedDevicesAsync(appFactory, device);

        var jsonOptions = GetJsonOptions(appFactory);
        var stateCache = appFactory.Services.GetRequiredService<IStateCache>();
        var client = appFactory.CreateClient();

        // Act
        var httpResponse = await client.PostAsync(
            $"/api/devices/by-id/{device.DeviceId}/accept",
            null,
            CancellationToken.None
        );
        var deviceResponse = await httpResponse.Content.ReadFromJsonAsync<DeviceResponse>(
            jsonOptions,
            CancellationToken.None
        );

        // Assert
        httpResponse.EnsureSuccessStatusCode();
        Assert.NotNull(deviceResponse);
        Assert.Equal(DeviceStatus.ACTIVE, deviceResponse.DeviceStatus);
        Assert.True(stateCache.TryGetDevice(device.DevEui, out var cachedDevice));
        Assert.Equal(DeviceStatus.ACTIVE, cachedDevice.DeviceStatus);
    }

    [Fact]
    public async Task Post_AcceptById_ReturnsBadGatewayWhenChirpstackFails()
    {
        // Arrange
        using var appFactory = WithChirpstackService<FakeChirpstackServiceWithFailed>();
        var device = CreateDevice(status: DeviceStatus.PENDING);
        await SeedDevicesAsync(appFactory, device);

        var jsonOptions = GetJsonOptions(appFactory);
        var stateCache = appFactory.Services.GetRequiredService<IStateCache>();
        stateCache[device.DevEui] = device;
        var client = appFactory.CreateClient();

        // Act
        var httpResponse = await client.PostAsync(
            $"/api/devices/by-id/{device.DeviceId}/accept",
            null,
            CancellationToken.None
        );
        var problem = await httpResponse.Content.ReadFromJsonAsync<ResultProblemDetails>(
            jsonOptions,
            CancellationToken.None
        );

        // Assert
        Assert.Equal(HttpStatusCode.BadGateway, httpResponse.StatusCode);
        Assert.NotNull(problem);
        Assert.Equal((int)HttpStatusCode.BadGateway, problem.Status);
        Assert.Equal(DeviceStatus.PENDING, stateCache[device.DevEui].DeviceStatus);
    }

    [Fact]
    public async Task Post_AcceptById_ReturnsGatewayTimeoutWhenChirpstackTimesOut()
    {
        // Arrange
        using var appFactory = WithChirpstackService<FakeChirpstackServiceWithTimeout>();
        var device = CreateDevice(status: DeviceStatus.PENDING);
        await SeedDevicesAsync(appFactory, device);

        var jsonOptions = GetJsonOptions(appFactory);
        var stateCache = appFactory.Services.GetRequiredService<IStateCache>();
        stateCache[device.DevEui] = device;
        var client = appFactory.CreateClient();

        // Act
        var httpResponse = await client.PostAsync(
            $"/api/devices/by-id/{device.DeviceId}/accept",
            null,
            CancellationToken.None
        );
        var problem = await httpResponse.Content.ReadFromJsonAsync<ResultProblemDetails>(
            jsonOptions,
            CancellationToken.None
        );

        // Assert
        Assert.Equal(HttpStatusCode.GatewayTimeout, httpResponse.StatusCode);
        Assert.NotNull(problem);
        Assert.Equal((int)HttpStatusCode.GatewayTimeout, problem.Status);
        Assert.Equal(DeviceStatus.PENDING, stateCache[device.DevEui].DeviceStatus);
    }

    [Theory]
    [InlineData(DeviceStatus.ACTIVE)]
    [InlineData(DeviceStatus.IGNORED)]
    [InlineData(DeviceStatus.DISCONNECTED)]
    public async Task Post_AcceptById_ReturnsConflictWhenDeviceIsNotPending(
        DeviceStatus initialStatus
    )
    {
        // Arrange
        using var appFactory = WithChirpstackService<FakeChirpstackServiceWithSuccess>();
        var device = CreateDevice(status: initialStatus);
        await SeedDevicesAsync(appFactory, device);

        var jsonOptions = GetJsonOptions(appFactory);
        var stateCache = appFactory.Services.GetRequiredService<IStateCache>();
        stateCache[device.DevEui] = device;
        var client = appFactory.CreateClient();

        // Act
        var httpResponse = await client.PostAsync(
            $"/api/devices/by-id/{device.DeviceId}/accept",
            null,
            CancellationToken.None
        );
        var problem = await httpResponse.Content.ReadFromJsonAsync<ResultProblemDetails>(
            jsonOptions,
            CancellationToken.None
        );

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, httpResponse.StatusCode);
        Assert.NotNull(problem);
        Assert.Equal((int)HttpStatusCode.Conflict, problem.Status);
        Assert.Equal(initialStatus, stateCache[device.DevEui].DeviceStatus);
    }

    [Fact]
    public async Task Post_AcceptById_ReturnsNotFoundWhenDeviceDoesNotExist()
    {
        // Arrange
        using var appFactory = WithChirpstackService<FakeChirpstackServiceWithSuccess>();
        var jsonOptions = GetJsonOptions(appFactory);
        var client = appFactory.CreateClient();

        // Act
        var httpResponse = await client.PostAsync(
            $"/api/devices/by-id/{Guid.NewGuid()}/accept",
            null,
            CancellationToken.None
        );
        var problem = await httpResponse.Content.ReadFromJsonAsync<ResultProblemDetails>(
            jsonOptions,
            CancellationToken.None
        );

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, httpResponse.StatusCode);
        Assert.NotNull(problem);
        Assert.Equal((int)HttpStatusCode.NotFound, problem.Status);
    }

    [Fact]
    public async Task Post_AcceptByName_UpdatesStatusAndStateCacheWhenChirpstackSucceeds()
    {
        // Arrange
        using var appFactory = WithChirpstackService<FakeChirpstackServiceWithSuccess>();
        var device = CreateDevice(status: DeviceStatus.PENDING);
        await SeedDevicesAsync(appFactory, device);

        var jsonOptions = GetJsonOptions(appFactory);
        var stateCache = appFactory.Services.GetRequiredService<IStateCache>();
        var client = appFactory.CreateClient();

        // Act
        var httpResponse = await client.PostAsync(
            $"/api/devices/by-name/{device.FriendlyName}/accept",
            null,
            CancellationToken.None
        );
        var deviceResponse = await httpResponse.Content.ReadFromJsonAsync<DeviceResponse>(
            jsonOptions,
            CancellationToken.None
        );

        // Assert
        httpResponse.EnsureSuccessStatusCode();
        Assert.NotNull(deviceResponse);
        Assert.Equal(DeviceStatus.ACTIVE, deviceResponse.DeviceStatus);
        Assert.True(stateCache.TryGetDevice(device.DevEui, out var cachedDevice));
        Assert.Equal(DeviceStatus.ACTIVE, cachedDevice.DeviceStatus);
    }

    [Fact]
    public async Task Post_AcceptByName_ReturnsBadGatewayWhenChirpstackFails()
    {
        // Arrange
        using var appFactory = WithChirpstackService<FakeChirpstackServiceWithFailed>();
        var device = CreateDevice(status: DeviceStatus.PENDING);
        await SeedDevicesAsync(appFactory, device);

        var jsonOptions = GetJsonOptions(appFactory);
        var stateCache = appFactory.Services.GetRequiredService<IStateCache>();
        stateCache[device.DevEui] = device;
        var client = appFactory.CreateClient();

        // Act
        var httpResponse = await client.PostAsync(
            $"/api/devices/by-name/{device.FriendlyName}/accept",
            null,
            CancellationToken.None
        );
        var problem = await httpResponse.Content.ReadFromJsonAsync<ResultProblemDetails>(
            jsonOptions,
            CancellationToken.None
        );

        // Assert
        Assert.Equal(HttpStatusCode.BadGateway, httpResponse.StatusCode);
        Assert.NotNull(problem);
        Assert.Equal((int)HttpStatusCode.BadGateway, problem.Status);
        Assert.Equal(DeviceStatus.PENDING, stateCache[device.DevEui].DeviceStatus);
    }

    [Fact]
    public async Task Post_AcceptByName_ReturnsGatewayTimeoutWhenChirpstackTimesOut()
    {
        // Arrange
        using var appFactory = WithChirpstackService<FakeChirpstackServiceWithTimeout>();
        var device = CreateDevice(status: DeviceStatus.PENDING);
        await SeedDevicesAsync(appFactory, device);

        var jsonOptions = GetJsonOptions(appFactory);
        var stateCache = appFactory.Services.GetRequiredService<IStateCache>();
        stateCache[device.DevEui] = device;
        var client = appFactory.CreateClient();

        // Act
        var httpResponse = await client.PostAsync(
            $"/api/devices/by-name/{device.FriendlyName}/accept",
            null,
            CancellationToken.None
        );
        var problem = await httpResponse.Content.ReadFromJsonAsync<ResultProblemDetails>(
            jsonOptions,
            CancellationToken.None
        );

        // Assert
        Assert.Equal(HttpStatusCode.GatewayTimeout, httpResponse.StatusCode);
        Assert.NotNull(problem);
        Assert.Equal((int)HttpStatusCode.GatewayTimeout, problem.Status);
        Assert.Equal(DeviceStatus.PENDING, stateCache[device.DevEui].DeviceStatus);
    }

    [Theory]
    [InlineData(DeviceStatus.ACTIVE)]
    [InlineData(DeviceStatus.IGNORED)]
    [InlineData(DeviceStatus.DISCONNECTED)]
    public async Task Post_AcceptByName_ReturnsConflictWhenDeviceIsNotPending(
        DeviceStatus initialStatus
    )
    {
        // Arrange
        using var appFactory = WithChirpstackService<FakeChirpstackServiceWithSuccess>();
        var device = CreateDevice(status: initialStatus);
        await SeedDevicesAsync(appFactory, device);

        var jsonOptions = GetJsonOptions(appFactory);
        var stateCache = appFactory.Services.GetRequiredService<IStateCache>();
        stateCache[device.DevEui] = device;
        var client = appFactory.CreateClient();

        // Act
        var httpResponse = await client.PostAsync(
            $"/api/devices/by-name/{device.FriendlyName}/accept",
            null,
            CancellationToken.None
        );
        var problem = await httpResponse.Content.ReadFromJsonAsync<ResultProblemDetails>(
            jsonOptions,
            CancellationToken.None
        );

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, httpResponse.StatusCode);
        Assert.NotNull(problem);
        Assert.Equal((int)HttpStatusCode.Conflict, problem.Status);
        Assert.Equal(initialStatus, stateCache[device.DevEui].DeviceStatus);
    }

    [Fact]
    public async Task Post_AcceptByName_ReturnsNotFoundWhenDeviceDoesNotExist()
    {
        // Arrange
        using var appFactory = WithChirpstackService<FakeChirpstackServiceWithSuccess>();
        var jsonOptions = GetJsonOptions(appFactory);
        var client = appFactory.CreateClient();

        // Act
        var httpResponse = await client.PostAsync(
            "/api/devices/by-name/non_existent_device/accept",
            null,
            CancellationToken.None
        );
        var problem = await httpResponse.Content.ReadFromJsonAsync<ResultProblemDetails>(
            jsonOptions,
            CancellationToken.None
        );

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, httpResponse.StatusCode);
        Assert.NotNull(problem);
        Assert.Equal((int)HttpStatusCode.NotFound, problem.Status);
    }

    [Fact]
    public async Task Post_AcceptByEui_UpdatesStatusAndStateCacheWhenChirpstackSucceeds()
    {
        // Arrange
        using var appFactory = WithChirpstackService<FakeChirpstackServiceWithSuccess>();
        var device = CreateDevice(status: DeviceStatus.PENDING);
        await SeedDevicesAsync(appFactory, device);

        var jsonOptions = GetJsonOptions(appFactory);
        var stateCache = appFactory.Services.GetRequiredService<IStateCache>();
        var client = appFactory.CreateClient();

        // Act
        var httpResponse = await client.PostAsync(
            $"/api/devices/by-eui/{device.DevEui}/accept",
            null,
            CancellationToken.None
        );
        var deviceResponse = await httpResponse.Content.ReadFromJsonAsync<DeviceResponse>(
            jsonOptions,
            CancellationToken.None
        );

        // Assert
        httpResponse.EnsureSuccessStatusCode();
        Assert.NotNull(deviceResponse);
        Assert.Equal(DeviceStatus.ACTIVE, deviceResponse.DeviceStatus);
        Assert.True(stateCache.TryGetDevice(device.DevEui, out var cachedDevice));
        Assert.Equal(DeviceStatus.ACTIVE, cachedDevice.DeviceStatus);
    }

    [Fact]
    public async Task Post_AcceptByEui_ReturnsBadGatewayWhenChirpstackFails()
    {
        // Arrange
        using var appFactory = WithChirpstackService<FakeChirpstackServiceWithFailed>();
        var device = CreateDevice(status: DeviceStatus.PENDING);
        await SeedDevicesAsync(appFactory, device);

        var jsonOptions = GetJsonOptions(appFactory);
        var stateCache = appFactory.Services.GetRequiredService<IStateCache>();
        stateCache[device.DevEui] = device;
        var client = appFactory.CreateClient();

        // Act
        var httpResponse = await client.PostAsync(
            $"/api/devices/by-eui/{device.DevEui}/accept",
            null,
            CancellationToken.None
        );
        var problem = await httpResponse.Content.ReadFromJsonAsync<ResultProblemDetails>(
            jsonOptions,
            CancellationToken.None
        );

        // Assert
        Assert.Equal(HttpStatusCode.BadGateway, httpResponse.StatusCode);
        Assert.NotNull(problem);
        Assert.Equal((int)HttpStatusCode.BadGateway, problem.Status);
        Assert.Equal(DeviceStatus.PENDING, stateCache[device.DevEui].DeviceStatus);
    }

    [Fact]
    public async Task Post_AcceptByEui_ReturnsGatewayTimeoutWhenChirpstackTimesOut()
    {
        // Arrange
        using var appFactory = WithChirpstackService<FakeChirpstackServiceWithTimeout>();
        var device = CreateDevice(status: DeviceStatus.PENDING);
        await SeedDevicesAsync(appFactory, device);

        var jsonOptions = GetJsonOptions(appFactory);
        var stateCache = appFactory.Services.GetRequiredService<IStateCache>();
        stateCache[device.DevEui] = device;
        var client = appFactory.CreateClient();

        // Act
        var httpResponse = await client.PostAsync(
            $"/api/devices/by-eui/{device.DevEui}/accept",
            null,
            CancellationToken.None
        );
        var problem = await httpResponse.Content.ReadFromJsonAsync<ResultProblemDetails>(
            jsonOptions,
            CancellationToken.None
        );

        // Assert
        Assert.Equal(HttpStatusCode.GatewayTimeout, httpResponse.StatusCode);
        Assert.NotNull(problem);
        Assert.Equal((int)HttpStatusCode.GatewayTimeout, problem.Status);
        Assert.Equal(DeviceStatus.PENDING, stateCache[device.DevEui].DeviceStatus);
    }

    [Theory]
    [InlineData(DeviceStatus.ACTIVE)]
    [InlineData(DeviceStatus.IGNORED)]
    [InlineData(DeviceStatus.DISCONNECTED)]
    public async Task Post_AcceptByEui_ReturnsConflictWhenDeviceIsNotPending(
        DeviceStatus initialStatus
    )
    {
        // Arrange
        using var appFactory = WithChirpstackService<FakeChirpstackServiceWithSuccess>();
        var device = CreateDevice(status: initialStatus);
        await SeedDevicesAsync(appFactory, device);

        var jsonOptions = GetJsonOptions(appFactory);
        var stateCache = appFactory.Services.GetRequiredService<IStateCache>();
        stateCache[device.DevEui] = device;
        var client = appFactory.CreateClient();

        // Act
        var httpResponse = await client.PostAsync(
            $"/api/devices/by-eui/{device.DevEui}/accept",
            null,
            CancellationToken.None
        );
        var problem = await httpResponse.Content.ReadFromJsonAsync<ResultProblemDetails>(
            jsonOptions,
            CancellationToken.None
        );

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, httpResponse.StatusCode);
        Assert.NotNull(problem);
        Assert.Equal((int)HttpStatusCode.Conflict, problem.Status);
        Assert.Equal(initialStatus, stateCache[device.DevEui].DeviceStatus);
    }

    [Fact]
    public async Task Post_AcceptByEui_ReturnsNotFoundWhenDeviceDoesNotExist()
    {
        // Arrange
        using var appFactory = WithChirpstackService<FakeChirpstackServiceWithSuccess>();
        var nonExistentEui = DevEui.Parse("FFEEDDCCBBAA2211");
        var jsonOptions = GetJsonOptions(appFactory);
        var client = appFactory.CreateClient();

        // Act
        var httpResponse = await client.PostAsync(
            $"/api/devices/by-eui/{nonExistentEui}/accept",
            null,
            CancellationToken.None
        );
        var problem = await httpResponse.Content.ReadFromJsonAsync<ResultProblemDetails>(
            jsonOptions,
            CancellationToken.None
        );

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, httpResponse.StatusCode);
        Assert.NotNull(problem);
        Assert.Equal((int)HttpStatusCode.NotFound, problem.Status);
    }

    [Theory]
    [InlineData(DeviceStatus.ACTIVE, false)]
    [InlineData(DeviceStatus.ACTIVE, true)]
    [InlineData(DeviceStatus.PENDING, true)]
    [InlineData(DeviceStatus.IGNORED, true)]
    [InlineData(DeviceStatus.DISCONNECTED, true)]
    public async Task Post_DisconnectById_UpdatesStatusAndStateCacheForValidDisconnection(
        DeviceStatus initialStatus,
        bool force
    )
    {
        // Arrange
        using var appFactory = WithChirpstackService<FakeChirpstackServiceWithSuccess>();
        var device = CreateDevice(status: initialStatus);
        await SeedDevicesAsync(appFactory, device);

        var jsonOptions = GetJsonOptions(appFactory);
        var stateCache = appFactory.Services.GetRequiredService<IStateCache>();
        var client = appFactory.CreateClient();

        // Act
        var httpResponse = await client.PostAsync(
            $"/api/devices/by-id/{device.DeviceId}/disconnect?force={force}",
            null,
            CancellationToken.None
        );
        var deviceResponse = await httpResponse.Content.ReadFromJsonAsync<DeviceResponse>(
            jsonOptions,
            CancellationToken.None
        );

        // Assert
        httpResponse.EnsureSuccessStatusCode();
        Assert.NotNull(deviceResponse);
        Assert.Equal(DeviceStatus.DISCONNECTED, deviceResponse.DeviceStatus);
        Assert.True(stateCache.TryGetDevice(device.DevEui, out var cachedDevice));
        Assert.Equal(DeviceStatus.DISCONNECTED, cachedDevice.DeviceStatus);
    }

    [Fact]
    public async Task Post_DisconnectById_ReturnsBadGatewayWhenChirpstackFails()
    {
        // Arrange
        using var appFactory = WithChirpstackService<FakeChirpstackServiceWithFailed>();
        var device = CreateDevice(status: DeviceStatus.ACTIVE);
        await SeedDevicesAsync(appFactory, device);

        var jsonOptions = GetJsonOptions(appFactory);
        var stateCache = appFactory.Services.GetRequiredService<IStateCache>();
        stateCache[device.DevEui] = device;
        var client = appFactory.CreateClient();

        // Act
        var httpResponse = await client.PostAsync(
            $"/api/devices/by-id/{device.DeviceId}/disconnect?force=false",
            null,
            CancellationToken.None
        );
        var problem = await httpResponse.Content.ReadFromJsonAsync<ResultProblemDetails>(
            jsonOptions,
            CancellationToken.None
        );

        // Assert
        Assert.Equal(HttpStatusCode.BadGateway, httpResponse.StatusCode);
        Assert.NotNull(problem);
        Assert.Equal((int)HttpStatusCode.BadGateway, problem.Status);
        Assert.Equal(DeviceStatus.ACTIVE, stateCache[device.DevEui].DeviceStatus);
    }

    [Fact]
    public async Task Post_DisconnectById_ReturnsGatewayTimeoutWhenChirpstackTimesOut()
    {
        // Arrange
        using var appFactory = WithChirpstackService<FakeChirpstackServiceWithTimeout>();
        var device = CreateDevice(status: DeviceStatus.ACTIVE);
        await SeedDevicesAsync(appFactory, device);

        var jsonOptions = GetJsonOptions(appFactory);
        var stateCache = appFactory.Services.GetRequiredService<IStateCache>();
        stateCache[device.DevEui] = device;
        var client = appFactory.CreateClient();

        // Act
        var httpResponse = await client.PostAsync(
            $"/api/devices/by-id/{device.DeviceId}/disconnect?force=false",
            null,
            CancellationToken.None
        );
        var problem = await httpResponse.Content.ReadFromJsonAsync<ResultProblemDetails>(
            jsonOptions,
            CancellationToken.None
        );

        // Assert
        Assert.Equal(HttpStatusCode.GatewayTimeout, httpResponse.StatusCode);
        Assert.NotNull(problem);
        Assert.Equal((int)HttpStatusCode.GatewayTimeout, problem.Status);
        Assert.Equal(DeviceStatus.ACTIVE, stateCache[device.DevEui].DeviceStatus);
    }

    [Theory]
    [InlineData(DeviceStatus.PENDING)]
    [InlineData(DeviceStatus.IGNORED)]
    [InlineData(DeviceStatus.DISCONNECTED)]
    public async Task Post_DisconnectById_ReturnsConflictWhenNotActiveAndForceIsFalse(
        DeviceStatus initialStatus
    )
    {
        // Arrange
        using var appFactory = WithChirpstackService<FakeChirpstackServiceWithSuccess>();
        var device = CreateDevice(status: initialStatus);
        await SeedDevicesAsync(appFactory, device);

        var jsonOptions = GetJsonOptions(appFactory);
        var stateCache = appFactory.Services.GetRequiredService<IStateCache>();
        stateCache[device.DevEui] = device;
        var client = appFactory.CreateClient();

        // Act
        var httpResponse = await client.PostAsync(
            $"/api/devices/by-id/{device.DeviceId}/disconnect?force=false",
            null,
            CancellationToken.None
        );
        var problem = await httpResponse.Content.ReadFromJsonAsync<ResultProblemDetails>(
            jsonOptions,
            CancellationToken.None
        );

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, httpResponse.StatusCode);
        Assert.NotNull(problem);
        Assert.Equal((int)HttpStatusCode.Conflict, problem.Status);
        Assert.Equal(initialStatus, stateCache[device.DevEui].DeviceStatus);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Post_DisconnectById_ReturnsNotFoundWhenDeviceDoesNotExist(bool force)
    {
        // Arrange
        using var appFactory = WithChirpstackService<FakeChirpstackServiceWithSuccess>();
        var jsonOptions = GetJsonOptions(appFactory);
        var client = appFactory.CreateClient();

        // Act
        var httpResponse = await client.PostAsync(
            $"/api/devices/by-id/{Guid.NewGuid()}/disconnect?force={force}",
            null,
            CancellationToken.None
        );
        var problem = await httpResponse.Content.ReadFromJsonAsync<ResultProblemDetails>(
            jsonOptions,
            CancellationToken.None
        );

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, httpResponse.StatusCode);
        Assert.NotNull(problem);
        Assert.Equal((int)HttpStatusCode.NotFound, problem.Status);
    }

    [Theory]
    [InlineData(DeviceStatus.ACTIVE, false)]
    [InlineData(DeviceStatus.ACTIVE, true)]
    [InlineData(DeviceStatus.PENDING, true)]
    [InlineData(DeviceStatus.IGNORED, true)]
    [InlineData(DeviceStatus.DISCONNECTED, true)]
    public async Task Post_DisconnectByName_UpdatesStatusAndStateCacheForValidDisconnection(
        DeviceStatus initialStatus,
        bool force
    )
    {
        // Arrange
        using var appFactory = WithChirpstackService<FakeChirpstackServiceWithSuccess>();
        var device = CreateDevice(status: initialStatus);
        await SeedDevicesAsync(appFactory, device);

        var jsonOptions = GetJsonOptions(appFactory);
        var stateCache = appFactory.Services.GetRequiredService<IStateCache>();
        var client = appFactory.CreateClient();

        // Act
        var httpResponse = await client.PostAsync(
            $"/api/devices/by-name/{device.FriendlyName}/disconnect?force={force}",
            null,
            CancellationToken.None
        );
        var deviceResponse = await httpResponse.Content.ReadFromJsonAsync<DeviceResponse>(
            jsonOptions,
            CancellationToken.None
        );

        // Assert
        httpResponse.EnsureSuccessStatusCode();
        Assert.NotNull(deviceResponse);
        Assert.Equal(DeviceStatus.DISCONNECTED, deviceResponse.DeviceStatus);
        Assert.True(stateCache.TryGetDevice(device.DevEui, out var cachedDevice));
        Assert.Equal(DeviceStatus.DISCONNECTED, cachedDevice.DeviceStatus);
    }

    [Fact]
    public async Task Post_DisconnectByName_ReturnsBadGatewayWhenChirpstackFails()
    {
        // Arrange
        using var appFactory = WithChirpstackService<FakeChirpstackServiceWithFailed>();
        var device = CreateDevice(status: DeviceStatus.ACTIVE);
        await SeedDevicesAsync(appFactory, device);

        var jsonOptions = GetJsonOptions(appFactory);
        var stateCache = appFactory.Services.GetRequiredService<IStateCache>();
        stateCache[device.DevEui] = device;
        var client = appFactory.CreateClient();

        // Act
        var httpResponse = await client.PostAsync(
            $"/api/devices/by-name/{device.FriendlyName}/disconnect?force=false",
            null,
            CancellationToken.None
        );
        var problem = await httpResponse.Content.ReadFromJsonAsync<ResultProblemDetails>(
            jsonOptions,
            CancellationToken.None
        );

        // Assert
        Assert.Equal(HttpStatusCode.BadGateway, httpResponse.StatusCode);
        Assert.NotNull(problem);
        Assert.Equal((int)HttpStatusCode.BadGateway, problem.Status);
        Assert.Equal(DeviceStatus.ACTIVE, stateCache[device.DevEui].DeviceStatus);
    }

    [Fact]
    public async Task Post_DisconnectByName_ReturnsGatewayTimeoutWhenChirpstackTimesOut()
    {
        // Arrange
        using var appFactory = WithChirpstackService<FakeChirpstackServiceWithTimeout>();
        var device = CreateDevice(status: DeviceStatus.ACTIVE);
        await SeedDevicesAsync(appFactory, device);

        var jsonOptions = GetJsonOptions(appFactory);
        var stateCache = appFactory.Services.GetRequiredService<IStateCache>();
        stateCache[device.DevEui] = device;
        var client = appFactory.CreateClient();

        // Act
        var httpResponse = await client.PostAsync(
            $"/api/devices/by-name/{device.FriendlyName}/disconnect?force=false",
            null,
            CancellationToken.None
        );
        var problem = await httpResponse.Content.ReadFromJsonAsync<ResultProblemDetails>(
            jsonOptions,
            CancellationToken.None
        );

        // Assert
        Assert.Equal(HttpStatusCode.GatewayTimeout, httpResponse.StatusCode);
        Assert.NotNull(problem);
        Assert.Equal((int)HttpStatusCode.GatewayTimeout, problem.Status);
        Assert.Equal(DeviceStatus.ACTIVE, stateCache[device.DevEui].DeviceStatus);
    }

    [Theory]
    [InlineData(DeviceStatus.PENDING)]
    [InlineData(DeviceStatus.IGNORED)]
    [InlineData(DeviceStatus.DISCONNECTED)]
    public async Task Post_DisconnectByName_ReturnsConflictWhenNotActiveAndForceIsFalse(
        DeviceStatus initialStatus
    )
    {
        // Arrange
        using var appFactory = WithChirpstackService<FakeChirpstackServiceWithSuccess>();
        var device = CreateDevice(status: initialStatus);
        await SeedDevicesAsync(appFactory, device);

        var jsonOptions = GetJsonOptions(appFactory);
        var stateCache = appFactory.Services.GetRequiredService<IStateCache>();
        stateCache[device.DevEui] = device;
        var client = appFactory.CreateClient();

        // Act
        var httpResponse = await client.PostAsync(
            $"/api/devices/by-name/{device.FriendlyName}/disconnect?force=false",
            null,
            CancellationToken.None
        );
        var problem = await httpResponse.Content.ReadFromJsonAsync<ResultProblemDetails>(
            jsonOptions,
            CancellationToken.None
        );

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, httpResponse.StatusCode);
        Assert.NotNull(problem);
        Assert.Equal((int)HttpStatusCode.Conflict, problem.Status);
        Assert.Equal(initialStatus, stateCache[device.DevEui].DeviceStatus);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Post_DisconnectByName_ReturnsNotFoundWhenDeviceDoesNotExist(bool force)
    {
        // Arrange
        using var appFactory = WithChirpstackService<FakeChirpstackServiceWithSuccess>();
        var jsonOptions = GetJsonOptions(appFactory);
        var client = appFactory.CreateClient();

        // Act
        var httpResponse = await client.PostAsync(
            $"/api/devices/by-name/non_existent_device/disconnect?force={force}",
            null,
            CancellationToken.None
        );
        var problem = await httpResponse.Content.ReadFromJsonAsync<ResultProblemDetails>(
            jsonOptions,
            CancellationToken.None
        );

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, httpResponse.StatusCode);
        Assert.NotNull(problem);
        Assert.Equal((int)HttpStatusCode.NotFound, problem.Status);
    }

    [Theory]
    [InlineData(DeviceStatus.ACTIVE, false)]
    [InlineData(DeviceStatus.ACTIVE, true)]
    [InlineData(DeviceStatus.PENDING, true)]
    [InlineData(DeviceStatus.IGNORED, true)]
    [InlineData(DeviceStatus.DISCONNECTED, true)]
    public async Task Post_DisconnectByEui_UpdatesStatusAndStateCacheForValidDisconnection(
        DeviceStatus initialStatus,
        bool force
    )
    {
        // Arrange
        using var appFactory = WithChirpstackService<FakeChirpstackServiceWithSuccess>();
        var device = CreateDevice(status: initialStatus);
        await SeedDevicesAsync(appFactory, device);

        var jsonOptions = GetJsonOptions(appFactory);
        var stateCache = appFactory.Services.GetRequiredService<IStateCache>();
        var client = appFactory.CreateClient();

        // Act
        var httpResponse = await client.PostAsync(
            $"/api/devices/by-eui/{device.DevEui}/disconnect?force={force}",
            null,
            CancellationToken.None
        );
        var deviceResponse = await httpResponse.Content.ReadFromJsonAsync<DeviceResponse>(
            jsonOptions,
            CancellationToken.None
        );

        // Assert
        httpResponse.EnsureSuccessStatusCode();
        Assert.NotNull(deviceResponse);
        Assert.Equal(DeviceStatus.DISCONNECTED, deviceResponse.DeviceStatus);
        Assert.True(stateCache.TryGetDevice(device.DevEui, out var cachedDevice));
        Assert.Equal(DeviceStatus.DISCONNECTED, cachedDevice.DeviceStatus);
    }

    [Fact]
    public async Task Post_DisconnectByEui_ReturnsEmptyDeviceResponseWhenDeviceDoesNotExistAndForceIsTrue()
    {
        // Arrange
        using var appFactory = WithChirpstackService<FakeChirpstackServiceWithSuccess>();
        var unknownEui = DevEui.Parse("FFEEDDCCBBAA2211");
        var expectedResponse = DeviceResponse.FromModel(new Device());
        var jsonOptions = GetJsonOptions(appFactory);
        var client = appFactory.CreateClient();

        // Act
        var httpResponse = await client.PostAsync(
            $"/api/devices/by-eui/{unknownEui}/disconnect?force=true",
            null,
            CancellationToken.None
        );
        var deviceResponse = await httpResponse.Content.ReadFromJsonAsync<DeviceResponse>(
            jsonOptions,
            CancellationToken.None
        );

        // Assert
        httpResponse.EnsureSuccessStatusCode();
        Assert.NotNull(deviceResponse);
        Assert.EquivalentWithExclusions(expectedResponse, deviceResponse, [d => d.LastSeen]);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task Post_DisconnectByEui_ReturnsBadGatewayWhenChirpstackFails(
        bool deviceExists,
        bool force
    )
    {
        // Arrange
        using var appFactory = WithChirpstackService<FakeChirpstackServiceWithFailed>();
        var devEui = DevEui.Parse("1122AABBCCDDEEFF");
        var stateCache = appFactory.Services.GetRequiredService<IStateCache>();
        if (deviceExists)
        {
            var device = CreateDevice(status: DeviceStatus.ACTIVE, devEui: "1122AABBCCDDEEFF");
            await SeedDevicesAsync(appFactory, device);
            stateCache[devEui] = device;
        }

        var jsonOptions = GetJsonOptions(appFactory);
        var client = appFactory.CreateClient();

        // Act
        var httpResponse = await client.PostAsync(
            $"/api/devices/by-eui/{devEui}/disconnect?force={force}",
            null,
            CancellationToken.None
        );
        var problem = await httpResponse.Content.ReadFromJsonAsync<ResultProblemDetails>(
            jsonOptions,
            CancellationToken.None
        );

        // Assert
        Assert.Equal(HttpStatusCode.BadGateway, httpResponse.StatusCode);
        Assert.NotNull(problem);
        Assert.Equal((int)HttpStatusCode.BadGateway, problem.Status);
        if (deviceExists)
        {
            Assert.Equal(DeviceStatus.ACTIVE, stateCache[devEui].DeviceStatus);
        }
        else
        {
            Assert.False(stateCache.TryGetDevice(devEui, out _));
        }
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task Post_DisconnectByEui_ReturnsGatewayTimeoutWhenChirpstackTimesOut(
        bool deviceExists,
        bool force
    )
    {
        // Arrange
        using var appFactory = WithChirpstackService<FakeChirpstackServiceWithTimeout>();
        var devEui = DevEui.Parse("1122AABBCCDDEEFF");
        var stateCache = appFactory.Services.GetRequiredService<IStateCache>();
        if (deviceExists)
        {
            var device = CreateDevice(status: DeviceStatus.ACTIVE, devEui: "1122AABBCCDDEEFF");
            await SeedDevicesAsync(appFactory, device);
            stateCache[devEui] = device;
        }

        var jsonOptions = GetJsonOptions(appFactory);
        var client = appFactory.CreateClient();

        // Act
        var httpResponse = await client.PostAsync(
            $"/api/devices/by-eui/{devEui}/disconnect?force={force}",
            null,
            CancellationToken.None
        );
        var problem = await httpResponse.Content.ReadFromJsonAsync<ResultProblemDetails>(
            jsonOptions,
            CancellationToken.None
        );

        // Assert
        Assert.Equal(HttpStatusCode.GatewayTimeout, httpResponse.StatusCode);
        Assert.NotNull(problem);
        Assert.Equal((int)HttpStatusCode.GatewayTimeout, problem.Status);
        if (deviceExists)
        {
            Assert.Equal(DeviceStatus.ACTIVE, stateCache[devEui].DeviceStatus);
        }
        else
        {
            Assert.False(stateCache.TryGetDevice(devEui, out _));
        }
    }

    [Theory]
    [InlineData(DeviceStatus.PENDING)]
    [InlineData(DeviceStatus.IGNORED)]
    [InlineData(DeviceStatus.DISCONNECTED)]
    public async Task Post_DisconnectByEui_ReturnsConflictWhenNotActiveAndForceIsFalse(
        DeviceStatus initialStatus
    )
    {
        // Arrange
        using var appFactory = WithChirpstackService<FakeChirpstackServiceWithSuccess>();
        var device = CreateDevice(status: initialStatus);
        await SeedDevicesAsync(appFactory, device);

        var jsonOptions = GetJsonOptions(appFactory);
        var stateCache = appFactory.Services.GetRequiredService<IStateCache>();
        stateCache[device.DevEui] = device;
        var client = appFactory.CreateClient();

        // Act
        var httpResponse = await client.PostAsync(
            $"/api/devices/by-eui/{device.DevEui}/disconnect?force=false",
            null,
            CancellationToken.None
        );
        var problem = await httpResponse.Content.ReadFromJsonAsync<ResultProblemDetails>(
            jsonOptions,
            CancellationToken.None
        );

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, httpResponse.StatusCode);
        Assert.NotNull(problem);
        Assert.Equal((int)HttpStatusCode.Conflict, problem.Status);
        Assert.Equal(initialStatus, stateCache[device.DevEui].DeviceStatus);
    }

    [Fact]
    public async Task Post_DisconnectByEui_ReturnsNotFoundWhenDeviceDoesNotExistAndForceIsFalse()
    {
        // Arrange
        using var appFactory = WithChirpstackService<FakeChirpstackServiceWithSuccess>();
        var nonExistentEui = DevEui.Parse("FFEEDDCCBBAA2211");
        var jsonOptions = GetJsonOptions(appFactory);
        var client = appFactory.CreateClient();

        // Act
        var httpResponse = await client.PostAsync(
            $"/api/devices/by-eui/{nonExistentEui}/disconnect?force=false",
            null,
            CancellationToken.None
        );
        var problem = await httpResponse.Content.ReadFromJsonAsync<ResultProblemDetails>(
            jsonOptions,
            CancellationToken.None
        );

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, httpResponse.StatusCode);
        Assert.NotNull(problem);
        Assert.Equal((int)HttpStatusCode.NotFound, problem.Status);
    }

    [Theory]
    [InlineData(DeviceStatus.ACTIVE)]
    [InlineData(DeviceStatus.DISCONNECTED)]
    [InlineData(DeviceStatus.IGNORED)]
    [InlineData(DeviceStatus.PENDING)]
    public async Task Post_ReplaceById_ReplacesDeviceWipesSubstituteAndUpdatesStateCache(
        DeviceStatus originalStatus
    )
    {
        // Arrange
        using var appFactory = WithChirpstackService<FakeChirpstackServiceWithSuccess>();
        var originalDevEui = DevEui.Parse("1122AABBCCDDEEFF");
        var substituteDevEui = DevEui.Parse("1122AABBCCDDEEF0");
        var device1 = CreateDevice(
            status: originalStatus,
            devEui: "1122AABBCCDDEEFF",
            friendlyName: "loris_friendly_1",
            deviceName: "loris_sample_1",
            withCapabilities: true
        );
        var device2 = CreateDevice(
            status: DeviceStatus.PENDING,
            devEui: "1122AABBCCDDEEF0",
            friendlyName: "loris_friendly_2",
            deviceName: "loris_sample_2",
            withCapabilities: true
        );
        await SeedDevicesAsync(appFactory, device1, device2);

        using (var seedScope = appFactory.Services.CreateScope())
        {
            var seedCtx = seedScope.ServiceProvider.GetRequiredService<LorisDbContext>();
            var subSensorId = device2.Sensors.First().SensorId;
            seedCtx.SensorReadings.Add(
                new SensorReading
                {
                    SensorId = subSensorId,
                    Time = DateTime.UtcNow,
                    FloatValue = 21.5f,
                }
            );
            await seedCtx.SaveChangesAsync(CancellationToken.None);
        }

        var jsonOptions = GetJsonOptions(appFactory);
        var stateCache = appFactory.Services.GetRequiredService<IStateCache>();
        stateCache[originalDevEui] = device1;
        var client = appFactory.CreateClient();
        var token = TestContext.Current.CancellationToken;

        var replaceRequest = new DeviceReplaceRequest { With = device2.DeviceId };
        var expectedAfter = CreateDevice(
            status: DeviceStatus.ACTIVE,
            devEui: "1122AABBCCDDEEF0",
            friendlyName: "loris_friendly_1",
            deviceName: "loris_sample_1"
        );
        expectedAfter.DeviceId = device1.DeviceId;

        // Act
        var replaceResponse = await client.PostAsJsonAsync(
            $"/api/devices/by-id/{device1.DeviceId}/replace",
            replaceRequest,
            jsonOptions,
            token
        );
        var replaceDeviceResponse = await replaceResponse.Content.ReadFromJsonAsync<DeviceResponse>(
            jsonOptions,
            token
        );

        var get1Response = await client.GetAsync($"/api/devices/by-id/{device1.DeviceId}", token);
        var get1DetailsResponse =
            await get1Response.Content.ReadFromJsonAsync<DeviceDetailsResponse>(jsonOptions, token);
        var get2Response = await client.GetAsync($"/api/devices/by-id/{device2.DeviceId}", token);

        // Assert
        replaceResponse.EnsureSuccessStatusCode();
        get1Response.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.NotFound, get2Response.StatusCode);
        Assert.NotNull(replaceDeviceResponse);
        Assert.EquivalentWithExclusions(
            DeviceResponse.FromModel(expectedAfter),
            replaceDeviceResponse,
            [d => d.LastSeen]
        );
        Assert.NotNull(get1DetailsResponse);
        Assert.Equal(substituteDevEui, get1DetailsResponse.DevEui);
        Assert.Equal(DeviceStatus.ACTIVE, get1DetailsResponse.DeviceStatus);
        Assert.Single(get1DetailsResponse.Sensors);
        Assert.Single(get1DetailsResponse.Functions);

        Assert.True(stateCache.TryGetDevice(substituteDevEui, out var cachedDevice));
        Assert.Equal(device1.DeviceId, cachedDevice.DeviceId);
        Assert.Equal(DeviceStatus.ACTIVE, cachedDevice.DeviceStatus);
        Assert.False(stateCache.TryGetDevice(originalDevEui, out _));

        using var verifyScope = appFactory.Services.CreateScope();
        var verifyCtx = verifyScope.ServiceProvider.GetRequiredService<LorisDbContext>();
        Assert.False(verifyCtx.Devices.Any(d => d.DeviceId == device2.DeviceId));
        Assert.False(verifyCtx.Sensors.Any(s => s.DeviceId == device2.DeviceId));
        Assert.False(verifyCtx.Functions.Any(f => f.DeviceId == device2.DeviceId));
        Assert.Empty(verifyCtx.SensorReadings);
    }

    [Theory]
    [InlineData("guid")]
    [InlineData("eui")]
    [InlineData("name")]
    public async Task Post_ReplaceById_SucceedsForAllSubstituteWithKeyVariations(string keyType)
    {
        // Arrange
        using var appFactory = WithChirpstackService<FakeChirpstackServiceWithSuccess>();
        var device1 = CreateDevice(
            status: DeviceStatus.ACTIVE,
            devEui: "1122AABBCCDDEEFF",
            friendlyName: "loris_friendly_1",
            deviceName: "loris_sample_1",
            withCapabilities: true
        );
        var device2 = CreateDevice(
            status: DeviceStatus.PENDING,
            devEui: "1122AABBCCDDEEF0",
            friendlyName: "loris_friendly_2",
            deviceName: "loris_sample_2",
            withCapabilities: true
        );
        await SeedDevicesAsync(appFactory, device1, device2);

        object withKey = keyType switch
        {
            "guid" => device2.DeviceId,
            "eui" => device2.DevEui,
            "name" => device2.FriendlyName,
            _ => throw new ArgumentOutOfRangeException(nameof(keyType)),
        };

        var jsonOptions = GetJsonOptions(appFactory);
        var stateCache = appFactory.Services.GetRequiredService<IStateCache>();
        var client = appFactory.CreateClient();
        var token = TestContext.Current.CancellationToken;

        var replaceRequest = new DeviceReplaceRequest { With = withKey };

        // Act
        var httpResponse = await client.PostAsJsonAsync(
            $"/api/devices/by-id/{device1.DeviceId}/replace",
            replaceRequest,
            jsonOptions,
            token
        );
        var deviceResponse = await httpResponse.Content.ReadFromJsonAsync<DeviceResponse>(
            jsonOptions,
            token
        );

        // Assert
        httpResponse.EnsureSuccessStatusCode();
        Assert.NotNull(deviceResponse);
        Assert.Equal(device2.DevEui, deviceResponse.DevEui);
        Assert.Equal(DeviceStatus.ACTIVE, deviceResponse.DeviceStatus);
        Assert.True(stateCache.TryGetDevice(device2.DevEui, out var cachedDevice));
        Assert.Equal(device1.DeviceId, cachedDevice.DeviceId);
    }

    [Theory]
    [InlineData(DeviceStatus.ACTIVE)]
    [InlineData(DeviceStatus.IGNORED)]
    [InlineData(DeviceStatus.DISCONNECTED)]
    public async Task Post_ReplaceById_ReturnsConflictWhenSubstituteIsNotPending(
        DeviceStatus substituteStatus
    )
    {
        // Arrange
        using var appFactory = WithChirpstackService<FakeChirpstackServiceWithSuccess>();
        var device1 = CreateDevice(
            status: DeviceStatus.ACTIVE,
            devEui: "1122AABBCCDDEEFF",
            friendlyName: "loris_friendly_1",
            deviceName: "loris_sample_1",
            withCapabilities: true
        );
        var device2 = CreateDevice(
            status: substituteStatus,
            devEui: "1122AABBCCDDEEF0",
            friendlyName: "loris_friendly_2",
            deviceName: "loris_sample_2",
            withCapabilities: true
        );
        await SeedDevicesAsync(appFactory, device1, device2);

        var jsonOptions = GetJsonOptions(appFactory);
        var stateCache = appFactory.Services.GetRequiredService<IStateCache>();
        stateCache[device1.DevEui] = device1;
        var client = appFactory.CreateClient();
        var token = TestContext.Current.CancellationToken;

        var replaceRequest = new DeviceReplaceRequest { With = device2.DeviceId };

        // Act
        var httpResponse = await client.PostAsJsonAsync(
            $"/api/devices/by-id/{device1.DeviceId}/replace",
            replaceRequest,
            jsonOptions,
            token
        );
        var problem = await httpResponse.Content.ReadFromJsonAsync<ResultProblemDetails>(
            jsonOptions,
            token
        );

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, httpResponse.StatusCode);
        Assert.NotNull(problem);
        Assert.Equal((int)HttpStatusCode.Conflict, problem.Status);
        Assert.True(stateCache.TryGetDevice(device1.DevEui, out _));
        Assert.False(stateCache.TryGetDevice(device2.DevEui, out _));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Post_ReplaceById_ReturnsErrorWhenSubstituteIsSameOrCapabilitiesMismatch(
        bool sameDevice
    )
    {
        // Arrange
        using var appFactory = WithChirpstackService<FakeChirpstackServiceWithSuccess>();
        var device1 = CreateDevice(
            status: DeviceStatus.PENDING,
            devEui: "1122AABBCCDDEEFF",
            friendlyName: "loris_friendly_1",
            deviceName: "loris_sample_1",
            withCapabilities: true
        );
        var device2 = CreateDevice(
            status: DeviceStatus.PENDING,
            devEui: "1122AABBCCDDEEF0",
            friendlyName: "loris_friendly_2",
            deviceName: "loris_sample_2",
            withCapabilities: false
        );
        await SeedDevicesAsync(appFactory, device1, device2);

        var jsonOptions = GetJsonOptions(appFactory);
        var client = appFactory.CreateClient();
        var token = TestContext.Current.CancellationToken;

        var replaceRequest = new DeviceReplaceRequest
        {
            With = sameDevice ? device1.DeviceId : device2.DeviceId,
        };

        // Act
        var httpResponse = await client.PostAsJsonAsync(
            $"/api/devices/by-id/{device1.DeviceId}/replace",
            replaceRequest,
            jsonOptions,
            token
        );
        var problem = await httpResponse.Content.ReadFromJsonAsync<ResultProblemDetails>(
            jsonOptions,
            token
        );

        // Assert
        Assert.True(
            httpResponse.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Conflict,
            $"Expected 400 or 409, got {httpResponse.StatusCode}"
        );
        Assert.NotNull(problem);
    }

    [Theory]
    [InlineData("originalNotFound")]
    [InlineData("substituteNotFound")]
    [InlineData("invalidKey")]
    public async Task Post_ReplaceById_ReturnsNotFoundOrBadRequestForMissingOrInvalidTargets(
        string scenario
    )
    {
        // Arrange
        using var appFactory = WithChirpstackService<FakeChirpstackServiceWithSuccess>();
        var device1 = CreateDevice(
            status: DeviceStatus.ACTIVE,
            devEui: "1122AABBCCDDEEFF",
            friendlyName: "loris_friendly_1",
            deviceName: "loris_sample_1",
            withCapabilities: true
        );
        await SeedDevicesAsync(appFactory, device1);

        var targetId = scenario == "originalNotFound" ? Guid.NewGuid() : device1.DeviceId;
        object withKey = scenario switch
        {
            "originalNotFound" => device1.DeviceId,
            "substituteNotFound" => Guid.NewGuid(),
            "invalidKey" => 12345,
            _ => throw new ArgumentOutOfRangeException(nameof(scenario)),
        };
        var expectedStatus =
            scenario == "originalNotFound" ? HttpStatusCode.NotFound : HttpStatusCode.BadRequest;

        var jsonOptions = GetJsonOptions(appFactory);
        var client = appFactory.CreateClient();
        var token = TestContext.Current.CancellationToken;

        var replaceRequest = new DeviceReplaceRequest { With = withKey };

        // Act
        var httpResponse = await client.PostAsJsonAsync(
            $"/api/devices/by-id/{targetId}/replace",
            replaceRequest,
            jsonOptions,
            token
        );
        var problem = await httpResponse.Content.ReadFromJsonAsync<ResultProblemDetails>(
            jsonOptions,
            token
        );

        // Assert
        Assert.Equal(expectedStatus, httpResponse.StatusCode);
        Assert.NotNull(problem);
        Assert.Equal((int)expectedStatus, problem.Status);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Post_ReplaceById_ReturnsGatewayErrorWhenChirpstackIsUnavailable(
        bool isTimeout
    )
    {
        // Arrange
        using var appFactory = isTimeout
            ? WithChirpstackService<FakeChirpstackServiceWithTimeout>()
            : WithChirpstackService<FakeChirpstackServiceWithFailed>();
        var device1 = CreateDevice(
            status: DeviceStatus.ACTIVE,
            devEui: "1122AABBCCDDEEFF",
            friendlyName: "loris_friendly_1",
            deviceName: "loris_sample_1",
            withCapabilities: true
        );
        var device2 = CreateDevice(
            status: DeviceStatus.PENDING,
            devEui: "1122AABBCCDDEEF0",
            friendlyName: "loris_friendly_2",
            deviceName: "loris_sample_2",
            withCapabilities: true
        );
        await SeedDevicesAsync(appFactory, device1, device2);

        var expectedStatus = isTimeout ? HttpStatusCode.GatewayTimeout : HttpStatusCode.BadGateway;
        var jsonOptions = GetJsonOptions(appFactory);
        var stateCache = appFactory.Services.GetRequiredService<IStateCache>();
        stateCache[device1.DevEui] = device1;
        var client = appFactory.CreateClient();
        var token = TestContext.Current.CancellationToken;

        var replaceRequest = new DeviceReplaceRequest { With = device2.DeviceId };

        // Act
        var httpResponse = await client.PostAsJsonAsync(
            $"/api/devices/by-id/{device1.DeviceId}/replace",
            replaceRequest,
            jsonOptions,
            token
        );
        var problem = await httpResponse.Content.ReadFromJsonAsync<ResultProblemDetails>(
            jsonOptions,
            token
        );

        // Assert
        Assert.Equal(expectedStatus, httpResponse.StatusCode);
        Assert.NotNull(problem);
        Assert.Equal((int)expectedStatus, problem.Status);
        Assert.True(stateCache.TryGetDevice(device1.DevEui, out var cachedOriginal));
        Assert.Equal(device1.DevEui, cachedOriginal.DevEui);
        Assert.False(stateCache.TryGetDevice(device2.DevEui, out _));
    }

    [Theory]
    [InlineData("guid")]
    [InlineData("eui")]
    [InlineData("name")]
    public async Task Post_ReplaceByName_ReplacesDeviceAndUpdatesStateCacheForAllWithKeyVariations(
        string keyType
    )
    {
        // Arrange
        using var appFactory = WithChirpstackService<FakeChirpstackServiceWithSuccess>();
        var device1 = CreateDevice(
            status: DeviceStatus.ACTIVE,
            devEui: "1122AABBCCDDEEFF",
            friendlyName: "loris_friendly_1",
            deviceName: "loris_sample_1",
            withCapabilities: true
        );
        var device2 = CreateDevice(
            status: DeviceStatus.PENDING,
            devEui: "1122AABBCCDDEEF0",
            friendlyName: "loris_friendly_2",
            deviceName: "loris_sample_2",
            withCapabilities: true
        );
        await SeedDevicesAsync(appFactory, device1, device2);

        object withKey = keyType switch
        {
            "guid" => device2.DeviceId,
            "eui" => device2.DevEui,
            "name" => device2.FriendlyName,
            _ => throw new ArgumentOutOfRangeException(nameof(keyType)),
        };

        var jsonOptions = GetJsonOptions(appFactory);
        var stateCache = appFactory.Services.GetRequiredService<IStateCache>();
        stateCache[device1.DevEui] = device1;
        var client = appFactory.CreateClient();
        var token = TestContext.Current.CancellationToken;

        var replaceRequest = new DeviceReplaceRequest { With = withKey };

        // Act
        var httpResponse = await client.PostAsJsonAsync(
            $"/api/devices/by-name/{device1.FriendlyName}/replace",
            replaceRequest,
            jsonOptions,
            token
        );
        var deviceResponse = await httpResponse.Content.ReadFromJsonAsync<DeviceResponse>(
            jsonOptions,
            token
        );

        // Assert
        httpResponse.EnsureSuccessStatusCode();
        Assert.NotNull(deviceResponse);
        Assert.Equal(device2.DevEui, deviceResponse.DevEui);
        Assert.Equal(DeviceStatus.ACTIVE, deviceResponse.DeviceStatus);
        Assert.True(stateCache.TryGetDevice(device2.DevEui, out var cachedDevice));
        Assert.Equal(device1.DeviceId, cachedDevice.DeviceId);
        Assert.False(stateCache.TryGetDevice(device1.DevEui, out _));
    }

    [Theory]
    [InlineData(DeviceStatus.ACTIVE)]
    [InlineData(DeviceStatus.IGNORED)]
    [InlineData(DeviceStatus.DISCONNECTED)]
    public async Task Post_ReplaceByName_ReturnsConflictWhenSubstituteIsNotPending(
        DeviceStatus substituteStatus
    )
    {
        // Arrange
        using var appFactory = WithChirpstackService<FakeChirpstackServiceWithSuccess>();
        var device1 = CreateDevice(
            status: DeviceStatus.ACTIVE,
            devEui: "1122AABBCCDDEEFF",
            friendlyName: "loris_friendly_1",
            deviceName: "loris_sample_1",
            withCapabilities: true
        );
        var device2 = CreateDevice(
            status: substituteStatus,
            devEui: "1122AABBCCDDEEF0",
            friendlyName: "loris_friendly_2",
            deviceName: "loris_sample_2",
            withCapabilities: true
        );
        await SeedDevicesAsync(appFactory, device1, device2);

        var jsonOptions = GetJsonOptions(appFactory);
        var stateCache = appFactory.Services.GetRequiredService<IStateCache>();
        stateCache[device1.DevEui] = device1;
        var client = appFactory.CreateClient();
        var token = TestContext.Current.CancellationToken;

        var replaceRequest = new DeviceReplaceRequest { With = device2.FriendlyName };

        // Act
        var httpResponse = await client.PostAsJsonAsync(
            $"/api/devices/by-name/{device1.FriendlyName}/replace",
            replaceRequest,
            jsonOptions,
            token
        );
        var problem = await httpResponse.Content.ReadFromJsonAsync<ResultProblemDetails>(
            jsonOptions,
            token
        );

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, httpResponse.StatusCode);
        Assert.NotNull(problem);
        Assert.Equal((int)HttpStatusCode.Conflict, problem.Status);
        Assert.True(stateCache.TryGetDevice(device1.DevEui, out _));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Post_ReplaceByName_ReturnsGatewayErrorWhenChirpstackIsUnavailable(
        bool isTimeout
    )
    {
        // Arrange
        using var appFactory = isTimeout
            ? WithChirpstackService<FakeChirpstackServiceWithTimeout>()
            : WithChirpstackService<FakeChirpstackServiceWithFailed>();
        var device1 = CreateDevice(
            status: DeviceStatus.ACTIVE,
            devEui: "1122AABBCCDDEEFF",
            friendlyName: "loris_friendly_1",
            deviceName: "loris_sample_1",
            withCapabilities: true
        );
        var device2 = CreateDevice(
            status: DeviceStatus.PENDING,
            devEui: "1122AABBCCDDEEF0",
            friendlyName: "loris_friendly_2",
            deviceName: "loris_sample_2",
            withCapabilities: true
        );
        await SeedDevicesAsync(appFactory, device1, device2);

        var expectedStatus = isTimeout ? HttpStatusCode.GatewayTimeout : HttpStatusCode.BadGateway;
        var jsonOptions = GetJsonOptions(appFactory);
        var stateCache = appFactory.Services.GetRequiredService<IStateCache>();
        stateCache[device1.DevEui] = device1;
        var client = appFactory.CreateClient();
        var token = TestContext.Current.CancellationToken;

        var replaceRequest = new DeviceReplaceRequest { With = device2.FriendlyName };

        // Act
        var httpResponse = await client.PostAsJsonAsync(
            $"/api/devices/by-name/{device1.FriendlyName}/replace",
            replaceRequest,
            jsonOptions,
            token
        );
        var problem = await httpResponse.Content.ReadFromJsonAsync<ResultProblemDetails>(
            jsonOptions,
            token
        );

        // Assert
        Assert.Equal(expectedStatus, httpResponse.StatusCode);
        Assert.NotNull(problem);
        Assert.Equal((int)expectedStatus, problem.Status);
        Assert.True(stateCache.TryGetDevice(device1.DevEui, out _));
        Assert.False(stateCache.TryGetDevice(device2.DevEui, out _));
    }

    [Fact]
    public async Task Post_ReplaceByName_ReturnsNotFoundWhenDeviceDoesNotExist()
    {
        // Arrange
        using var appFactory = WithChirpstackService<FakeChirpstackServiceWithSuccess>();
        var jsonOptions = GetJsonOptions(appFactory);
        var client = appFactory.CreateClient();
        var token = TestContext.Current.CancellationToken;

        var replaceRequest = new DeviceReplaceRequest { With = Guid.NewGuid() };

        // Act
        var httpResponse = await client.PostAsJsonAsync(
            "/api/devices/by-name/non_existent_device/replace",
            replaceRequest,
            jsonOptions,
            token
        );
        var problem = await httpResponse.Content.ReadFromJsonAsync<ResultProblemDetails>(
            jsonOptions,
            token
        );

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, httpResponse.StatusCode);
        Assert.NotNull(problem);
        Assert.Equal((int)HttpStatusCode.NotFound, problem.Status);
    }

    [Theory]
    [InlineData("guid")]
    [InlineData("eui")]
    [InlineData("name")]
    public async Task Post_ReplaceByEui_ReplacesDeviceAndUpdatesStateCacheForAllWithKeyVariations(
        string keyType
    )
    {
        // Arrange
        using var appFactory = WithChirpstackService<FakeChirpstackServiceWithSuccess>();
        var device1 = CreateDevice(
            status: DeviceStatus.ACTIVE,
            devEui: "1122AABBCCDDEEFF",
            friendlyName: "loris_friendly_1",
            deviceName: "loris_sample_1",
            withCapabilities: true
        );
        var device2 = CreateDevice(
            status: DeviceStatus.PENDING,
            devEui: "1122AABBCCDDEEF0",
            friendlyName: "loris_friendly_2",
            deviceName: "loris_sample_2",
            withCapabilities: true
        );
        await SeedDevicesAsync(appFactory, device1, device2);

        object withKey = keyType switch
        {
            "guid" => device2.DeviceId,
            "eui" => device2.DevEui,
            "name" => device2.FriendlyName,
            _ => throw new ArgumentOutOfRangeException(nameof(keyType)),
        };

        var jsonOptions = GetJsonOptions(appFactory);
        var stateCache = appFactory.Services.GetRequiredService<IStateCache>();
        stateCache[device1.DevEui] = device1;
        var client = appFactory.CreateClient();
        var token = TestContext.Current.CancellationToken;

        var replaceRequest = new DeviceReplaceRequest { With = withKey };

        // Act
        var httpResponse = await client.PostAsJsonAsync(
            $"/api/devices/by-eui/{device1.DevEui}/replace",
            replaceRequest,
            jsonOptions,
            token
        );
        var deviceResponse = await httpResponse.Content.ReadFromJsonAsync<DeviceResponse>(
            jsonOptions,
            token
        );

        // Assert
        httpResponse.EnsureSuccessStatusCode();
        Assert.NotNull(deviceResponse);
        Assert.Equal(device2.DevEui, deviceResponse.DevEui);
        Assert.Equal(DeviceStatus.ACTIVE, deviceResponse.DeviceStatus);
        Assert.True(stateCache.TryGetDevice(device2.DevEui, out var cachedDevice));
        Assert.Equal(device1.DeviceId, cachedDevice.DeviceId);
        Assert.False(stateCache.TryGetDevice(device1.DevEui, out _));
    }

    [Theory]
    [InlineData(DeviceStatus.ACTIVE)]
    [InlineData(DeviceStatus.IGNORED)]
    [InlineData(DeviceStatus.DISCONNECTED)]
    public async Task Post_ReplaceByEui_ReturnsConflictWhenSubstituteIsNotPending(
        DeviceStatus substituteStatus
    )
    {
        // Arrange
        using var appFactory = WithChirpstackService<FakeChirpstackServiceWithSuccess>();
        var device1 = CreateDevice(
            status: DeviceStatus.ACTIVE,
            devEui: "1122AABBCCDDEEFF",
            friendlyName: "loris_friendly_1",
            deviceName: "loris_sample_1",
            withCapabilities: true
        );
        var device2 = CreateDevice(
            status: substituteStatus,
            devEui: "1122AABBCCDDEEF0",
            friendlyName: "loris_friendly_2",
            deviceName: "loris_sample_2",
            withCapabilities: true
        );
        await SeedDevicesAsync(appFactory, device1, device2);

        var jsonOptions = GetJsonOptions(appFactory);
        var stateCache = appFactory.Services.GetRequiredService<IStateCache>();
        stateCache[device1.DevEui] = device1;
        var client = appFactory.CreateClient();
        var token = TestContext.Current.CancellationToken;

        var replaceRequest = new DeviceReplaceRequest { With = device2.DevEui };

        // Act
        var httpResponse = await client.PostAsJsonAsync(
            $"/api/devices/by-eui/{device1.DevEui}/replace",
            replaceRequest,
            jsonOptions,
            token
        );
        var problem = await httpResponse.Content.ReadFromJsonAsync<ResultProblemDetails>(
            jsonOptions,
            token
        );

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, httpResponse.StatusCode);
        Assert.NotNull(problem);
        Assert.Equal((int)HttpStatusCode.Conflict, problem.Status);
        Assert.True(stateCache.TryGetDevice(device1.DevEui, out _));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Post_ReplaceByEui_ReturnsGatewayErrorWhenChirpstackIsUnavailable(
        bool isTimeout
    )
    {
        // Arrange
        using var appFactory = isTimeout
            ? WithChirpstackService<FakeChirpstackServiceWithTimeout>()
            : WithChirpstackService<FakeChirpstackServiceWithFailed>();
        var device1 = CreateDevice(
            status: DeviceStatus.ACTIVE,
            devEui: "1122AABBCCDDEEFF",
            friendlyName: "loris_friendly_1",
            deviceName: "loris_sample_1",
            withCapabilities: true
        );
        var device2 = CreateDevice(
            status: DeviceStatus.PENDING,
            devEui: "1122AABBCCDDEEF0",
            friendlyName: "loris_friendly_2",
            deviceName: "loris_sample_2",
            withCapabilities: true
        );
        await SeedDevicesAsync(appFactory, device1, device2);

        var expectedStatus = isTimeout ? HttpStatusCode.GatewayTimeout : HttpStatusCode.BadGateway;
        var jsonOptions = GetJsonOptions(appFactory);
        var stateCache = appFactory.Services.GetRequiredService<IStateCache>();
        stateCache[device1.DevEui] = device1;
        var client = appFactory.CreateClient();
        var token = TestContext.Current.CancellationToken;

        var replaceRequest = new DeviceReplaceRequest { With = device2.DevEui };

        // Act
        var httpResponse = await client.PostAsJsonAsync(
            $"/api/devices/by-eui/{device1.DevEui}/replace",
            replaceRequest,
            jsonOptions,
            token
        );
        var problem = await httpResponse.Content.ReadFromJsonAsync<ResultProblemDetails>(
            jsonOptions,
            token
        );

        // Assert
        Assert.Equal(expectedStatus, httpResponse.StatusCode);
        Assert.NotNull(problem);
        Assert.Equal((int)expectedStatus, problem.Status);
        Assert.True(stateCache.TryGetDevice(device1.DevEui, out _));
        Assert.False(stateCache.TryGetDevice(device2.DevEui, out _));
    }

    [Fact]
    public async Task Post_ReplaceByEui_ReturnsNotFoundWhenDeviceDoesNotExist()
    {
        // Arrange
        using var appFactory = WithChirpstackService<FakeChirpstackServiceWithSuccess>();
        var nonExistentEui = DevEui.Parse("FFEEDDCCBBAA2211");
        var jsonOptions = GetJsonOptions(appFactory);
        var client = appFactory.CreateClient();
        var token = TestContext.Current.CancellationToken;

        var replaceRequest = new DeviceReplaceRequest { With = Guid.NewGuid() };

        // Act
        var httpResponse = await client.PostAsJsonAsync(
            $"/api/devices/by-eui/{nonExistentEui}/replace",
            replaceRequest,
            jsonOptions,
            token
        );
        var problem = await httpResponse.Content.ReadFromJsonAsync<ResultProblemDetails>(
            jsonOptions,
            token
        );

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, httpResponse.StatusCode);
        Assert.NotNull(problem);
        Assert.Equal((int)HttpStatusCode.NotFound, problem.Status);
    }
}
