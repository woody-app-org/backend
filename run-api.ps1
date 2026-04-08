$ErrorActionPreference = "Stop"
$backendRoot = $PSScriptRoot
. (Join-Path $backendRoot "scripts\Load-DotEnv.ps1")
Set-Location (Join-Path $backendRoot "src")
dotnet run --project .\Woody.Api\ --launch-profile https
