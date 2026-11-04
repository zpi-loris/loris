using Loris.Automations.Actions;
using Loris.Automations.Actions.Test;
using Loris.Automations.Conditions;
using Loris.Automations.Conditions.Test;
using Loris.Automations.Triggers;
using Loris.Automations.Triggers.Test;
using Loris.Extensions;
using Loris.MQTT.Client;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Configure channels for the ingestion subsystem
builder.Services.AddChannels();

// Add chirpstack integration background task with handler implementation
builder.Services.AddChirpstackIntegration<ChirpstackIntegrationHandler>(builder.Configuration);

builder.Services.AddControllers();

// TODO: create extension method
builder.Services.AddKeyedSingleton<ITriggerService, TestTriggerService>(
    TestTriggerDto.Discriminator
);
builder.Services.AddKeyedSingleton<IConditionService, TestConditionService>(
    TestConditionDto.Discriminator
);
builder.Services.AddKeyedSingleton<IActionService, TestActionService>(TestActionDto.Discriminator);

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

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
