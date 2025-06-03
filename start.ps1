# IoT Sensor Data Processor - Quick Start Script (PowerShell)

Write-Host "🚀 Starting IoT Sensor Data Processor Solution..." -ForegroundColor Green
Write-Host "==============================================" -ForegroundColor Green

# Check if Docker is running
try {
    docker info | Out-Null
    Write-Host "✅ Docker is running" -ForegroundColor Green
} catch {
    Write-Host "❌ Docker is not running. Please start Docker Desktop first." -ForegroundColor Red
    exit 1
}

# Check if Docker Compose is available
if (!(Get-Command docker-compose -ErrorAction SilentlyContinue)) {
    Write-Host "❌ Docker Compose is not installed. Please install Docker Compose." -ForegroundColor Red
    exit 1
}

# Create necessary directories
Write-Host "📁 Creating necessary directories..." -ForegroundColor Yellow
New-Item -ItemType Directory -Force -Path "logs/api", "logs/worker", "mqtt/data", "mqtt/logs" | Out-Null

# Stop any existing containers
Write-Host "🛑 Stopping existing containers..." -ForegroundColor Yellow
docker-compose down

# Pull latest images
Write-Host "📥 Pulling latest Docker images..." -ForegroundColor Yellow
docker-compose pull

# Build and start services
Write-Host "🔨 Building and starting services..." -ForegroundColor Yellow
docker-compose up --build -d

# Wait for services to be ready
Write-Host "⏳ Waiting for services to start..." -ForegroundColor Yellow
Start-Sleep -Seconds 30

# Check service health
Write-Host "🏥 Checking service health..." -ForegroundColor Yellow
Write-Host ""

# Check API health
try {
    $response = Invoke-WebRequest -Uri "http://localhost:5059/health" -UseBasicParsing -TimeoutSec 5
    if ($response.StatusCode -eq 200) {
        Write-Host "✅ API Service: http://localhost:5059" -ForegroundColor Green
        Write-Host "✅ Web Dashboard: http://localhost:5059" -ForegroundColor Green
    }
} catch {
    Write-Host "❌ API Service: Not responding" -ForegroundColor Red
}

# Check Cosmos DB Emulator
try {
    $response = Invoke-WebRequest -Uri "https://localhost:8081/_explorer/index.html" -UseBasicParsing -TimeoutSec 5 -SkipCertificateCheck
    if ($response.StatusCode -eq 200) {
        Write-Host "✅ Cosmos DB Emulator: https://localhost:8081" -ForegroundColor Green
    }
} catch {
    Write-Host "❌ Cosmos DB Emulator: Not responding" -ForegroundColor Red
}

# Check MQTT Broker
try {
    $tcpClient = New-Object System.Net.Sockets.TcpClient
    $tcpClient.Connect("localhost", 1883)
    $tcpClient.Close()
    Write-Host "✅ MQTT Broker: localhost:1883" -ForegroundColor Green
} catch {
    Write-Host "❌ MQTT Broker: Not responding" -ForegroundColor Red
}

# Check Prometheus
try {
    $response = Invoke-WebRequest -Uri "http://localhost:9090" -UseBasicParsing -TimeoutSec 5
    if ($response.StatusCode -eq 200) {
        Write-Host "✅ Prometheus: http://localhost:9090" -ForegroundColor Green
    }
} catch {
    Write-Host "❌ Prometheus: Not responding" -ForegroundColor Red
}

# Check Grafana
try {
    $response = Invoke-WebRequest -Uri "http://localhost:3000" -UseBasicParsing -TimeoutSec 5
    if ($response.StatusCode -eq 200) {
        Write-Host "✅ Grafana: http://localhost:3000 (admin/admin123)" -ForegroundColor Green
    }
} catch {
    Write-Host "❌ Grafana: Not responding" -ForegroundColor Red
}

Write-Host ""
Write-Host "🎉 IoT Sensor Data Processor is running!" -ForegroundColor Green
Write-Host ""
Write-Host "📊 Access Points:" -ForegroundColor Cyan
Write-Host "  • Web Dashboard: http://localhost:5059" -ForegroundColor White
Write-Host "  • API Swagger: http://localhost:5059/swagger" -ForegroundColor White
Write-Host "  • Cosmos DB Explorer: https://localhost:8081" -ForegroundColor White
Write-Host "  • Prometheus: http://localhost:9090" -ForegroundColor White
Write-Host "  • Grafana: http://localhost:3000" -ForegroundColor White
Write-Host ""
Write-Host "📋 To view logs:" -ForegroundColor Cyan
Write-Host "  • docker-compose logs -f iot-sensor-api" -ForegroundColor White
Write-Host "  • docker-compose logs -f iot-sensor-worker" -ForegroundColor White
Write-Host ""
Write-Host "🛑 To stop all services:" -ForegroundColor Cyan
Write-Host "  • docker-compose down" -ForegroundColor White
