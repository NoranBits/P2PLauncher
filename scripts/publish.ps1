param(
  [ValidateSet('client','server','all','standalone')][string]$Target = 'all',
  [ValidateSet('Debug','Release')][string]$Configuration = 'Release',
  [switch]$SelfContained,
  [string]$Runtime = 'win-x64'
)
$ErrorActionPreference = 'Stop'

function Write-ErrorDetail([System.Exception]$ex) {
  Write-Error ($ex.Message)
  if ($ex.InnerException) { Write-Host "Inner: $($ex.InnerException.Message)" -ForegroundColor Yellow }
}

function Test-LastExitCode([string]$context) { if ($LASTEXITCODE -ne 0) { throw "Failed: $context (exit $LASTEXITCODE)" } }
function Invoke-DotNet([string]$cmd, [string[]]$dotnetArgs, [string]$context) { & dotnet $cmd @dotnetArgs; Test-LastExitCode $context }

function Invoke-WithRetry([ScriptBlock]$Action, [int]$Retries = 5, [int]$DelayMs = 250, [string]$Context = 'operation') {
  for ($i=1; $i -le $Retries; $i++) {
    try { & $Action; return } catch { if ($i -eq $Retries) { throw "${Context} failed after ${Retries} attempts: $($_.Exception.Message)" } Start-Sleep -Milliseconds $DelayMs }
  }
}

function Set-Content-Retry([string]$Path, [string]$Value, [string]$Encoding='UTF8') {
  Invoke-WithRetry -Context "write $Path" -Action { Set-Content -Path $Path -Value $Value -Encoding $Encoding -Force }
}

function Compress-Archive-Retry([string]$Source, [string]$Destination) {
  if (Test-Path $Destination) { Remove-Item -Force $Destination }
  Invoke-WithRetry -Context "zip $Destination" -Action { Compress-Archive -Path $Source -DestinationPath $Destination -Force }
}

function Copy-Item-Retry([string]$Source, [string]$Destination, [switch]$Recurse) {
  Invoke-WithRetry -Context "copy to $Destination" -Action { Copy-Item -Path $Source -Destination $Destination -Force -Recurse:$Recurse }
}

function Get-PublishArgs([string]$projectPath, [string]$cfg, [string]$outPath) {
  $args = @($projectPath,'-c',$cfg,'-nologo','/p:GenerateFullPaths=true','/consoleloggerparameters:NoSummary;ForceNoAlign')
  if ($SelfContained) { $args += @('-r',$Runtime,'/p:PublishSingleFile=true','/p:SelfContained=true','/p:PublishTrimmed=false') }
  $args += @('-o',$outPath)
  return $args
}

