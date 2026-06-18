$ErrorActionPreference = "Stop"

$serverIp = "85.198.87.45"
$serverPath = "/var/www/showdanwebapi"
$serviceName = "showdanwebapi.service"
$projectPath = "ShowDanWebApi.API/ShowDanWebApi.API.csproj"

# Функция для записи строго в UTF-8 БЕЗ BOM (чтобы Linux и Postgres не ругались)
function Write-Utf8WithoutBom ($path, $content) {
    $utf8WithoutBom = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllLines($path, $content, $utf8WithoutBom)
}

try {
    Write-Host "--- 1. Building Self-Contained ZIP (Linux-x64)... ---" -ForegroundColor Cyan
    
    # Полная очистка перед билдом
    if (Test-Path "./publish") { Remove-Item -Recurse -Force ./publish }
    if (Test-Path "./publish.zip") { Remove-Item "./publish.zip" }

    dotnet publish $projectPath `
        -c Release `
        -o ./publish `
        -r linux-x64 `
        --self-contained true `
        /p:UseAppHost=true `
        /p:PublishSingleFile=false

    # Проверка билда
    if (!(Test-Path "./publish/")) { throw "Build failed: publish directory not found." }

    Write-Host "--- 1.1 Archiving... ---" -ForegroundColor Gray
    & "C:\Program Files\7-Zip\7z.exe" a -tzip ./publish.zip ./publish/*

    Write-Host "--- 2. Generating Deployment Files... ---" -ForegroundColor Cyan
    
    # 2.1 Файл конфигурации сервиса .NET
    $serviceContent = @"
[Unit]
Description=.NET Web API App running on Linux
After=network.target

[Service]
WorkingDirectory=$serverPath
ExecStart=$serverPath/ShowDanWebApi.API
Restart=always
RestartSec=10
Environment=ASPNETCORE_URLS=http://0.0.0.0:5123
KillSignal=SIGINT
SyslogIdentifier=dotnet-example
User=root
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false

# РАСПРЕДЕЛЕНИЕ НАГРУЗКИ ПОД 4ГБ ОЗУ (ВЫЖИМАЕМ МАКСИМУМ):
Environment=DOTNET_GCHeapHardLimit=1073741824
Environment=DOTNET_GCLatencyMode=Interactive
Environment=DOTNET_GCWorkstation=1
MemoryHigh=1100M
MemoryMax=1300M

[Install]
WantedBy=multi-user.target
"@
    Write-Utf8WithoutBom "./$serviceName" $serviceContent

    # 2.2 Чистый postgresql.conf БЕЗ BOM
    $postgresConfig = @"
max_connections = 100
shared_buffers = 768MB
effective_cache_size = 2GB
work_mem = 8MB
maintenance_work_mem = 128MB
min_wal_size = 256MB
max_wal_size = 2GB
checkpoint_completion_target = 0.9
checkpoint_timeout = 15min
synchronous_commit = off
listen_addresses = '*'
"@
    Write-Utf8WithoutBom "./postgresql.conf" $postgresConfig

    # 2.3 Чистый Bash скрипт деплоя БЕЗ BOM
    $bashScript = @"
#!/bin/bash
set -e

echo "--- 1. Stopping .NET Service ---"
systemctl stop $serviceName || true

echo "--- 2. Cleaning up old API files ---"
mkdir -p $serverPath
find $serverPath -mindepth 1 ! -name 'appsettings.json' ! -name 'appsettings.Production.json' -delete

echo "--- 3. Extracting new API files ---"
unzip -o /tmp/publish.zip -d $serverPath
chmod +x $serverPath/ShowDanWebApi.API

echo "--- 4. Updating .NET Service Config ---"
mv /tmp/$serviceName /etc/systemd/system/$serviceName

echo "--- 5. Optimizing Ollama Memory Retention ---"
if [ -f /etc/systemd/system/ollama.service ] || systemctl list-unit-files | grep -q 'ollama.service'; then
    mkdir -p /etc/systemd/system/ollama.service.d
    echo '[Service]' > /etc/systemd/system/ollama.service.d/override.conf
    echo 'Environment="OLLAMA_KEEP_ALIVE=-1"' >> /etc/systemd/system/ollama.service.d/override.conf
    systemctl daemon-reload
    systemctl restart ollama
    echo "Ollama optimized."
else
    echo "WARNING: Ollama service not found. Skipping."
fi

echo "--- 6. Overwriting PostgreSQL Config inside Docker ---"
if docker ps -a --format '{{.Names}}' | grep -q '^showdan_db$'; then
    # На всякий случай чистим возможные Windows-переносы строк в конфиге постгреса
    sed -i 's/\r$//' /tmp/postgresql.conf
    
    # Копируем конфиг напрямую в volume контейнера
    docker cp /tmp/postgresql.conf showdan_db:/var/lib/postgresql/data/postgresql.conf
    docker restart showdan_db
    echo "PostgreSQL config replaced and container restarted."
else
    echo "WARNING: Container showdan_db not found. Skipping."
fi

echo "--- 7. Starting .NET Service ---"
systemctl daemon-reload
systemctl start $serviceName
systemctl status $serviceName --no-pager
"@
    Write-Utf8WithoutBom "./deploy.sh" $bashScript

    Write-Host "--- 3. Sending ALL files to Server (ONE PASSWORD PROMPT) ---" -ForegroundColor Cyan
    scp ./publish.zip "./$serviceName" "./postgresql.conf" "./deploy.sh" root@${serverIp}:/tmp/

    Write-Host "--- 4. Executing Deployment on Server (SECOND PASSWORD PROMPT) ---" -ForegroundColor Cyan
    # sed убирает \r (Windows переносы), а также удаляет BOM маркеры, если они каким-то чудом пролезли
    ssh root@$serverIp "sed -i 's/\r$//; 1s/^\xef\xbb\xbf//' /tmp/deploy.sh && bash /tmp/deploy.sh"

    Write-Host "--- DONE! PLATFORM DEPLOYED & OPTIMIZED SUCCESSFULLY! ---" -ForegroundColor Green
}
catch {
    Write-Host "DEPLOY ERROR: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Stack Trace: $($_.ScriptStackTrace)" -ForegroundColor Gray
}
finally {
    # Полная зачистка Windows от временных файлов деплоя
    if (Test-Path "./publish.zip") { Remove-Item "./publish.zip" }
    if (Test-Path "./$serviceName") { Remove-Item "./$serviceName" }
    if (Test-Path "./postgresql.conf") { Remove-Item "./postgresql.conf" }
    if (Test-Path "./deploy.sh") { Remove-Item "./deploy.sh" }
    Write-Host "`nPress any key to exit..."
    $null = [Console]::ReadKey($true)
}