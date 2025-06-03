# IoT Sensor Data Processor

A comprehensive .NET solution for collecting, processing, and analyzing IoT sensor data using modern cloud-scale architecture with real-time capabilities.

## 🚀 Features

- **High-Performance Data Processing**: C# Channels for concurrent sensor data streaming
- **Real-time Communication**: MQTT for IoT devices + SignalR for web clients
- **Cloud-Scale Storage**: Azure Cosmos DB for scalable data persistence
- **Anomaly Detection**: Statistical algorithms with severity-based alerting
- **Live Dashboard**: Responsive web UI with real-time charts
- **Device Simulation**: Realistic IoT sensor data generation
- **Health Monitoring**: System performance tracking and logging
- **Microservices Architecture**: Clean separation with dependency injection

## 🏗️ Architecture

```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   Worker        │    │      API        │    │   Web Client    │
│  (Data Gen)     │    │   (REST API)    │    │  (Dashboard)    │
│                 │    │                 │    │                 │
│ • IoT Simulator │    │ • Controllers   │    │ • Real-time UI  │
│ • Data Pipeline │◄──►│ • SignalR Hub   │◄──►│ • Charts        │
│ • Anomaly Det.  │    │ • Health Check  │    │ • Monitoring    │
└─────────────────┘    └─────────────────┘    └─────────────────┘
         │                       │                       │
         ▼                       ▼                       ▼
┌─────────────────────────────────────────────────────────────────┐
│                    Infrastructure Layer                        │
│                                                                 │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐           │
│  │    MQTT     │  │  Cosmos DB  │  │  Channels   │           │
│  │   Broker    │  │  Database   │  │  (Memory)   │           │
│  └─────────────┘  └─────────────┘  └─────────────┘           │
└─────────────────────────────────────────────────────────────────┘
```

## 📦 Projects

### IoTSensorDataProcessor.Core
Core domain models, interfaces, and business logic
- `SensorData` - Primary sensor reading model
- `AnomalyDetectionResult` - Anomaly detection output
- `IChannelService` - High-performance data streaming
- `ISensorDataProcessor` - Data processing pipeline

### IoTSensorDataProcessor.Infrastructure
Infrastructure implementations for external services
- `CosmosDbSensorDataRepository` - Cosmos DB data access
- `MqttSensorDataService` - MQTT client for IoT communication
- `SignalRSensorDataPublisher` - Real-time web notifications
- `SimpleAnomalyDetectionService` - Statistical anomaly detection

### IoTSensorDataProcessor.Api
ASP.NET Core Web API with real-time dashboard
- REST endpoints for sensor data CRUD
- SignalR hubs for real-time updates
- Swagger documentation
- Health check endpoints
- Responsive web dashboard

### IoTSensorDataProcessor.Worker
Background service for continuous data processing
- IoT device simulation (8 virtual sensors)
- Channel-based data processing pipeline
- System health monitoring
- MQTT data publishing

