using System.Text.Json;
using System.Text.Json.Serialization;
using Loris.Channels;
using Loris.Consumers;
using Loris.Data;
using Loris.Extensions;
using Loris.MQTT.Client;
using Loris.Services;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

string? connectionString = builder.Configuration.GetConnectionString("Timescale");

// add factory dbContext. It will serve as an entry point for factory retrieval
// as well as single, scoped DbContext instance for each request.
builder.Services.AddDbContextFactory<LorisDbContext>(options =>
    options.UseNpgsql(connectionString)
);

// Global state
builder.Services.AddSingleton<IAutomationIndex, AutomationIndex>();
builder.Services.AddSingleton<IStateCache, StateCache>();

// Configure channels for the ingestion subsystem
builder.Services.AddChannels();

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
builder.Services.AddScoped<IAutomationService, AutomationService>();
builder.Services.AddScoped<IDeviceService, DeviceService>();

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
