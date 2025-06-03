# IoT Sensor Data Processor - Deployment Guide

## 🚀 Production Deployment

### Azure Cloud Deployment

#### Prerequisites
- Azure subscription
- Azure CLI installed
- Docker Desktop
- .NET 8 SDK

#### 1. Azure Resources Setup
```bash
# Login to Azure
az login

# Create resource group
az group create --name iot-sensor-rg --location eastus

# Create Cosmos DB account
az cosmosdb create --name iot-sensor-cosmos --resource-group iot-sensor-rg --kind GlobalDocumentDB

# Create Container Registry
az acr create --name iotsensoracr --resource-group iot-sensor-rg --sku Basic --admin-enabled true

# Create Container Apps Environment
az containerapp env create --name iot-sensor-env --resource-group iot-sensor-rg --location eastus
```

#### 2. Build and Push Images
```bash
# Get ACR login server
ACR_LOGIN_SERVER=$(az acr show --name iotsensoracr --resource-group iot-sensor-rg --query loginServer --output tsv)

# Login to ACR
az acr login --name iotsensoracr

# Build and push API image
docker build -t $ACR_LOGIN_SERVER/iot-sensor-api:latest -f IoTSensorDataProcessor.Api/Dockerfile .
docker push $ACR_LOGIN_SERVER/iot-sensor-api:latest

# Build and push Worker image
docker build -t $ACR_LOGIN_SERVER/iot-sensor-worker:latest -f IoTSensorDataProcessor.Worker/Dockerfile .
docker push $ACR_LOGIN_SERVER/iot-sensor-worker:latest
```

#### 3. Deploy Container Apps
```bash
# Deploy API Container App
az containerapp create \
  --name iot-sensor-api \
  --resource-group iot-sensor-rg \
  --environment iot-sensor-env \
  --image $ACR_LOGIN_SERVER/iot-sensor-api:latest \
  --target-port 8080 \
  --ingress external \
  --registry-server $ACR_LOGIN_SERVER \
  --env-vars "ASPNETCORE_ENVIRONMENT=Production" "ConnectionStrings__CosmosDb=<cosmos-connection-string>"

# Deploy Worker Container App  
az containerapp create \
  --name iot-sensor-worker \
  --resource-group iot-sensor-rg \
  --environment iot-sensor-env \
  --image $ACR_LOGIN_SERVER/iot-sensor-worker:latest \
  --registry-server $ACR_LOGIN_SERVER \
  --env-vars "DOTNET_ENVIRONMENT=Production" "ConnectionStrings__CosmosDb=<cosmos-connection-string>"
```

## 🐳 Docker Swarm Deployment

### 1. Initialize Swarm
```bash
docker swarm init
```

### 2. Deploy Stack
```bash
# Create production Docker Compose override
cp docker-compose.yml docker-compose.prod.yml

# Deploy to swarm
docker stack deploy -c docker-compose.prod.yml iot-sensor-stack
```

### 3. Scale Services
```bash
# Scale worker instances
docker service scale iot-sensor-stack_iot-sensor-worker=3

# Scale API instances
docker service scale iot-sensor-stack_iot-sensor-api=2
```

## ☸️ Kubernetes Deployment

### 1. Create Kubernetes Manifests
```yaml
# k8s/namespace.yaml
apiVersion: v1
kind: Namespace
metadata:
  name: iot-sensor-system
```

### 2. Deploy to Kubernetes
```bash
# Apply manifests
kubectl apply -f k8s/

# Check deployment status
kubectl get pods -n iot-sensor-system

# Get service URLs
kubectl get services -n iot-sensor-system
```

## 🔧 Configuration Management

### Environment Variables
```bash
# Production environment file (.env.prod)
ASPNETCORE_ENVIRONMENT=Production
COSMOS_DB_ENDPOINT=https://your-cosmos-account.documents.azure.com:443/
COSMOS_DB_KEY=your-primary-key
MQTT_BROKER_HOST=your-mqtt-broker.com
REDIS_CONNECTION_STRING=your-redis-connection-string
```

### Secrets Management
```bash
# Azure Key Vault
az keyvault create --name iot-sensor-kv --resource-group iot-sensor-rg --location eastus

# Store secrets
az keyvault secret set --vault-name iot-sensor-kv --name CosmosDbKey --value "your-cosmos-key"
az keyvault secret set --vault-name iot-sensor-kv --name MqttPassword --value "your-mqtt-password"
```