## 🛠️ Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop)
- [Azure Cosmos DB Emulator](https://docs.microsoft.com/en-us/azure/cosmos-db/local-emulator) (optional)
- [MQTT Broker](https://mosquitto.org/) (optional)

## 🚀 Quick Start

### Automated Deployment (Recommended)
```powershell
# Windows (PowerShell)
.\start.ps1

# Linux/Mac (Bash)
./start.sh
```

The startup script will:
- ✅ Check Docker availability
- 📁 Create necessary directories  
- 🔨 Build and start all services
- 🏥 Perform health checks
- 📊 Display access URLs

### Manual Setup
```bash
# 1. Clone and Build
git clone <repository-url>
cd IoTSensorDataProcessor
dotnet restore
dotnet build

# 2. Copy environment configuration
cp .env.example .env

# 3. Run with Docker Compose
docker-compose up -d
```

### Development Mode
```bash
# Start infrastructure only
docker-compose up -d cosmos-emulator mqtt-broker

# Run API locally
cd IoTSensorDataProcessor.Api
dotnet run

# Run Worker locally (in separate terminal)
cd IoTSensorDataProcessor.Worker  
dotnet run
```

### Access Applications
- **API + Dashboard**: http://localhost:5059
- **Swagger Documentation**: http://localhost:5059/swagger
- **Health Check**: http://localhost:5059/health
- **MQTT Broker**: mqtt://localhost:1883
- **Cosmos DB Emulator**: https://localhost:8081/_explorer/index.html

## 🐳 Docker Deployment

### Using Docker Compose (Recommended)
```bash
# Start all services
docker-compose up -d

# View logs
docker-compose logs -f

# Stop all services
docker-compose down

# Clean up (remove volumes)
docker-compose down -v
```

### Manual Docker Commands
```bash
# Build images
docker build -t iot-sensor-api -f IoTSensorDataProcessor.Api/Dockerfile .
docker build -t iot-sensor-worker -f IoTSensorDataProcessor.Worker/Dockerfile .

# Run containers
docker run -d -p 5059:8080 --name iot-api iot-sensor-api
docker run -d --name iot-worker iot-sensor-worker
```

## ⚙️ Configuration

### API Settings (appsettings.json)
```json
{
  "ConnectionStrings": {
    "CosmosDb": "AccountEndpoint=https://localhost:8081/;AccountKey=...",
    "MQTT": "Server=localhost;Port=1883"
  },
  "CosmosDb": {
    "DatabaseName": "IoTSensorDB",
    "ContainerName": "SensorData"
  },
  "Mqtt": {
    "BrokerHost": "localhost",
    "BrokerPort": 1883,
    "Topics": ["sensors/+/data", "iot/+/telemetry"]
  }
}
```

### Worker Settings
```json
{
  "SensorGeneration": {
    "IntervalSeconds": 5,
    "DeviceCount": 8,
    "EnableAnomalies": true
  },
  "Processing": {
    "ChannelCapacity": 1000,
    "BatchSize": 10
  }
}
```

## 📊 Monitoring

### Available Endpoints
- `GET /health` - Application health status
- `GET /api/sensors` - List all sensor data
- `POST /api/sensors` - Create new sensor reading
- `GET /api/sensors/{id}` - Get specific sensor reading
- `DELETE /api/sensors/{id}` - Delete sensor reading

### Real-time Events (SignalR)
- `SensorDataReceived` - New sensor data available
- `AnomalyDetected` - Anomaly detected in sensor readings
- `SystemHealthUpdate` - System performance metrics

### Logging
Structured logging with Serilog:
- **Console**: Development debugging
- **File**: Production logs in `/logs` directory
- **Cosmos DB**: Error tracking (configurable)

## 🔧 Development

### Running Locally
```bash
# Terminal 1: Start API
cd IoTSensorDataProcessor.Api
dotnet run

# Terminal 2: Start Worker
cd IoTSensorDataProcessor.Worker
dotnet run
```

### Building for Production
```bash
dotnet publish IoTSensorDataProcessor.Api -c Release -o ./publish/api
dotnet publish IoTSensorDataProcessor.Worker -c Release -o ./publish/worker
```

### Running Tests
```bash
dotnet test
```

## 🔒 Security Considerations

### Current Implementation
- CORS enabled for development
- Health checks available
- Structured logging for audit trails

### Production Recommendations
- Implement JWT authentication
- Add API rate limiting
- Enable HTTPS only
- Configure proper CORS origins
- Use Azure Key Vault for secrets
- Implement proper input validation

## 📈 Performance

### Throughput Capabilities
- **Channels**: 10,000+ messages/second in-memory
- **MQTT**: Depends on broker configuration
- **Cosmos DB**: 400-100,000+ RU/s based on provisioning
- **SignalR**: 1,000+ concurrent connections

### Scaling Options
- **Horizontal**: Multiple worker instances
- **Vertical**: Increased container resources
- **Database**: Cosmos DB auto-scaling
- **Message Queue**: MQTT broker clustering

## 🚨 Troubleshooting

### Common Issues

**MQTT Connection Failed**
```bash
# Check if MQTT broker is running
docker ps | grep mqtt
# Restart MQTT broker
docker-compose restart mqtt-broker
```

**Cosmos DB Connection Error**
```bash
# Start Cosmos DB Emulator
docker-compose up cosmos-emulator
# Wait for emulator to be ready (2-3 minutes)
```

**Port Already in Use**
```bash
# Find process using port
netstat -ano | findstr :5059
# Kill process or change port in launchSettings.json
```

## 📚 Documentation & Scripts

### Quick Start Scripts
- `validate-environment.ps1` - Check system requirements and environment setup
- `start.ps1` - One-click deployment for Windows (PowerShell)  
- `start.sh` - One-click deployment for Linux/Mac (Bash)

### Configuration Files
- `.env.example` - Environment variables template
- `docker-compose.yml` - Complete multi-service deployment
- `DEPLOYMENT.md` - Production deployment guide for Azure/K8s

### Directory Structure
```
├── IoTSensorDataProcessor.Api/     # REST API & Web Dashboard
├── IoTSensorDataProcessor.Core/    # Domain models & interfaces  
├── IoTSensorDataProcessor.Infrastructure/ # External service implementations
├── IoTSensorDataProcessor.Worker/  # Background processing service
├── monitoring/                     # Prometheus & Grafana config
├── mqtt/                          # MQTT broker configuration
└── logs/                          # Application logs
```

## 🚨 Troubleshooting

### Environment Validation
```powershell
# Check system requirements
.\validate-environment.ps1
```

### Common Issues

**MQTT Connection Failed**
```bash
# Check if MQTT broker is running
docker ps | grep mqtt
# Restart MQTT broker
docker-compose restart mqtt-broker
```

**Cosmos DB Connection Error**
```bash
# Start Cosmos DB Emulator
docker-compose up cosmos-emulator
# Wait for emulator to be ready (2-3 minutes)
```

**Port Already in Use**
```bash
# Find process using port
netstat -ano | findstr :5059
# Kill process or change port in launchSettings.json
```

**Docker Build Issues**
```bash
# Clean Docker cache
docker system prune -a

# Rebuild images
docker-compose build --no-cache
```

## 📚 Additional Resources

- **Project Documentation**
  - [README.md](README.md) - Main project documentation
  - [DEPLOYMENT.md](DEPLOYMENT.md) - Production deployment guide
- **.NET Resources**
  - [.NET 8 Documentation](https://docs.microsoft.com/en-us/dotnet/)
  - [ASP.NET Core](https://docs.microsoft.com/en-us/aspnet/core/)
  - [SignalR Real-time](https://docs.microsoft.com/en-us/aspnet/core/signalr/)
- **Azure Resources**
  - [Azure Cosmos DB](https://docs.microsoft.com/en-us/azure/cosmos-db/)
  - [Azure Container Apps](https://docs.microsoft.com/en-us/azure/container-apps/)
- **IoT & Messaging**
  - [MQTT Protocol](https://mqtt.org/)
  - [Eclipse Mosquitto](https://mosquitto.org/)
- **DevOps & Monitoring**
  - [Docker Documentation](https://docs.docker.com/)
  - [Prometheus Monitoring](https://prometheus.io/docs/)
  - [Grafana Dashboards](https://grafana.com/docs/)

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests if applicable
5. Submit a pull request

## 📄 License

This project is licensed under the MIT License - see the LICENSE file for details.

## 🏷️ Version

**Current Version**: 1.0.0
**Target Framework**: .NET 8.0
**Last Updated**: June 2025
