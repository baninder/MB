# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution file
COPY IoTSensorDataProcessor.sln .

# Copy project files
COPY IoTSensorDataProcessor.Worker/IoTSensorDataProcessor.Worker.csproj IoTSensorDataProcessor.Worker/
COPY IoTSensorDataProcessor.Core/IoTSensorDataProcessor.Core.csproj IoTSensorDataProcessor.Core/
COPY IoTSensorDataProcessor.Infrastructure/IoTSensorDataProcessor.Infrastructure.csproj IoTSensorDataProcessor.Infrastructure/

# Restore dependencies
RUN dotnet restore IoTSensorDataProcessor.Worker/IoTSensorDataProcessor.Worker.csproj

# Copy source code
COPY IoTSensorDataProcessor.Worker/ IoTSensorDataProcessor.Worker/
COPY IoTSensorDataProcessor.Core/ IoTSensorDataProcessor.Core/
COPY IoTSensorDataProcessor.Infrastructure/ IoTSensorDataProcessor.Infrastructure/

# Build the application
WORKDIR /src/IoTSensorDataProcessor.Worker
RUN dotnet build -c Release -o /app/build --no-restore

# Publish the application
RUN dotnet publish -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS runtime
WORKDIR /app

# Copy published application
COPY --from=build /app/publish .

# Create logs directory
RUN mkdir -p /app/logs

# Set environment variables
ENV DOTNET_ENVIRONMENT=Production

# Start the application
ENTRYPOINT ["dotnet", "IoTSensorDataProcessor.Worker.dll"]