## 📊 Monitoring Setup

### Application Insights
```bash
# Create Application Insights
az monitor app-insights component create \
  --app iot-sensor-insights \
  --location eastus \
  --resource-group iot-sensor-rg
```

### Prometheus & Grafana
```bash
# Deploy monitoring stack
helm repo add prometheus-community https://prometheus-community.github.io/helm-charts
helm repo add grafana https://grafana.github.io/helm-charts

helm install prometheus prometheus-community/kube-prometheus-stack -n monitoring --create-namespace
helm install grafana grafana/grafana -n monitoring
```

## 🔒 Security Configuration

### SSL/TLS Certificates
```bash
# Let's Encrypt with cert-manager
kubectl apply -f https://github.com/cert-manager/cert-manager/releases/download/v1.13.0/cert-manager.yaml

# Configure ClusterIssuer
kubectl apply -f k8s/cluster-issuer.yaml
```

### Network Policies
```yaml
# k8s/network-policy.yaml
apiVersion: networking.k8s.io/v1
kind: NetworkPolicy
metadata:
  name: iot-sensor-network-policy
  namespace: iot-sensor-system
spec:
  podSelector: {}
  policyTypes:
  - Ingress
  - Egress
```

## 📈 Performance Tuning

### Resource Limits
```yaml
# k8s/api-deployment.yaml
resources:
  requests:
    memory: "256Mi"
    cpu: "250m"
  limits:
    memory: "512Mi"
    cpu: "500m"
```

### Auto-scaling
```bash
# Horizontal Pod Autoscaler
kubectl autoscale deployment iot-sensor-api --cpu-percent=70 --min=2 --max=10 -n iot-sensor-system
```

## 🚨 Disaster Recovery

### Backup Strategy
```bash
# Cosmos DB automatic backups are enabled by default
# For additional point-in-time restore:
az cosmosdb sql database restore \
  --account-name iot-sensor-cosmos \
  --resource-group iot-sensor-rg \
  --name IoTSensorData \
  --restore-timestamp "2025-06-02T10:00:00Z"
```

### Multi-region Deployment
```bash
# Add read regions to Cosmos DB
az cosmosdb update \
  --name iot-sensor-cosmos \
  --resource-group iot-sensor-rg \
  --locations regionName=eastus failoverPriority=0 isZoneRedundant=false \
  --locations regionName=westus failoverPriority=1 isZoneRedundant=false
```

## 🔍 Troubleshooting

### Common Issues

**Container Startup Failures**
```bash
# Check container logs
docker logs <container-id>
kubectl logs <pod-name> -n iot-sensor-system

# Check resource usage
docker stats
kubectl top pods -n iot-sensor-system
```

**Database Connection Issues**
```bash
# Test Cosmos DB connectivity
curl -k "https://your-cosmos-account.documents.azure.com:443/"

# Check firewall settings
az cosmosdb firewall-rule list --account-name iot-sensor-cosmos --resource-group iot-sensor-rg
```

**MQTT Connection Problems**
```bash
# Test MQTT broker
mosquitto_sub -h your-mqtt-broker.com -t "sensors/+/data"

# Check broker logs
docker logs <mqtt-broker-container>
```

### Health Check Endpoints
- API Health: `https://your-api-url/health`
- Worker Status: Check container logs
- Database: Cosmos DB portal metrics
- MQTT: Broker connection logs

## 📋 Maintenance Tasks

### Regular Updates
```bash
# Update container images
docker pull mcr.microsoft.com/dotnet/aspnet:8.0
docker pull mcr.microsoft.com/dotnet/runtime:8.0

# Rebuild and redeploy
docker-compose build --no-cache
docker-compose up -d
```

### Database Maintenance
```bash
# Monitor Cosmos DB metrics
az monitor metrics list --resource /subscriptions/<sub>/resourceGroups/iot-sensor-rg/providers/Microsoft.DocumentDB/databaseAccounts/iot-sensor-cosmos

# Optimize indexing policies
# Review and update through Azure portal
```

### Log Management
```bash
# Rotate log files
find ./logs -name "*.txt" -mtime +30 -delete

# Archive old logs to Azure Storage
az storage blob upload-batch --destination logs --source ./logs --account-name <storage-account>
```
