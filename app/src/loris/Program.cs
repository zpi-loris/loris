using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Loris.Channels;
using Loris.Consumers;
using Loris.Data;
using loris.Extensions;
using Loris.Extensions;
using Loris.Jobs;
using Loris.Json;
using Loris.MQTT.Client;
using loris.Services;
using Loris.Services;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

var socketPath = Path.Combine("/", "run", "loris", "loris.socket");
if (builder.Environment.IsDevelopment())
{
    socketPath = Path.Combine(Path.GetTempPath(), "loris.socket");
}

builder.WebHost.ConfigureLorisLocalhost(port: 5158);
builder.WebHost.ConfigureLorisUnixSocket(socketPath: socketPath);

// add factory dbContext. It will serve as an entry point for factory retrieval
// as well as single, scoped DbContext instance for each request.
string? connectionString = builder.Configuration.GetConnectionString("Timescale");
builder.Services.AddDbContextFactory<LorisDbContext>(options =>
    options.UseNpgsql(connectionString)
);

//
// Global state
//
builder.Services.AddSingleton<IAutomationIndex, AutomationIndex>();
builder.Services.AddSingleton<IStateCache, StateCache>();

//
// Configure channels for the ingestion subsystem
//
builder.Services.AddChannels();

//
// Pre startup tasks.
// ORDER IS IMPORTANT
//
builder.Services.AddHostedService<PreStartupAutomationJob>();
builder.Services.AddHostedService<PreStartupStateCacheJob>();

// Configure consumers for each channel
builder.Services.AddHostedService<DeviceChannelConsumer>();
builder.Services.AddHostedService<DbWriteChannelConsumer>();
builder.Services.AddHostedService<EventChannelConsumer>();

// Configure handlers for each channel consumer
builder.Services.AddSingleton<IDeviceChannelHandler, DeviceChannelHandler>();
builder.Services.AddSingleton<IDbWriteChannelHandler, DbWriteChannelHandler>();
builder.Services.AddSingleton<IEventChannelHandler, EventChannelHandler>();

// Register all custom automations blocks compilers
builder.Services.AddAutomationsCompilers();

// Services for controller
builder.Services.AddTransient<IChirpstackService, ChirpstackService>();
builder.Services.AddTransient<IAutomationService, AutomationService>();
builder.Services.AddTransient<IDeviceService, DeviceService>();
builder.Services.AddTransient<IUserService, UserService>();

// Chirpstacks integration background service and device grpc downlink service
builder.Services.AddChirpstackIntegration<ChirpstackIntegrationHandler>(builder.Configuration);
builder.Services.AddChirpstackDeviceService(builder.Configuration);

builder
    .Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
        );
        options.JsonSerializerOptions.TypeInfoResolver = new DefaultJsonTypeInfoResolver
        {
            Modifiers = { OneOfTypesPropertyModifier.Modifier },
        };
    });

builder.Services.AddProblemDetails();

builder.Services.AddLorisOpenApi();

builder.Services.Configure<RouteOptions>(options =>
{
    options.LowercaseUrls = true;
});

var app = builder.Build();

// development open api and docs serving
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "v1");
    });
}

// app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
