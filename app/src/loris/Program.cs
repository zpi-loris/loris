using System.Threading.Channels;
using app.Messages;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddSingleton(deviceChannel => Channel.CreateUnbounded<DeviceMessage>());
builder.Services.AddSingleton(eventChannel => Channel.CreateUnbounded<BaseEventMessage>());
builder.Services.AddSingleton(dbWriteChannel => Channel.CreateUnbounded<IDbWriteMessage>());

builder.Services.AddControllers();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
