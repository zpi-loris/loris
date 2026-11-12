using Loris.Channels;
using Loris.Consumers;
using Loris.Data;
using Loris.Extensions;
using Loris.MQTT.Client;
using Loris.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

string? connectionString = builder.Configuration.GetConnectionString("Timescale");
builder.Services.AddDbContext<LorisDbContext>(options => options.UseNpgsql(connectionString));

// Global state
builder.Services.AddSingleton<IAutomationIndex, AutomationIndex>();
builder.Services.AddSingleton<IStateCache, StateCache>();

// Configure channels for the ingestion subsystem
builder.Services.AddChannels();

// Configure consumers for each channel
builder.Services.AddHostedService<DeviceChannelConsumer>();

// builder.Services.AddHostedService<EventChannelConsumer>();
// builder.Services.AddHostedService<DbWriteChannelConsumer>();

// Configure handlers for each channel consumer
builder.Services.AddSingleton<IDeviceChannelHandler, DeviceChannelHandler>();

builder.Services.AddAutomationsCompilers();

builder.Services.AddScoped<IAutomationService, AutomationService>();

// Add chirpstack integration background task with handler implementation
builder.Services.AddChirpstackIntegration<ChirpstackIntegrationHandler>(builder.Configuration);

// Add chirpstack grpc device service client factory
builder.Services.AddChirpstackDeviceService(builder.Configuration);

builder.Services.AddControllers();

builder.Services.AddProblemDetails();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// app.UseExceptionHandler();
// app.UseStatusCodePages();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwaggerUi(options =>
    {
        options.DocumentPath = "/openapi/v1.json";
    });
    app.MapOpenApi();
}

// app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
