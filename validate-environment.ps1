# Environment Validation Script for IoT Sensor Data Processor

Write-Host "🔍 Validating IoT Sensor Data Processor Environment..." -ForegroundColor Green
Write-Host "=================================================" -ForegroundColor Green

$validationResults = @()

# Check .NET 8 SDK
Write-Host "`n📦 Checking .NET 8 SDK..." -ForegroundColor Yellow
try {
    $dotnetVersion = dotnet --version
    if ($dotnetVersion -like "8.*") {
        Write-Host "✅ .NET 8 SDK found: $dotnetVersion" -ForegroundColor Green
        $validationResults += @{Component=".NET 8 SDK"; Status="✅ Installed"; Version=$dotnetVersion}
    } else {
        Write-Host "❌ .NET 8 SDK not found. Current version: $dotnetVersion" -ForegroundColor Red
        $validationResults += @{Component=".NET 8 SDK"; Status="❌ Wrong Version"; Version=$dotnetVersion}
    }
} catch {
    Write-Host "❌ .NET SDK not found. Please install .NET 8 SDK" -ForegroundColor Red
    $validationResults += @{Component=".NET 8 SDK"; Status="❌ Not Installed"; Version="N/A"}
}

# Check Docker Desktop
Write-Host "`n🐳 Checking Docker..." -ForegroundColor Yellow
try {
    $dockerVersion = docker --version
    docker info | Out-Null
    Write-Host "✅ Docker is running: $dockerVersion" -ForegroundColor Green
    $validationResults += @{Component="Docker"; Status="✅ Running"; Version=$dockerVersion}
} catch {
    Write-Host "❌ Docker is not running or not installed" -ForegroundColor Red
    $validationResults += @{Component="Docker"; Status="❌ Not Running"; Version="N/A"}
}

# Check Docker Compose
Write-Host "`n📋 Checking Docker Compose..." -ForegroundColor Yellow
try {
    $composeVersion = docker-compose --version
    Write-Host "✅ Docker Compose found: $composeVersion" -ForegroundColor Green
    $validationResults += @{Component="Docker Compose"; Status="✅ Installed"; Version=$composeVersion}
} catch {
    Write-Host "❌ Docker Compose not found" -ForegroundColor Red
    $validationResults += @{Component="Docker Compose"; Status="❌ Not Installed"; Version="N/A"}
}

# Check PowerShell version
Write-Host "`n⚡ Checking PowerShell..." -ForegroundColor Yellow
$psVersion = $PSVersionTable.PSVersion
if ($psVersion.Major -ge 5) {
    Write-Host "✅ PowerShell version: $($psVersion.ToString())" -ForegroundColor Green
    $validationResults += @{Component="PowerShell"; Status="✅ Compatible"; Version=$psVersion.ToString()}
} else {
    Write-Host "❌ PowerShell version too old: $($psVersion.ToString())" -ForegroundColor Red
    $validationResults += @{Component="PowerShell"; Status="❌ Outdated"; Version=$psVersion.ToString()}
}

# Check available ports
Write-Host "`n🔌 Checking required ports..." -ForegroundColor Yellow
$requiredPorts = @(5059, 8081, 1883, 9090, 3000, 6379)
$portIssues = @()

foreach ($port in $requiredPorts) {
    try {
        $connection = Test-NetConnection -ComputerName "localhost" -Port $port -InformationLevel Quiet -WarningAction SilentlyContinue
        if ($connection) {
            Write-Host "⚠️  Port $port is already in use" -ForegroundColor Yellow
            $portIssues += $port
        } else {
            Write-Host "✅ Port $port is available" -ForegroundColor Green
        }
    } catch {
        Write-Host "✅ Port $port is available" -ForegroundColor Green
    }
}

if ($portIssues.Count -eq 0) {
    $validationResults += @{Component="Required Ports"; Status="✅ All Available"; Version="5059,8081,1883,9090,3000,6379"}
} else {
    $validationResults += @{Component="Required Ports"; Status="⚠️  Some Busy"; Version="Busy: $($portIssues -join ',')"}
}

# Check project files
Write-Host "`n📁 Checking project structure..." -ForegroundColor Yellow
$requiredFiles = @(
    "IoTSensorDataProcessor.sln",
    "docker-compose.yml",
    "IoTSensorDataProcessor.Api\Dockerfile",
    "IoTSensorDataProcessor.Worker\Dockerfile",
    "monitoring\prometheus.yml",
    "mqtt\config\mosquitto.conf"
)

