$ErrorActionPreference = "Stop"
$backendRoot = $PSScriptRoot
. (Join-Path $backendRoot "scripts\Load-DotEnv.ps1")
Set-Location (Join-Path $backendRoot "src")
# Sem --launch-profile: perfis em launchSettings.json forçam ASPNETCORE_ENVIRONMENT=Development
# e ignoram ASPNETCORE_ENVIRONMENT do .env. URLs alinhadas ao perfil "https" antigo.
dotnet run --project .\Woody.Api\ --no-launch-profile --urls "https://localhost:7101;http://localhost:5000"
