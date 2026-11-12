using Chirpstack.Api;
using Grpc.Core;
using Loris.Automations;
using Loris.Automations.Actions;
using Loris.Automations.Conditions;
using Loris.Automations.Triggers;
using Loris.Data;
using Loris.Extensions;
using Loris.MQTT.Client;
using Loris.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

string? connectionString = builder.Configuration.GetConnectionString("Timescale");
builder.Services.AddDbContext<LorisDbContext>(options => options.UseNpgsql(connectionString));

// Configure channels for the ingestion subsystem
builder.Services.AddChannels();

builder.Services.AddSingleton<IAutomationIndex, AutomationIndex>();

builder.Services.AddSingleton<IStateCache, StateCache>();

builder.Services.AddScoped<IAutomationService, AutomationService>();

builder.Services.AddAutomationsCompilers();

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