$missingFiles = @()
foreach ($file in $requiredFiles) {
    if (Test-Path $file) {
        Write-Host "✅ Found: $file" -ForegroundColor Green
    } else {
        Write-Host "❌ Missing: $file" -ForegroundColor Red
        $missingFiles += $file
    }
}

if ($missingFiles.Count -eq 0) {
    $validationResults += @{Component="Project Files"; Status="✅ Complete"; Version="All files present"}
} else {
    $validationResults += @{Component="Project Files"; Status="❌ Incomplete"; Version="Missing: $($missingFiles.Count)"}
}

# Memory check
Write-Host "`n💾 Checking system resources..." -ForegroundColor Yellow
$memory = Get-CimInstance -ClassName Win32_ComputerSystem
$totalMemoryGB = [math]::Round($memory.TotalPhysicalMemory / 1GB, 2)

if ($totalMemoryGB -ge 8) {
    Write-Host "✅ Memory: $totalMemoryGB GB (Recommended: 8GB+)" -ForegroundColor Green
    $validationResults += @{Component="System Memory"; Status="✅ Sufficient"; Version="$totalMemoryGB GB"}
} elseif ($totalMemoryGB -ge 4) {
    Write-Host "⚠️  Memory: $totalMemoryGB GB (Minimum met, recommended: 8GB+)" -ForegroundColor Yellow
    $validationResults += @{Component="System Memory"; Status="⚠️  Minimum"; Version="$totalMemoryGB GB"}
} else {
    Write-Host "❌ Memory: $totalMemoryGB GB (Insufficient, minimum: 4GB)" -ForegroundColor Red
    $validationResults += @{Component="System Memory"; Status="❌ Insufficient"; Version="$totalMemoryGB GB"}
}

# Disk space check
$diskSpace = Get-CimInstance -ClassName Win32_LogicalDisk | Where-Object {$_.DeviceID -eq "C:"}
$freeSpaceGB = [math]::Round($diskSpace.FreeSpace / 1GB, 2)

if ($freeSpaceGB -ge 10) {
    Write-Host "✅ Free disk space: $freeSpaceGB GB" -ForegroundColor Green
    $validationResults += @{Component="Disk Space"; Status="✅ Sufficient"; Version="$freeSpaceGB GB free"}
} else {
    Write-Host "❌ Free disk space: $freeSpaceGB GB (Need at least 10GB)" -ForegroundColor Red
    $validationResults += @{Component="Disk Space"; Status="❌ Insufficient"; Version="$freeSpaceGB GB free"}
}

# Summary
Write-Host "`n📊 Validation Summary:" -ForegroundColor Cyan
Write-Host "=====================" -ForegroundColor Cyan

$successCount = ($validationResults | Where-Object {$_.Status -like "*✅*"}).Count
$warningCount = ($validationResults | Where-Object {$_.Status -like "*⚠️*"}).Count
$errorCount = ($validationResults | Where-Object {$_.Status -like "*❌*"}).Count

foreach ($result in $validationResults) {
    $color = if ($result.Status -like "*✅*") { "Green" } 
             elseif ($result.Status -like "*⚠️*") { "Yellow" }
             else { "Red" }
    
    Write-Host "  $($result.Component): $($result.Status)" -ForegroundColor $color
}

Write-Host "`n🎯 Results: $successCount Success, $warningCount Warnings, $errorCount Errors" -ForegroundColor White

if ($errorCount -eq 0 -and $warningCount -eq 0) {
    Write-Host "`n🎉 Environment is ready! You can run .\start.ps1 to deploy the solution." -ForegroundColor Green
} elseif ($errorCount -eq 0) {
    Write-Host "`n⚠️  Environment has warnings but should work. Consider addressing them." -ForegroundColor Yellow
    Write-Host "You can try running .\start.ps1 to deploy the solution." -ForegroundColor Yellow
} else {
    Write-Host "`n❌ Environment has errors that need to be fixed before deployment." -ForegroundColor Red
    Write-Host "Please install missing components and run this script again." -ForegroundColor Red
}

Write-Host "`n📚 For help, see README.md or DEPLOYMENT.md" -ForegroundColor Cyan
