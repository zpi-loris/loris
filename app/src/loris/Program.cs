using Loris.Automations;
using Loris.Automations.Actions;
using Loris.Automations.Conditions;
using Loris.Automations.Triggers;
using Loris.Data;
using Loris.Extensions;
using Loris.MQTT.Client;
using Loris.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Configure channels for the ingestion subsystem
builder.Services.AddChannels();

builder.Services.AddSingleton<AutomationIndex>();

// Add chirpstack integration background task with handler implementation
builder.Services.AddChirpstackIntegration<ChirpstackIntegrationHandler>(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddProblemDetails();

builder.Services.AddAutomationsCompilers();

// TODO: create extension method
// builder.Services.AddKeyedSingleton<ITriggerCompiler, TestValidTriggerCompiler>(
//     TestValidTriggerDto.Discriminator
// );
// builder.Services.AddKeyedSingleton<IConditionCompiler, TestValidConditionCompiler>(
//     TestValidConditionDto.Discriminator
// );
// builder.Services.AddKeyedSingleton<IActionCompiler, ExampleActionCompiler>(
//     ExampleActionDto.Discriminator
// );
// builder.Services.AddKeyedSingleton<ITriggerCompiler, TestInvalidTriggerCompiler>(
//     TestInvalidTriggerDto.Discriminator
// );
// builder.Services.AddKeyedSingleton<IConditionCompiler, TestInvalidConditionCompiler>(
//     TestInvalidConditionDto.Discriminator
// );
// builder.Services.AddKeyedSingleton<IActionCompiler, TestInvalidActionCompiler>(
//     TestInvalidActionDto.Discriminator
// );

builder.Services.AddScoped<IAutomationService, AutomationService>();

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
