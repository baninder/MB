#!/bin/bash

# IoT Sensor Data Processor - Quick Start Script

echo "🚀 Starting IoT Sensor Data Processor Solution..."
echo "=============================================="

# Check if Docker is running
if ! docker info > /dev/null 2>&1; then
    echo "❌ Docker is not running. Please start Docker Desktop first."
    exit 1
fi

# Check if Docker Compose is available
if ! command -v docker-compose &> /dev/null; then
    echo "❌ Docker Compose is not installed. Please install Docker Compose."
    exit 1
fi

echo "✅ Docker is running"

# Create necessary directories
echo "📁 Creating necessary directories..."
mkdir -p logs/api logs/worker mqtt/data mqtt/logs

# Stop any existing containers
echo "🛑 Stopping existing containers..."
docker-compose down

# Pull latest images
echo "📥 Pulling latest Docker images..."
docker-compose pull

# Build and start services
echo "🔨 Building and starting services..."
docker-compose up --build -d

# Wait for services to be ready
echo "⏳ Waiting for services to start..."
sleep 30

# Check service health
echo "🏥 Checking service health..."
echo ""

# Check API health
if curl -f http://localhost:5059/health > /dev/null 2>&1; then
    echo "✅ API Service: http://localhost:5059"
    echo "✅ Web Dashboard: http://localhost:5059"
else
    echo "❌ API Service: Not responding"
fi

# Check Cosmos DB Emulator
if curl -k -f https://localhost:8081/_explorer/index.html > /dev/null 2>&1; then
    echo "✅ Cosmos DB Emulator: https://localhost:8081"
else
    echo "❌ Cosmos DB Emulator: Not responding"
fi

# Check MQTT Broker
if nc -z localhost 1883 > /dev/null 2>&1; then
    echo "✅ MQTT Broker: localhost:1883"
else
    echo "❌ MQTT Broker: Not responding"
fi

# Check Prometheus
if curl -f http://localhost:9090 > /dev/null 2>&1; then
    echo "✅ Prometheus: http://localhost:9090"
else
    echo "❌ Prometheus: Not responding"
fi

# Check Grafana
if curl -f http://localhost:3000 > /dev/null 2>&1; then
    echo "✅ Grafana: http://localhost:3000 (admin/admin123)"
else
    echo "❌ Grafana: Not responding"
fi

echo ""
echo "🎉 IoT Sensor Data Processor is running!"
echo ""
echo "📊 Access Points:"
echo "  • Web Dashboard: http://localhost:5059"
echo "  • API Swagger: http://localhost:5059/swagger"
echo "  • Cosmos DB Explorer: https://localhost:8081"
echo "  • Prometheus: http://localhost:9090"
echo "  • Grafana: http://localhost:3000"
echo ""
echo "📋 To view logs:"
echo "  • docker-compose logs -f iot-sensor-api"
echo "  • docker-compose logs -f iot-sensor-worker"
echo ""
echo "🛑 To stop all services:"
echo "  • docker-compose down"
