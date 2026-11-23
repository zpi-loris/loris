using System.Net;
using Loris.Data;
using Loris.Dtos.Device;
using loris.IntegrationTests.Fakes;
using Loris.Models;
using Loris.Protocol;
using Loris.Results;
using loris.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

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

    [Fact]
    public async Task Get_GetDevices_ReturnsDeviceDetailsResponseListWhenMoreThanOneExists()
    {
        // Arrange
        var device = new Device
        {
            DeviceName = "loris_sample",
            DevEui = DevEui.Parse("1122AABBCCDDEEFF"),
            FriendlyName = "loris_friendly",
            LastSeen = DateTime.UtcNow,
            DeviceStatus = DeviceStatus.ACTIVE,
        };
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
        using var scope = _appFactory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<LorisDbContext>();
        ctx.Devices.Add(device);
        await ctx.SaveChangesAsync(CancellationToken.None);

        var jsonOptions = _appFactory
            .Services.GetRequiredService<IOptions<JsonOptions>>()
            .Value.JsonSerializerOptions;
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
        var device1 = new Device
        {
            DeviceName = "loris_sample_1",
            DevEui = DevEui.Parse("1122AABBCCDDEEFF"),
            FriendlyName = "loris_friendly_1",
            LastSeen = DateTime.UtcNow,
            DeviceStatus = DeviceStatus.ACTIVE,
        };
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
        var device2 = new Device
        {
            DeviceName = "loris_sample_2",
            DevEui = DevEui.Parse("1122AABBCCDDEEF0"),
            FriendlyName = "loris_friendly_2",
            LastSeen = DateTime.UtcNow,
            DeviceStatus = DeviceStatus.PENDING,
        };
        var device3 = new Device
        {
            DeviceName = "loris_sample_3",
            DevEui = DevEui.Parse("1122AABBCCDDEEF1"),
            FriendlyName = "loris_friendly_3",
            LastSeen = DateTime.UtcNow,
            DeviceStatus = DeviceStatus.IGNORED,
        };
        using var scope = _appFactory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<LorisDbContext>();
        ctx.Devices.AddRange(device1, device2, device3);
        await ctx.SaveChangesAsync(CancellationToken.None);

        var jsonOptions = _appFactory
            .Services.GetRequiredService<IOptions<JsonOptions>>()
            .Value.JsonSerializerOptions;
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
        var device = new Device
        {
            DeviceName = "loris_sample",
            DevEui = DevEui.Parse("1122AABBCCDDEEFF"),
            FriendlyName = "loris_friendly",
            LastSeen = DateTime.UtcNow,
            DeviceStatus = DeviceStatus.ACTIVE,
        };
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
        using var scope = _appFactory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<LorisDbContext>();
        ctx.Devices.Add(device);
        await ctx.SaveChangesAsync(CancellationToken.None);

        var jsonOptions = _appFactory
            .Services.GetRequiredService<IOptions<JsonOptions>>()
            .Value.JsonSerializerOptions;
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
        var jsonOptions = _appFactory
            .Services.GetRequiredService<IOptions<JsonOptions>>()
            .Value.JsonSerializerOptions;
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
        var devEui = DevEui.Parse("1122AABBCCDDEEFF");
        var device = new Device
        {
            DeviceName = "loris_sample",
            DevEui = devEui,
            FriendlyName = "loris_friendly",
            LastSeen = DateTime.UtcNow,
            DeviceStatus = DeviceStatus.ACTIVE,
        };
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
        using var scope = _appFactory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<LorisDbContext>();
        ctx.Devices.Add(device);
        await ctx.SaveChangesAsync(CancellationToken.None);

        var jsonOptions = _appFactory
            .Services.GetRequiredService<IOptions<JsonOptions>>()
            .Value.JsonSerializerOptions;
        var expectedResponse = DeviceDetailsResponse.FromModel(device);
        var client = _appFactory.CreateClient();

        // Act
        var httpResponse = await client.GetAsync(
            $"/api/devices/by-eui/{devEui}",
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
        var jsonOptions = _appFactory
            .Services.GetRequiredService<IOptions<JsonOptions>>()
            .Value.JsonSerializerOptions;
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
        var device = new Device
        {
            DeviceName = "loris_sample",
            DevEui = DevEui.Parse("1122AABBCCDDEEFF"),
            FriendlyName = "loris_friendly",
            LastSeen = DateTime.UtcNow,
            DeviceStatus = DeviceStatus.ACTIVE,
        };
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
        using var scope = _appFactory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<LorisDbContext>();
        ctx.Devices.Add(device);
        await ctx.SaveChangesAsync(CancellationToken.None);

        var jsonOptions = _appFactory
            .Services.GetRequiredService<IOptions<JsonOptions>>()
            .Value.JsonSerializerOptions;
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
        var jsonOptions = _appFactory
            .Services.GetRequiredService<IOptions<JsonOptions>>()
            .Value.JsonSerializerOptions;
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
        var device = new Device
        {
            DeviceName = "loris_sample",
            DevEui = DevEui.Parse("1122AABBCCDDEEFF"),
            FriendlyName = "loris_friendly",
            LastSeen = DateTime.UtcNow,
            DeviceStatus = DeviceStatus.ACTIVE,
        };
        using var scope = _appFactory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<LorisDbContext>();
        ctx.Devices.Add(device);
        await ctx.SaveChangesAsync(CancellationToken.None);

        var request = new RenameDeviceRequest { NewFriendlyName = "loris_renamed" };
        var jsonOptions = _appFactory
            .Services.GetRequiredService<IOptions<JsonOptions>>()
            .Value.JsonSerializerOptions;
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
        var jsonOptions = _appFactory
            .Services.GetRequiredService<IOptions<JsonOptions>>()
            .Value.JsonSerializerOptions;
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
        var device1 = new Device
        {
            DeviceName = "loris_sample_1",
            DevEui = DevEui.Parse("1122AABBCCDDEEFF"),
            FriendlyName = "loris_friendly_1",
            LastSeen = DateTime.UtcNow,
            DeviceStatus = DeviceStatus.ACTIVE,
        };
        var device2 = new Device
        {
            DeviceName = "loris_sample_2",
            DevEui = DevEui.Parse("1122AABBCCDDEEF0"),
            FriendlyName = "loris_friendly_2",
            LastSeen = DateTime.UtcNow,
            DeviceStatus = DeviceStatus.ACTIVE,
        };
        using var scope = _appFactory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<LorisDbContext>();
        ctx.Devices.AddRange(device1, device2);
        await ctx.SaveChangesAsync(CancellationToken.None);

        var request = new RenameDeviceRequest { NewFriendlyName = device2.FriendlyName };
        var jsonOptions = _appFactory
            .Services.GetRequiredService<IOptions<JsonOptions>>()
            .Value.JsonSerializerOptions;
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
        var devEui = DevEui.Parse("1122AABBCCDDEEFF");
        var device = new Device
        {
            DeviceName = "loris_sample",
            DevEui = devEui,
            FriendlyName = "loris_friendly",
            LastSeen = DateTime.UtcNow,
            DeviceStatus = DeviceStatus.ACTIVE,
        };
        using var scope = _appFactory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<LorisDbContext>();
        ctx.Devices.Add(device);
        await ctx.SaveChangesAsync(CancellationToken.None);

        var request = new RenameDeviceRequest { NewFriendlyName = "loris_renamed" };
        var jsonOptions = _appFactory
            .Services.GetRequiredService<IOptions<JsonOptions>>()
            .Value.JsonSerializerOptions;
        var stateCache = _appFactory.Services.GetRequiredService<IStateCache>();
        var client = _appFactory.CreateClient();

        // Act
        var httpResponse = await client.PatchAsJsonAsync(
            $"/api/devices/by-eui/{devEui}",
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
        Assert.True(stateCache.TryGetDevice(devEui, out var cachedDevice));
        Assert.Equal("loris_renamed", cachedDevice.FriendlyName);
    }

    [Fact]
    public async Task Patch_RenameByEui_ReturnsNotFoundWhenDeviceDoesNotExist()
    {
        // Arrange
        var nonExistentEui = DevEui.Parse("FFEEDDCCBBAA2211");
        var request = new RenameDeviceRequest { NewFriendlyName = "loris_renamed" };
        var jsonOptions = _appFactory
            .Services.GetRequiredService<IOptions<JsonOptions>>()
            .Value.JsonSerializerOptions;
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
        var device1 = new Device
        {
            DeviceName = "loris_sample_1",
            DevEui = DevEui.Parse("1122AABBCCDDEEFF"),
            FriendlyName = "loris_friendly_1",
            LastSeen = DateTime.UtcNow,
            DeviceStatus = DeviceStatus.ACTIVE,
        };
        var device2 = new Device
        {
            DeviceName = "loris_sample_2",
            DevEui = DevEui.Parse("1122AABBCCDDEEF0"),
            FriendlyName = "loris_friendly_2",
            LastSeen = DateTime.UtcNow,
            DeviceStatus = DeviceStatus.ACTIVE,
        };
        using var scope = _appFactory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<LorisDbContext>();
        ctx.Devices.AddRange(device1, device2);
        await ctx.SaveChangesAsync(CancellationToken.None);

        var request = new RenameDeviceRequest { NewFriendlyName = device2.FriendlyName };
        var jsonOptions = _appFactory
            .Services.GetRequiredService<IOptions<JsonOptions>>()
            .Value.JsonSerializerOptions;
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
        var device = new Device
        {
            DeviceName = "loris_sample",
            DevEui = DevEui.Parse("1122AABBCCDDEEFF"),
            FriendlyName = "loris_friendly",
            LastSeen = DateTime.UtcNow,
            DeviceStatus = DeviceStatus.ACTIVE,
        };
        using var scope = _appFactory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<LorisDbContext>();
        ctx.Devices.Add(device);
        await ctx.SaveChangesAsync(CancellationToken.None);

        var request = new RenameDeviceRequest { NewFriendlyName = "loris_renamed" };
        var jsonOptions = _appFactory
            .Services.GetRequiredService<IOptions<JsonOptions>>()
            .Value.JsonSerializerOptions;
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
        var jsonOptions = _appFactory
            .Services.GetRequiredService<IOptions<JsonOptions>>()
            .Value.JsonSerializerOptions;
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
        var device1 = new Device
        {
            DeviceName = "loris_sample_1",
            DevEui = DevEui.Parse("1122AABBCCDDEEFF"),
            FriendlyName = "loris_friendly_1",
            LastSeen = DateTime.UtcNow,
            DeviceStatus = DeviceStatus.ACTIVE,
        };
        var device2 = new Device
        {
            DeviceName = "loris_sample_2",
            DevEui = DevEui.Parse("1122AABBCCDDEEF0"),
            FriendlyName = "loris_friendly_2",
            LastSeen = DateTime.UtcNow,
            DeviceStatus = DeviceStatus.ACTIVE,
        };
        using var scope = _appFactory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<LorisDbContext>();
        ctx.Devices.AddRange(device1, device2);
        await ctx.SaveChangesAsync(CancellationToken.None);

        var request = new RenameDeviceRequest { NewFriendlyName = device2.FriendlyName };
        var jsonOptions = _appFactory
            .Services.GetRequiredService<IOptions<JsonOptions>>()
            .Value.JsonSerializerOptions;
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
        var device = new Device
        {
            DeviceName = "loris_sample",
            DevEui = DevEui.Parse("1122AABBCCDDEEFF"),
            FriendlyName = "loris_friendly",
            LastSeen = DateTime.UtcNow,
            DeviceStatus = initialStatus,
        };
        using var scope = _appFactory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<LorisDbContext>();
        ctx.Devices.Add(device);
        await ctx.SaveChangesAsync(CancellationToken.None);

        var jsonOptions = _appFactory
            .Services.GetRequiredService<IOptions<JsonOptions>>()
            .Value.JsonSerializerOptions;
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
        var device = new Device
        {
            DeviceName = "loris_sample",
            DevEui = DevEui.Parse("1122AABBCCDDEEFF"),
            FriendlyName = "loris_friendly",
            LastSeen = DateTime.UtcNow,
            DeviceStatus = initialStatus,
        };
        using var scope = _appFactory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<LorisDbContext>();
        ctx.Devices.Add(device);
        await ctx.SaveChangesAsync(CancellationToken.None);

        var jsonOptions = _appFactory
            .Services.GetRequiredService<IOptions<JsonOptions>>()
            .Value.JsonSerializerOptions;
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
        var jsonOptions = _appFactory
            .Services.GetRequiredService<IOptions<JsonOptions>>()
            .Value.JsonSerializerOptions;
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
        var device = new Device
        {
            DeviceName = "loris_sample",
            DevEui = DevEui.Parse("1122AABBCCDDEEFF"),
            FriendlyName = "loris_friendly",
            LastSeen = DateTime.UtcNow,
            DeviceStatus = initialStatus,
        };
        using var scope = _appFactory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<LorisDbContext>();
        ctx.Devices.Add(device);
        await ctx.SaveChangesAsync(CancellationToken.None);

        var jsonOptions = _appFactory
            .Services.GetRequiredService<IOptions<JsonOptions>>()
            .Value.JsonSerializerOptions;
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
        var device = new Device
        {
            DeviceName = "loris_sample",
            DevEui = DevEui.Parse("1122AABBCCDDEEFF"),
            FriendlyName = "loris_friendly",
            LastSeen = DateTime.UtcNow,
            DeviceStatus = initialStatus,
        };
        using var scope = _appFactory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<LorisDbContext>();
        ctx.Devices.Add(device);
        await ctx.SaveChangesAsync(CancellationToken.None);

        var jsonOptions = _appFactory
            .Services.GetRequiredService<IOptions<JsonOptions>>()
            .Value.JsonSerializerOptions;
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
        var jsonOptions = _appFactory
            .Services.GetRequiredService<IOptions<JsonOptions>>()
            .Value.JsonSerializerOptions;
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
        var devEui = DevEui.Parse("1122AABBCCDDEEFF");
        var device = new Device
        {
            DeviceName = "loris_sample",
            DevEui = devEui,
            FriendlyName = "loris_friendly",
            LastSeen = DateTime.UtcNow,
            DeviceStatus = initialStatus,
        };
        using var scope = _appFactory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<LorisDbContext>();
        ctx.Devices.Add(device);
        await ctx.SaveChangesAsync(CancellationToken.None);

        var jsonOptions = _appFactory
            .Services.GetRequiredService<IOptions<JsonOptions>>()
            .Value.JsonSerializerOptions;
        var stateCache = _appFactory.Services.GetRequiredService<IStateCache>();
        var client = _appFactory.CreateClient();

        // Act
        var httpResponse = await client.PostAsync(
            $"/api/devices/by-eui/{devEui}/ignore?revert={revert}",
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
        Assert.True(stateCache.TryGetDevice(devEui, out var cachedDevice));
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
        var devEui = DevEui.Parse("1122AABBCCDDEEFF");
        var device = new Device
        {
            DeviceName = "loris_sample",
            DevEui = devEui,
            FriendlyName = "loris_friendly",
            LastSeen = DateTime.UtcNow,
            DeviceStatus = initialStatus,
        };
        using var scope = _appFactory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<LorisDbContext>();
        ctx.Devices.Add(device);
        await ctx.SaveChangesAsync(CancellationToken.None);

        var jsonOptions = _appFactory
            .Services.GetRequiredService<IOptions<JsonOptions>>()
            .Value.JsonSerializerOptions;
        var client = _appFactory.CreateClient();

        // Act
        var httpResponse = await client.PostAsync(
            $"/api/devices/by-eui/{devEui}/ignore?revert={revert}",
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
        var jsonOptions = _appFactory
            .Services.GetRequiredService<IOptions<JsonOptions>>()
            .Value.JsonSerializerOptions;
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
        var appFactory = _appFactory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddTransient<IChirpstackService, FakeChirpstackServiceWithSuccess>();
            });
        });
        var device = new Device
        {
            DeviceName = "loris_sample",
            DevEui = DevEui.Parse("1122AABBCCDDEEFF"),
            FriendlyName = "loris_friendly",
            LastSeen = DateTime.UtcNow,
            DeviceStatus = DeviceStatus.PENDING,
        };
        using var scope = appFactory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<LorisDbContext>();
        ctx.Devices.Add(device);
        await ctx.SaveChangesAsync(CancellationToken.None);

        var jsonOptions = appFactory
            .Services.GetRequiredService<IOptions<JsonOptions>>()
            .Value.JsonSerializerOptions;
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
        var appFactory = _appFactory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddTransient<IChirpstackService, FakeChirpstackServiceWithFailed>();
            });
        });
        var device = new Device
        {
            DeviceName = "loris_sample",
            DevEui = DevEui.Parse("1122AABBCCDDEEFF"),
            FriendlyName = "loris_friendly",
            LastSeen = DateTime.UtcNow,
            DeviceStatus = DeviceStatus.PENDING,
        };
        using var scope = appFactory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<LorisDbContext>();
        ctx.Devices.Add(device);
        await ctx.SaveChangesAsync(CancellationToken.None);

        var jsonOptions = appFactory
            .Services.GetRequiredService<IOptions<JsonOptions>>()
            .Value.JsonSerializerOptions;
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
        var appFactory = _appFactory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddTransient<IChirpstackService, FakeChirpstackServiceWithTimeout>();
            });
        });
        var device = new Device
        {
            DeviceName = "loris_sample",
            DevEui = DevEui.Parse("1122AABBCCDDEEFF"),
            FriendlyName = "loris_friendly",
            LastSeen = DateTime.UtcNow,
            DeviceStatus = DeviceStatus.PENDING,
        };
        using var scope = appFactory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<LorisDbContext>();
        ctx.Devices.Add(device);
        await ctx.SaveChangesAsync(CancellationToken.None);

        var jsonOptions = appFactory
            .Services.GetRequiredService<IOptions<JsonOptions>>()
            .Value.JsonSerializerOptions;
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
        var appFactory = _appFactory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddTransient<IChirpstackService, FakeChirpstackServiceWithSuccess>();
            });
        });
        var device = new Device
        {
            DeviceName = "loris_sample",
            DevEui = DevEui.Parse("1122AABBCCDDEEFF"),
            FriendlyName = "loris_friendly",
            LastSeen = DateTime.UtcNow,
            DeviceStatus = initialStatus,
        };
        using var scope = appFactory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<LorisDbContext>();
        ctx.Devices.Add(device);
        await ctx.SaveChangesAsync(CancellationToken.None);

        var jsonOptions = appFactory
            .Services.GetRequiredService<IOptions<JsonOptions>>()
            .Value.JsonSerializerOptions;
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
        var appFactory = _appFactory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddTransient<IChirpstackService, FakeChirpstackServiceWithSuccess>();
            });
        });
        var jsonOptions = appFactory
            .Services.GetRequiredService<IOptions<JsonOptions>>()
            .Value.JsonSerializerOptions;
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
        var appFactory = _appFactory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddTransient<IChirpstackService, FakeChirpstackServiceWithSuccess>();
            });
        });
        var device = new Device
        {
            DeviceName = "loris_sample",
            DevEui = DevEui.Parse("1122AABBCCDDEEFF"),
            FriendlyName = "loris_friendly",
            LastSeen = DateTime.UtcNow,
            DeviceStatus = DeviceStatus.PENDING,
        };
        using var scope = appFactory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<LorisDbContext>();
        ctx.Devices.Add(device);
        await ctx.SaveChangesAsync(CancellationToken.None);

        var jsonOptions = appFactory
            .Services.GetRequiredService<IOptions<JsonOptions>>()
            .Value.JsonSerializerOptions;
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
        var appFactory = _appFactory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddTransient<IChirpstackService, FakeChirpstackServiceWithFailed>();
            });
        });
        var device = new Device
        {
            DeviceName = "loris_sample",
            DevEui = DevEui.Parse("1122AABBCCDDEEFF"),
            FriendlyName = "loris_friendly",
            LastSeen = DateTime.UtcNow,
            DeviceStatus = DeviceStatus.PENDING,
        };
        using var scope = appFactory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<LorisDbContext>();
        ctx.Devices.Add(device);
        await ctx.SaveChangesAsync(CancellationToken.None);

        var jsonOptions = appFactory
            .Services.GetRequiredService<IOptions<JsonOptions>>()
            .Value.JsonSerializerOptions;
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
        var appFactory = _appFactory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddTransient<IChirpstackService, FakeChirpstackServiceWithTimeout>();
            });
        });
        var device = new Device
        {
            DeviceName = "loris_sample",
            DevEui = DevEui.Parse("1122AABBCCDDEEFF"),
            FriendlyName = "loris_friendly",
            LastSeen = DateTime.UtcNow,
            DeviceStatus = DeviceStatus.PENDING,
        };
        using var scope = appFactory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<LorisDbContext>();
        ctx.Devices.Add(device);
        await ctx.SaveChangesAsync(CancellationToken.None);

        var jsonOptions = appFactory
            .Services.GetRequiredService<IOptions<JsonOptions>>()
            .Value.JsonSerializerOptions;
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
        var appFactory = _appFactory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddTransient<IChirpstackService, FakeChirpstackServiceWithSuccess>();
            });
        });
        var device = new Device
        {
            DeviceName = "loris_sample",
            DevEui = DevEui.Parse("1122AABBCCDDEEFF"),
            FriendlyName = "loris_friendly",
            LastSeen = DateTime.UtcNow,
            DeviceStatus = initialStatus,
        };
        using var scope = appFactory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<LorisDbContext>();
        ctx.Devices.Add(device);
        await ctx.SaveChangesAsync(CancellationToken.None);

        var jsonOptions = appFactory
            .Services.GetRequiredService<IOptions<JsonOptions>>()
            .Value.JsonSerializerOptions;
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
        var appFactory = _appFactory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddTransient<IChirpstackService, FakeChirpstackServiceWithSuccess>();
            });
        });
        var jsonOptions = appFactory
            .Services.GetRequiredService<IOptions<JsonOptions>>()
            .Value.JsonSerializerOptions;
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
        var appFactory = _appFactory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddTransient<IChirpstackService, FakeChirpstackServiceWithSuccess>();
            });
        });
        var devEui = DevEui.Parse("1122AABBCCDDEEFF");
        var device = new Device
        {
            DeviceName = "loris_sample",
            DevEui = devEui,
            FriendlyName = "loris_friendly",
            LastSeen = DateTime.UtcNow,
            DeviceStatus = DeviceStatus.PENDING,
        };
        using var scope = appFactory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<LorisDbContext>();
        ctx.Devices.Add(device);
        await ctx.SaveChangesAsync(CancellationToken.None);

        var jsonOptions = appFactory
            .Services.GetRequiredService<IOptions<JsonOptions>>()
            .Value.JsonSerializerOptions;
        var stateCache = appFactory.Services.GetRequiredService<IStateCache>();
        var client = appFactory.CreateClient();

        // Act
        var httpResponse = await client.PostAsync(
            $"/api/devices/by-eui/{devEui}/accept",
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
        Assert.True(stateCache.TryGetDevice(devEui, out var cachedDevice));
        Assert.Equal(DeviceStatus.ACTIVE, cachedDevice.DeviceStatus);
    }

    [Fact]
    public async Task Post_AcceptByEui_ReturnsBadGatewayWhenChirpstackFails()
    {
        // Arrange
        var appFactory = _appFactory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddTransient<IChirpstackService, FakeChirpstackServiceWithFailed>();
            });
        });
        var devEui = DevEui.Parse("1122AABBCCDDEEFF");
        var device = new Device
        {
            DeviceName = "loris_sample",
            DevEui = devEui,
            FriendlyName = "loris_friendly",
            LastSeen = DateTime.UtcNow,
            DeviceStatus = DeviceStatus.PENDING,
        };
        using var scope = appFactory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<LorisDbContext>();
        ctx.Devices.Add(device);
        await ctx.SaveChangesAsync(CancellationToken.None);

        var jsonOptions = appFactory
            .Services.GetRequiredService<IOptions<JsonOptions>>()
            .Value.JsonSerializerOptions;
        var stateCache = appFactory.Services.GetRequiredService<IStateCache>();
        stateCache[device.DevEui] = device;
        var client = appFactory.CreateClient();

        // Act
        var httpResponse = await client.PostAsync(
            $"/api/devices/by-eui/{devEui}/accept",
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
        var appFactory = _appFactory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddTransient<IChirpstackService, FakeChirpstackServiceWithTimeout>();
            });
        });
        var devEui = DevEui.Parse("1122AABBCCDDEEFF");
        var device = new Device
        {
            DeviceName = "loris_sample",
            DevEui = devEui,
            FriendlyName = "loris_friendly",
            LastSeen = DateTime.UtcNow,
            DeviceStatus = DeviceStatus.PENDING,
        };
        using var scope = appFactory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<LorisDbContext>();
        ctx.Devices.Add(device);
        await ctx.SaveChangesAsync(CancellationToken.None);

        var jsonOptions = appFactory
            .Services.GetRequiredService<IOptions<JsonOptions>>()
            .Value.JsonSerializerOptions;
        var stateCache = appFactory.Services.GetRequiredService<IStateCache>();
        stateCache[device.DevEui] = device;
        var client = appFactory.CreateClient();

        // Act
        var httpResponse = await client.PostAsync(
            $"/api/devices/by-eui/{devEui}/accept",
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
        var appFactory = _appFactory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddTransient<IChirpstackService, FakeChirpstackServiceWithSuccess>();
            });
        });
        var devEui = DevEui.Parse("1122AABBCCDDEEFF");
        var device = new Device
        {
            DeviceName = "loris_sample",
            DevEui = devEui,
            FriendlyName = "loris_friendly",
            LastSeen = DateTime.UtcNow,
            DeviceStatus = initialStatus,
        };
        using var scope = appFactory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<LorisDbContext>();
        ctx.Devices.Add(device);
        await ctx.SaveChangesAsync(CancellationToken.None);

        var jsonOptions = appFactory
            .Services.GetRequiredService<IOptions<JsonOptions>>()
            .Value.JsonSerializerOptions;
        var stateCache = appFactory.Services.GetRequiredService<IStateCache>();
        stateCache[device.DevEui] = device;
        var client = appFactory.CreateClient();

        // Act
        var httpResponse = await client.PostAsync(
            $"/api/devices/by-eui/{devEui}/accept",
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
        var appFactory = _appFactory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddTransient<IChirpstackService, FakeChirpstackServiceWithSuccess>();
            });
        });
        var nonExistentEui = DevEui.Parse("FFEEDDCCBBAA2211");
        var jsonOptions = appFactory
            .Services.GetRequiredService<IOptions<JsonOptions>>()
            .Value.JsonSerializerOptions;
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
        var appFactory = _appFactory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddTransient<IChirpstackService, FakeChirpstackServiceWithSuccess>();
            });
        });
        var device = new Device
        {
            DeviceName = "loris_sample",
            DevEui = DevEui.Parse("1122AABBCCDDEEFF"),
            FriendlyName = "loris_friendly",
            LastSeen = DateTime.UtcNow,
            DeviceStatus = initialStatus,
        };
        using var scope = appFactory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<LorisDbContext>();
        ctx.Devices.Add(device);
        await ctx.SaveChangesAsync(CancellationToken.None);

        var jsonOptions = appFactory
            .Services.GetRequiredService<IOptions<JsonOptions>>()
            .Value.JsonSerializerOptions;
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
        var appFactory = _appFactory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddTransient<IChirpstackService, FakeChirpstackServiceWithFailed>();
            });
        });
        var device = new Device
        {
            DeviceName = "loris_sample",
            DevEui = DevEui.Parse("1122AABBCCDDEEFF"),
            FriendlyName = "loris_friendly",
            LastSeen = DateTime.UtcNow,
            DeviceStatus = DeviceStatus.ACTIVE,
        };
        using var scope = appFactory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<LorisDbContext>();
        ctx.Devices.Add(device);
        await ctx.SaveChangesAsync(CancellationToken.None);

        var jsonOptions = appFactory
            .Services.GetRequiredService<IOptions<JsonOptions>>()
            .Value.JsonSerializerOptions;
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
        var appFactory = _appFactory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddTransient<IChirpstackService, FakeChirpstackServiceWithTimeout>();
            });
        });
        var device = new Device
        {
            DeviceName = "loris_sample",
            DevEui = DevEui.Parse("1122AABBCCDDEEFF"),
            FriendlyName = "loris_friendly",
            LastSeen = DateTime.UtcNow,
            DeviceStatus = DeviceStatus.ACTIVE,
        };
        using var scope = appFactory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<LorisDbContext>();
        ctx.Devices.Add(device);
        await ctx.SaveChangesAsync(CancellationToken.None);

        var jsonOptions = appFactory
            .Services.GetRequiredService<IOptions<JsonOptions>>()
            .Value.JsonSerializerOptions;
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
        var appFactory = _appFactory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddTransient<IChirpstackService, FakeChirpstackServiceWithSuccess>();
            });
        });
        var device = new Device
        {
            DeviceName = "loris_sample",
            DevEui = DevEui.Parse("1122AABBCCDDEEFF"),
            FriendlyName = "loris_friendly",
            LastSeen = DateTime.UtcNow,
            DeviceStatus = initialStatus,
        };
        using var scope = appFactory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<LorisDbContext>();
        ctx.Devices.Add(device);
        await ctx.SaveChangesAsync(CancellationToken.None);

        var jsonOptions = appFactory
            .Services.GetRequiredService<IOptions<JsonOptions>>()
            .Value.JsonSerializerOptions;
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
        var appFactory = _appFactory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddTransient<IChirpstackService, FakeChirpstackServiceWithSuccess>();
            });
        });
        var jsonOptions = appFactory
            .Services.GetRequiredService<IOptions<JsonOptions>>()
            .Value.JsonSerializerOptions;
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
        var appFactory = _appFactory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddTransient<IChirpstackService, FakeChirpstackServiceWithSuccess>();
            });
        });
        var device = new Device
        {
            DeviceName = "loris_sample",
            DevEui = DevEui.Parse("1122AABBCCDDEEFF"),
            FriendlyName = "loris_friendly",
            LastSeen = DateTime.UtcNow,
            DeviceStatus = initialStatus,
        };
        using var scope = appFactory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<LorisDbContext>();
        ctx.Devices.Add(device);
        await ctx.SaveChangesAsync(CancellationToken.None);

        var jsonOptions = appFactory
            .Services.GetRequiredService<IOptions<JsonOptions>>()
            .Value.JsonSerializerOptions;
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
        var appFactory = _appFactory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddTransient<IChirpstackService, FakeChirpstackServiceWithFailed>();
            });
        });
        var device = new Device
        {
            DeviceName = "loris_sample",
            DevEui = DevEui.Parse("1122AABBCCDDEEFF"),
            FriendlyName = "loris_friendly",
            LastSeen = DateTime.UtcNow,
            DeviceStatus = DeviceStatus.ACTIVE,
        };
        using var scope = appFactory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<LorisDbContext>();
        ctx.Devices.Add(device);
        await ctx.SaveChangesAsync(CancellationToken.None);

        var jsonOptions = appFactory
            .Services.GetRequiredService<IOptions<JsonOptions>>()
            .Value.JsonSerializerOptions;
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
        var appFactory = _appFactory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddTransient<IChirpstackService, FakeChirpstackServiceWithTimeout>();
            });
        });
        var device = new Device
        {
            DeviceName = "loris_sample",
            DevEui = DevEui.Parse("1122AABBCCDDEEFF"),
            FriendlyName = "loris_friendly",
            LastSeen = DateTime.UtcNow,
            DeviceStatus = DeviceStatus.ACTIVE,
        };
        using var scope = appFactory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<LorisDbContext>();
        ctx.Devices.Add(device);
        await ctx.SaveChangesAsync(CancellationToken.None);

        var jsonOptions = appFactory
            .Services.GetRequiredService<IOptions<JsonOptions>>()
            .Value.JsonSerializerOptions;
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
        var appFactory = _appFactory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddTransient<IChirpstackService, FakeChirpstackServiceWithSuccess>();
            });
        });
        var device = new Device
        {
            DeviceName = "loris_sample",
            DevEui = DevEui.Parse("1122AABBCCDDEEFF"),
            FriendlyName = "loris_friendly",
            LastSeen = DateTime.UtcNow,
            DeviceStatus = initialStatus,
        };
        using var scope = appFactory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<LorisDbContext>();
        ctx.Devices.Add(device);
        await ctx.SaveChangesAsync(CancellationToken.None);

        var jsonOptions = appFactory
            .Services.GetRequiredService<IOptions<JsonOptions>>()
            .Value.JsonSerializerOptions;
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
        var appFactory = _appFactory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddTransient<IChirpstackService, FakeChirpstackServiceWithSuccess>();
            });
        });
        var jsonOptions = appFactory
            .Services.GetRequiredService<IOptions<JsonOptions>>()
            .Value.JsonSerializerOptions;
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
        var appFactory = _appFactory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddTransient<IChirpstackService, FakeChirpstackServiceWithSuccess>();
            });
        });
        var devEui = DevEui.Parse("1122AABBCCDDEEFF");
        var device = new Device
        {
            DeviceName = "loris_sample",
            DevEui = devEui,
            FriendlyName = "loris_friendly",
            LastSeen = DateTime.UtcNow,
            DeviceStatus = initialStatus,
        };
        using var scope = appFactory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<LorisDbContext>();
        ctx.Devices.Add(device);
        await ctx.SaveChangesAsync(CancellationToken.None);

        var jsonOptions = appFactory
            .Services.GetRequiredService<IOptions<JsonOptions>>()
            .Value.JsonSerializerOptions;
        var stateCache = appFactory.Services.GetRequiredService<IStateCache>();
        var client = appFactory.CreateClient();

        // Act
        var httpResponse = await client.PostAsync(
            $"/api/devices/by-eui/{devEui}/disconnect?force={force}",
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
        Assert.True(stateCache.TryGetDevice(devEui, out var cachedDevice));
        Assert.Equal(DeviceStatus.DISCONNECTED, cachedDevice.DeviceStatus);
    }

    [Fact]
    public async Task Post_DisconnectByEui_ReturnsEmptyDeviceResponseWhenDeviceDoesNotExistAndForceIsTrue()
    {
        // Arrange
        var appFactory = _appFactory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddTransient<IChirpstackService, FakeChirpstackServiceWithSuccess>();
            });
        });
        var unknownEui = DevEui.Parse("FFEEDDCCBBAA2211");
        var expectedResponse = DeviceResponse.FromModel(new Device());
        var jsonOptions = appFactory
            .Services.GetRequiredService<IOptions<JsonOptions>>()
            .Value.JsonSerializerOptions;
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
        var appFactory = _appFactory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddTransient<IChirpstackService, FakeChirpstackServiceWithFailed>();
            });
        });
        var devEui = DevEui.Parse("1122AABBCCDDEEFF");
        var stateCache = appFactory.Services.GetRequiredService<IStateCache>();
        if (deviceExists)
        {
            var device = new Device
            {
                DeviceName = "loris_sample",
                DevEui = devEui,
                FriendlyName = "loris_friendly",
                LastSeen = DateTime.UtcNow,
                DeviceStatus = DeviceStatus.ACTIVE,
            };
            using var scope = appFactory.Services.CreateScope();
            var ctx = scope.ServiceProvider.GetRequiredService<LorisDbContext>();
            ctx.Devices.Add(device);
            await ctx.SaveChangesAsync(CancellationToken.None);
            stateCache[devEui] = device;
        }

        var jsonOptions = appFactory
            .Services.GetRequiredService<IOptions<JsonOptions>>()
            .Value.JsonSerializerOptions;
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
        var appFactory = _appFactory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddTransient<IChirpstackService, FakeChirpstackServiceWithTimeout>();
            });
        });
        var devEui = DevEui.Parse("1122AABBCCDDEEFF");
        var stateCache = appFactory.Services.GetRequiredService<IStateCache>();
        if (deviceExists)
        {
            var device = new Device
            {
                DeviceName = "loris_sample",
                DevEui = devEui,
                FriendlyName = "loris_friendly",
                LastSeen = DateTime.UtcNow,
                DeviceStatus = DeviceStatus.ACTIVE,
            };
            using var scope = appFactory.Services.CreateScope();
            var ctx = scope.ServiceProvider.GetRequiredService<LorisDbContext>();
            ctx.Devices.Add(device);
            await ctx.SaveChangesAsync(CancellationToken.None);
            stateCache[devEui] = device;
        }

        var jsonOptions = appFactory
            .Services.GetRequiredService<IOptions<JsonOptions>>()
            .Value.JsonSerializerOptions;
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
        var appFactory = _appFactory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddTransient<IChirpstackService, FakeChirpstackServiceWithSuccess>();
            });
        });
        var devEui = DevEui.Parse("1122AABBCCDDEEFF");
        var device = new Device
        {
            DeviceName = "loris_sample",
            DevEui = devEui,
            FriendlyName = "loris_friendly",
            LastSeen = DateTime.UtcNow,
            DeviceStatus = initialStatus,
        };
        using var scope = appFactory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<LorisDbContext>();
        ctx.Devices.Add(device);
        await ctx.SaveChangesAsync(CancellationToken.None);

        var jsonOptions = appFactory
            .Services.GetRequiredService<IOptions<JsonOptions>>()
            .Value.JsonSerializerOptions;
        var stateCache = appFactory.Services.GetRequiredService<IStateCache>();
        stateCache[devEui] = device;
        var client = appFactory.CreateClient();

        // Act
        var httpResponse = await client.PostAsync(
            $"/api/devices/by-eui/{devEui}/disconnect?force=false",
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
        Assert.Equal(initialStatus, stateCache[devEui].DeviceStatus);
    }

    [Fact]
    public async Task Post_DisconnectByEui_ReturnsNotFoundWhenDeviceDoesNotExistAndForceIsFalse()
    {
        // Arrange
        var appFactory = _appFactory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddTransient<IChirpstackService, FakeChirpstackServiceWithSuccess>();
            });
        });
        var nonExistentEui = DevEui.Parse("FFEEDDCCBBAA2211");
        var jsonOptions = appFactory
            .Services.GetRequiredService<IOptions<JsonOptions>>()
            .Value.JsonSerializerOptions;
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
}
