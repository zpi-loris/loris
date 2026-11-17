using Loris.Channels;
using Loris.Consumers;
using Loris.Data;
using Loris.Extensions;
using Loris.Models;
using Loris.MQTT.Client;
using Loris.Services;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

string? connectionString = builder.Configuration.GetConnectionString("Timescale");

// add factory first because it's singleton, thus options will inherit singleton
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

builder.Services.AddAutomationsCompilers();

builder.Services.AddScoped<IAutomationService, AutomationService>();
builder.Services.AddScoped<IDeviceService, DeviceService>();

// Add chirpstack integration background task with handler implementation
builder.Services.AddChirpstackIntegration<ChirpstackIntegrationHandler>(builder.Configuration);

// Add chirpstack grpc device service client factory
builder.Services.AddChirpstackDeviceService(builder.Configuration);

builder.Services.AddControllers();

builder.Services.AddProblemDetails();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddLorisOpenApi();

builder.Services.Configure<RouteOptions>(options =>
{
    options.LowercaseUrls = true;
});

var app = builder.Build();

// app.UseExceptionHandler();
// app.UseStatusCodePages();

// Configure the HTTP request pipeline.
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
