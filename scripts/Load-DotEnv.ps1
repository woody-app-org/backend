<#
.SYNOPSIS
  Carrega variáveis de ambiente a partir de um ficheiro .env (formato KEY=VALUE).
  Não substitui o ASP.NET Core; apenas define variáveis no processo atual.
#>
param(
    [string] $EnvFile = (Join-Path (Split-Path $PSScriptRoot -Parent) ".env")
)

if (-not (Test-Path -LiteralPath $EnvFile)) {
    Write-Error "Ficheiro não encontrado: $EnvFile`nCopie .env.example para .env na raiz do backend."
}

Get-Content -LiteralPath $EnvFile -Encoding UTF8 | ForEach-Object {
    $line = $_.Trim()
    if ($line.Length -eq 0 -or $line.StartsWith('#')) { return }
    $eq = $line.IndexOf('=')
    if ($eq -lt 1) { return }
    $key = $line.Substring(0, $eq).Trim()
    $value = $line.Substring($eq + 1).Trim()
    if ($value.Length -ge 2 -and $value.StartsWith('"') -and $value.EndsWith('"')) {
        $value = $value.Substring(1, $value.Length - 2).Replace('\"', '"')
    }
    elseif ($value.Length -ge 2 -and $value.StartsWith("'") -and $value.EndsWith("'")) {
        $value = $value.Substring(1, $value.Length - 2).Replace("''", "'")
    }
    Set-Item -Path "Env:$key" -Value $value
}
