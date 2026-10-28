using Loris.Extensions;
using Loris.MQTT.Client;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Configure channels for the ingestion subsystem
builder.Services.AddChannels();

builder.Services.AddHostedService<LoraWanServerMqttClient>();

builder.Services.AddControllers();

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
