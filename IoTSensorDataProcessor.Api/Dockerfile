# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution file
COPY IoTSensorDataProcessor.sln .

# Copy project files
COPY IoTSensorDataProcessor.Api/IoTSensorDataProcessor.Api.csproj IoTSensorDataProcessor.Api/
COPY IoTSensorDataProcessor.Core/IoTSensorDataProcessor.Core.csproj IoTSensorDataProcessor.Core/
COPY IoTSensorDataProcessor.Infrastructure/IoTSensorDataProcessor.Infrastructure.csproj IoTSensorDataProcessor.Infrastructure/

# Restore dependencies
RUN dotnet restore IoTSensorDataProcessor.Api/IoTSensorDataProcessor.Api.csproj

# Copy source code
COPY IoTSensorDataProcessor.Api/ IoTSensorDataProcessor.Api/
COPY IoTSensorDataProcessor.Core/ IoTSensorDataProcessor.Core/
COPY IoTSensorDataProcessor.Infrastructure/ IoTSensorDataProcessor.Infrastructure/

# Build the application
WORKDIR /src/IoTSensorDataProcessor.Api
RUN dotnet build -c Release -o /app/build --no-restore

# Publish the application
RUN dotnet publish -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Copy published application
COPY --from=build /app/publish .

# Create logs directory
RUN mkdir -p /app/logs

# Set environment variables
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080

# Expose port
EXPOSE 8080

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# Start the application
ENTRYPOINT ["dotnet", "IoTSensorDataProcessor.Api.dll"]
