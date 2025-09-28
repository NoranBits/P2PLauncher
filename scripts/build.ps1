param(
  [ValidateSet('client','server','all')][string]$Target = 'all',
  [ValidateSet('Debug','Release')][string]$Configuration = 'Release',
  [switch]$Clean
)
$ErrorActionPreference = 'Stop'

function Write-ErrorDetail([System.Exception]$ex) {
  Write-Error ($ex.Message)
  if ($ex.InnerException) { Write-Host "Inner: $($ex.InnerException.Message)" -ForegroundColor Yellow }
}

function Test-LastExitCode([string]$context) {
  if ($LASTEXITCODE -ne 0) { throw "Failed: $context (exit $LASTEXITCODE)" }
}

function Invoke-DotNet([string]$cmd, [string[]]$dotnetArgs, [string]$context) {
  & dotnet $cmd @dotnetArgs
  Test-LastExitCode $context
}

try {
  # Resolve solution root
  $root = Split-Path -Parent $MyInvocation.MyCommand.Path
  $root = Resolve-Path (Join-Path $root '..')
  Set-Location $root

  # Ensure artifacts root exists (downstream tooling expects it)
  $artifacts = Join-Path $root 'artifacts'
  if (!(Test-Path $artifacts)) { New-Item -ItemType Directory -Path $artifacts | Out-Null }

  if ($Clean) {
    Write-Host "Cleaning solution..." -ForegroundColor Cyan
    Invoke-DotNet 'clean' @('P2PLauncher.sln','--nologo','/p:GenerateFullPaths=true','"/consoleloggerparameters:NoSummary;ForceNoAlign"') 'dotnet clean'
  }

  Write-Host "Restoring packages..." -ForegroundColor Cyan
  Invoke-DotNet 'restore' @('P2PLauncher.sln') 'dotnet restore'

  $projects = @()
  if ($Target -eq 'client' -or $Target -eq 'all') { $projects += 'P2PLauncher/P2PLauncher.csproj' }
  if ($Target -eq 'server' -or $Target -eq 'all') { $projects += 'apps/P2PLauncher.Server/P2PLauncher.Server.csproj' }

  foreach ($proj in $projects) {
    Write-Host "Building $proj ($Configuration)..." -ForegroundColor Cyan
    Invoke-DotNet 'build' @($proj,'-c',$Configuration,'--nologo','/p:GenerateFullPaths=true','"/consoleloggerparameters:NoSummary;ForceNoAlign"') "build $proj"
  }

  Write-Host "Build complete." -ForegroundColor Green
}
catch {
  Write-Host "Build failed." -ForegroundColor Red
  Write-ErrorDetail $_.Exception
  exit 1
}
