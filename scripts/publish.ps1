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

function Publish-ClientVariant($cfg, $suffix) {
  Write-Host "Publishing client ($cfg)..." -ForegroundColor Cyan
  $out = Join-Path $clientOut $cfg
  $null = New-Item -ItemType Directory -Force -Path $out -ErrorAction SilentlyContinue
  & dotnet publish 'P2PLauncher/P2PLauncher.csproj' @($commonArgs[0], $cfg, @($commonArgs[2..($commonArgs.Length-1)])) -o $out
  $zip = Join-Path $clientOut "P2PLauncher-client-$suffix.zip"
  Compress-Archive -Path (Join-Path $out '*') -DestinationPath $zip -Force
  Write-Host "Created $zip" -ForegroundColor Green
}

function Publish-StandaloneVariant($cfg, $suffix) {
  Write-Host "Publishing standalone client ($cfg)..." -ForegroundColor Cyan
  $out = Join-Path $clientOut "$cfg-standalone"
  $null = New-Item -ItemType Directory -Force -Path $out -ErrorAction SilentlyContinue
  & dotnet publish 'apps/P2PLauncher.Standalone/P2PLauncher.Standalone.csproj' @($commonArgs[0], $cfg, @($commonArgs[2..($commonArgs.Length-1)])) -o $out
  $zip = Join-Path $clientOut "P2PLauncher-standalone-$suffix.zip"
  Compress-Archive -Path (Join-Path $out '*') -DestinationPath $zip -Force
  Write-Host "Created $zip" -ForegroundColor Green
}

if ($Target -eq 'client' -or $Target -eq 'all') {
  Publish-ClientVariant -cfg $Configuration -suffix $Configuration
  # Also produce Debug and a 'dev' variant (dev is same as Debug but labeled)
  Publish-ClientVariant -cfg 'Debug' -suffix 'Debug'
  Copy-Item -Path (Join-Path $clientOut 'P2PLauncher-client-Debug.zip') -Destination (Join-Path $clientOut 'P2PLauncher-client-Dev.zip') -Force
}

if ($Target -eq 'standalone' -or $Target -eq 'all') {
  Publish-StandaloneVariant -cfg $Configuration -suffix $Configuration
  Publish-StandaloneVariant -cfg 'Debug' -suffix 'Debug'
  Copy-Item -Path (Join-Path $clientOut 'P2PLauncher-standalone-Debug.zip') -Destination (Join-Path $clientOut 'P2PLauncher-standalone-Dev.zip') -Force
}

if ($Target -eq 'server' -or $Target -eq 'all') {
  Write-Host "Publishing server..." -ForegroundColor Cyan
  $out = Join-Path $serverOut $Configuration
  $null = New-Item -ItemType Directory -Force -Path $out -ErrorAction SilentlyContinue
  & dotnet publish 'apps/P2PLauncher.Server/P2PLauncher.Server.csproj' @commonArgs -o $out

  # Create simple boot scripts in the server output so releases include a runnable entrypoint
  $bat = @(
    '@echo off',
    'REM Simple server bootstrap — adjust arguments as needed',
    'cd /d "%~dp0"',
    'if exist P2PLauncher.Server.exe (',
    '  echo Launching P2PLauncher.Server',
    '  start "P2PLauncher Server" "%~dp0\P2PLauncher.Server.exe" %*',
    ') else (',
    '  echo ERROR: P2PLauncher.Server.exe not found in this folder.',
    '  exit /b 1',
    ')'
  ) -join "`r`n"
  Set-Content -Path (Join-Path $out 'run-server.bat') -Value $bat -Encoding ASCII

  $ps1 = @(
    'Param([String[]]$Args)',
    'Write-Host "Launching P2PLauncher.Server..."',
    'Push-Location $PSScriptRoot',
    'if (Test-Path "P2PLauncher.Server.exe") {',
    '  Start-Process -FilePath "./P2PLauncher.Server.exe" -ArgumentList $Args -NoNewWindow',
    '} else {',
    '  Write-Error "P2PLauncher.Server.exe not found in $PSScriptRoot"; exit 1',
    '}',
    'Pop-Location'
  ) -join "`r`n"
  Set-Content -Path (Join-Path $out 'run-server.ps1') -Value $ps1 -Encoding UTF8

  $zip = Join-Path $serverOut "P2PLauncher-server-$Configuration.zip"
  Compress-Archive -Path (Join-Path $out '*') -DestinationPath $zip -Force
  Write-Host "Created $zip" -ForegroundColor Green
}

Write-Host "Publish complete. Artifacts in $artifacts" -ForegroundColor Green
