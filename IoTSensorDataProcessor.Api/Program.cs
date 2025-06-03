using IoTSensorDataProcessor.Core.Interfaces;
using IoTSensorDataProcessor.Core.Services;
using IoTSensorDataProcessor.Core.Extensions;
using IoTSensorDataProcessor.Infrastructure.Repositories;
using IoTSensorDataProcessor.Infrastructure.Services;
using Microsoft.Azure.Cosmos;
using MQTTnet.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/iot-sensor-processor-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add SignalR
builder.Services.AddSignalR();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Configure Azure Cosmos DB
var cosmosConnectionString = builder.Configuration.GetConnectionString("CosmosDb") ?? 
    "AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";

builder.Services.AddSingleton(serviceProvider =>
{
    return new CosmosClient(cosmosConnectionString);
});

// Register application services
builder.Services.AddScoped<ISensorDataRepository, CosmosDbSensorDataRepository>();
builder.Services.AddScoped<IAnomalyDetectionService, SimpleAnomalyDetectionService>();
builder.Services.AddScoped<ISensorDataPublisher, SignalRSensorDataPublisher>();
builder.Services.AddScoped<ISensorDataProcessor, SensorDataProcessor>();

// Register advanced design patterns and strategies
builder.Services.AddAdvancedPatterns();

// Configure strategies with custom options
builder.Services.ConfigureStrategies(options =>
{
    options.DefaultAnomalyStrategy = "statistical";
    options.AnomalyThresholds["statistical"] = 2.5;
    options.AnomalyThresholds["timeseries"] = 0.3;
    options.AnomalyThresholds["threshold"] = 1.0;
    options.DefaultSerializationFormat = "application/json";
    options.Spark.AppName = "IoTSensorDataProcessor-API";
    options.Spark.DefaultParallelism = Environment.ProcessorCount;
});

// Register MQTT service as singleton
builder.Services.AddSingleton<IMqttSensorDataService>(serviceProvider =>
{
    var mqttHost = builder.Configuration["MQTT:Host"] ?? "localhost";
    var mqttPort = int.Parse(builder.Configuration["MQTT:Port"] ?? "1883");
    var mqttUser = builder.Configuration["MQTT:Username"] ?? "";
    var mqttPassword = builder.Configuration["MQTT:Password"] ?? "";
    
    return new MqttSensorDataService(mqttHost, mqttPort, mqttUser, mqttPassword);
});

// Add hosted service for background processing
builder.Services.AddHostedService<SensorDataBackgroundService>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthorization();

app.MapControllers();
app.MapHub<SensorDataHub>("/sensorDataHub");

// Initialize Cosmos DB
await InitializeCosmosDbAsync(app.Services);

app.Run();

static async Task InitializeCosmosDbAsync(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var cosmosClient = scope.ServiceProvider.GetRequiredService<CosmosClient>();
    
    try
    {
        var database = await cosmosClient.CreateDatabaseIfNotExistsAsync("IoTSensorData");
        await database.Database.CreateContainerIfNotExistsAsync("SensorReadings", "/DeviceId");
        Log.Information("Cosmos DB initialized successfully");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Failed to initialize Cosmos DB");
    }
}
