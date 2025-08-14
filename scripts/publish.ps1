param(
  [ValidateSet('client','server','all','standalone')][string]$Target = 'all',
  [ValidateSet('Debug','Release')][string]$Configuration = 'Release',
  [switch]$SelfContained,
  [string]$Runtime = 'win-x64'
)
$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$root = Resolve-Path (Join-Path $root '..')
Set-Location $root

$artifacts = Join-Path $root 'artifacts'
$clientOut = Join-Path $artifacts 'client'
$serverOut = Join-Path $artifacts 'server'
$null = New-Item -ItemType Directory -Force -Path $clientOut,$serverOut -ErrorAction SilentlyContinue

$commonArgs = @('-c', $Configuration, '--nologo', '/p:GenerateFullPaths=true', '"/consoleloggerparameters:NoSummary;ForceNoAlign"')
if ($SelfContained) { $commonArgs += @('-r', $Runtime, '/p:PublishSingleFile=true', '/p:SelfContained=true', '/p:PublishTrimmed=false') }

if ($Target -eq 'client' -or $Target -eq 'all') {
  Write-Host "Publishing client..." -ForegroundColor Cyan
  $out = Join-Path $clientOut $Configuration
  $null = New-Item -ItemType Directory -Force -Path $out -ErrorAction SilentlyContinue
  & dotnet publish 'P2PLauncher/P2PLauncher.csproj' @commonArgs -o $out
  Compress-Archive -Path (Join-Path $out '*') -DestinationPath (Join-Path $clientOut "P2PLauncher-client-$Configuration.zip") -Force
}

if ($Target -eq 'standalone' -or $Target -eq 'all') {
  Write-Host "Publishing standalone client..." -ForegroundColor Cyan
  $out = Join-Path $clientOut "$Configuration-standalone"
  $null = New-Item -ItemType Directory -Force -Path $out -ErrorAction SilentlyContinue
  & dotnet publish 'apps/P2PLauncher.Standalone/P2PLauncher.Standalone.csproj' @commonArgs -o $out
  Compress-Archive -Path (Join-Path $out '*') -DestinationPath (Join-Path $clientOut "P2PLauncher-standalone-$Configuration.zip") -Force
}

if ($Target -eq 'server' -or $Target -eq 'all') {
  Write-Host "Publishing server..." -ForegroundColor Cyan
  $out = Join-Path $serverOut $Configuration
  $null = New-Item -ItemType Directory -Force -Path $out -ErrorAction SilentlyContinue
  & dotnet publish 'apps/P2PLauncher.Server/P2PLauncher.Server.csproj' @commonArgs -o $out
  Compress-Archive -Path (Join-Path $out '*') -DestinationPath (Join-Path $serverOut "P2PLauncher-server-$Configuration.zip") -Force
}

Write-Host "Publish complete. Artifacts in $artifacts" -ForegroundColor Green
