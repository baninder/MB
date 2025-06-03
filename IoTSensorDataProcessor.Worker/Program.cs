using IoTSensorDataProcessor.Core.Interfaces;
using IoTSensorDataProcessor.Core.Models;
using IoTSensorDataProcessor.Core.Services;
using IoTSensorDataProcessor.Infrastructure.Repositories;
using IoTSensorDataProcessor.Infrastructure.Services;
using IoTSensorDataProcessor.Worker.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/worker-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Services.AddSerilog();

// Register core services
builder.Services.AddSingleton<IChannelService, ChannelService>();
builder.Services.AddSingleton<ISensorDataProcessor, SensorDataProcessor>();

// Register infrastructure services
builder.Services.AddSingleton<IMqttSensorDataService, MqttSensorDataService>();
builder.Services.AddSingleton<IAnomalyDetectionService, SimpleAnomalyDetectionService>();

// Configure Cosmos DB
builder.Services.AddSingleton(provider =>
{
    var configuration = provider.GetRequiredService<IConfiguration>();
    var connectionString = configuration.GetConnectionString("CosmosDb") ?? 
        "AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";
    return new Microsoft.Azure.Cosmos.CosmosClient(connectionString);
});
builder.Services.AddSingleton<ISensorDataRepository, CosmosDbSensorDataRepository>();
builder.Services.AddSingleton<ISensorDataPublisher, ConsoleSensorDataPublisher>();

// Register worker services
builder.Services.AddHostedService<SensorDataGeneratorService>();
builder.Services.AddHostedService<SensorDataProcessingService>();
builder.Services.AddHostedService<SystemHealthMonitorService>();

var host = builder.Build();

try
{
    Log.Information("Starting IoT Sensor Data Processor Worker");
    await host.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Worker terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