try {
  $root = Split-Path -Parent $MyInvocation.MyCommand.Path
  $root = Resolve-Path (Join-Path $root '..')
  Set-Location $root

  $artifacts = Join-Path $root 'artifacts'
  $clientOut = Join-Path $artifacts 'client'
  $serverOut = Join-Path $artifacts 'server'
  $null = New-Item -ItemType Directory -Force -Path $clientOut,$serverOut -ErrorAction SilentlyContinue

  function Publish-ClientVariant($cfg, $suffix) {
    Write-Host "Publishing client ($cfg)..." -ForegroundColor Cyan
    $out = Join-Path $clientOut $cfg
    $null = New-Item -ItemType Directory -Force -Path $out -ErrorAction SilentlyContinue
    $args = Get-PublishArgs -projectPath 'P2PLauncher/P2PLauncher.csproj' -cfg $cfg -outPath $out
    Invoke-DotNet 'publish' $args "publish client $cfg"
    $zip = Join-Path $clientOut "P2PLauncher-client-$suffix.zip"
    Compress-Archive-Retry -Source (Join-Path $out '*') -Destination $zip
    Write-Host "Created $zip" -ForegroundColor Green
  }

  function Publish-StandaloneVariant($cfg, $suffix) {
    Write-Host "Publishing standalone client ($cfg)..." -ForegroundColor Cyan
    $out = Join-Path $clientOut "$cfg-standalone"
    $null = New-Item -ItemType Directory -Force -Path $out -ErrorAction SilentlyContinue
    $args = Get-PublishArgs -projectPath 'apps/P2PLauncher.Standalone/P2PLauncher.Standalone.csproj' -cfg $cfg -outPath $out
    Invoke-DotNet 'publish' $args "publish standalone $cfg"
    $zip = Join-Path $clientOut "P2PLauncher-standalone-$suffix.zip"
    Compress-Archive-Retry -Source (Join-Path $out '*') -Destination $zip
    Write-Host "Created $zip" -ForegroundColor Green
  }

  function Write-ServerBootScripts($out) {
    $bat = @(
      '@echo off', 'REM Simple server bootstrap — adjust arguments as needed', 'cd /d "%~dp0"',
      'if exist P2PLauncher.Server.exe (', '  echo Launching P2PLauncher.Server', '  start "P2PLauncher Server" "%~dp0\P2PLauncher.Server.exe" %*', ') else (', '  echo ERROR: P2PLauncher.Server.exe not found in this folder.', '  exit /b 1', ')'
    ) -join "`r`n"
    Set-Content-Retry -Path (Join-Path $out 'run-server.bat') -Value $bat -Encoding 'ASCII'

    $ps1 = @(
      'Param([String[]]$Args)', 'Write-Host "Launching P2PLauncher.Server..."', 'Push-Location $PSScriptRoot',
      'if (Test-Path "P2PLauncher.Server.exe") {', '  Start-Process -FilePath "./P2PLauncher.Server.exe" -ArgumentList $Args -NoNewWindow',
      '} else {', '  Write-Error "P2PLauncher.Server.exe not found in $PSScriptRoot"; exit 1', '}', 'Pop-Location'
    ) -join "`r`n"
    Set-Content-Retry -Path (Join-Path $out 'run-server.ps1') -Value $ps1 -Encoding 'UTF8'
  }

  function Publish-ServerVariant($cfg) {
    Write-Host "Publishing server ($cfg)..." -ForegroundColor Cyan
    $out = Join-Path $serverOut $cfg
    $null = New-Item -ItemType Directory -Force -Path $out -ErrorAction SilentlyContinue
    $args = Get-PublishArgs -projectPath 'apps/P2PLauncher.Server/P2PLauncher.Server.csproj' -cfg $cfg -outPath $out
    Invoke-DotNet 'publish' $args "publish server $cfg"
    Write-ServerBootScripts -out $out
    $zip = Join-Path $serverOut "P2PLauncher-server-$cfg.zip"
    Compress-Archive-Retry -Source (Join-Path $out '*') -Destination $zip
    Write-Host "Created $zip" -ForegroundColor Green
  }

  # Client variants
  if ($Target -eq 'client' -or $Target -eq 'all') {
    Publish-ClientVariant -cfg $Configuration -suffix $Configuration
    Publish-ClientVariant -cfg 'Debug' -suffix 'Debug'
    $clientDebugDir = Join-Path $clientOut 'Debug'
    $clientDevDir = Join-Path $clientOut 'Dev'
    if (Test-Path $clientDevDir) { Remove-Item -Recurse -Force $clientDevDir }
    Copy-Item-Retry -Source $clientDebugDir -Destination $clientDevDir -Recurse
    Copy-Item-Retry -Source (Join-Path $clientOut 'P2PLauncher-client-Debug.zip') -Destination (Join-Path $clientOut 'P2PLauncher-client-Dev.zip')
  }

  # Standalone client variants
  if ($Target -eq 'standalone' -or $Target -eq 'all') {
    Publish-StandaloneVariant -cfg $Configuration -suffix $Configuration
    Publish-StandaloneVariant -cfg 'Debug' -suffix 'Debug'
    $standaloneDebugDir = Join-Path $clientOut 'Debug-standalone'
    $standaloneDevDir = Join-Path $clientOut 'Dev-standalone'
    if (Test-Path $standaloneDevDir) { Remove-Item -Recurse -Force $standaloneDevDir }
    Copy-Item-Retry -Source $standaloneDebugDir -Destination $standaloneDevDir -Recurse
    Copy-Item-Retry -Source (Join-Path $clientOut 'P2PLauncher-standalone-Debug.zip') -Destination (Join-Path $clientOut 'P2PLauncher-standalone-Dev.zip')
  }

  # Server variants
  if ($Target -eq 'server' -or $Target -eq 'all') {
    Publish-ServerVariant -cfg $Configuration
    Publish-ServerVariant -cfg 'Debug'
    $serverDebugDir = Join-Path $serverOut 'Debug'
    $serverDevDir = Join-Path $serverOut 'Dev'
    if (Test-Path $serverDevDir) { Remove-Item -Recurse -Force $serverDevDir }
    Copy-Item-Retry -Source $serverDebugDir -Destination $serverDevDir -Recurse
    Copy-Item-Retry -Source (Join-Path $serverOut 'P2PLauncher-server-Debug.zip') -Destination (Join-Path $serverOut 'P2PLauncher-server-Dev.zip')
  }

  Write-Host "Publish complete. Artifacts in $artifacts" -ForegroundColor Green
}
catch {
  Write-Host "Publish failed." -ForegroundColor Red
  Write-ErrorDetail $_.Exception
  exit 1
}
