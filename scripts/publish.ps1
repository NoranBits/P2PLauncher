param(
  [ValidateSet('all','client','server','standalone')][string]$Target = 'all',
  [ValidateSet('Debug','Release')][string]$Configuration = 'Release',
  [switch]$SelfContained,
  [string]$Runtime = 'win-x64'
)

$ErrorActionPreference = 'Stop'
$InformationPreference = 'Continue'

function Write-ErrorDetail([System.Exception]$ex) {
  Write-Error ($ex.Message)
  if ($ex.InnerException) { 
    Write-Host "Inner: $($ex.InnerException.Message)" -ForegroundColor Yellow 
  }
}

function Test-LastExitCode([string]$context) { 
  if ($LASTEXITCODE -ne 0) { 
    throw "Failed: $context (exit code $LASTEXITCODE)" 
  } 
}

function Invoke-DotNet([string]$cmd, [string[]]$dotnetArgs, [string]$context) { 
  Write-Information "Executing: dotnet $cmd $($dotnetArgs -join ' ')"
  & dotnet $cmd @dotnetArgs
  Test-LastExitCode $context 
}

function Invoke-WithRetry([ScriptBlock]$Action, [int]$Retries = 5, [int]$DelayMs = 250, [string]$Context = 'operation') {
  for ($i = 1; $i -le $Retries; $i++) {
    try { 
      & $Action
      return 
    } catch { 
      if ($i -eq $Retries) { 
        throw "${Context} failed after ${Retries} attempts: $($_.Exception.Message)" 
      } 
      Start-Sleep -Milliseconds $DelayMs 
    }
  }
}

function Set-Content-Retry([string]$Path, [string]$Value, [string]$Encoding = 'UTF8') {
  Invoke-WithRetry -Context "write $Path" -Action { 
    Set-Content -Path $Path -Value $Value -Encoding $Encoding -Force 
  }
}

function Compress-Archive-Retry([string]$Source, [string]$Destination) {
  if (Test-Path $Destination) { Remove-Item -Force $Destination }
  Invoke-WithRetry -Context "zip $Destination" -Action { 
    Compress-Archive -Path $Source -DestinationPath $Destination -Force 
  }
}

function Copy-Item-Retry([string]$Source, [string]$Destination, [switch]$Recurse) {
  Invoke-WithRetry -Context "copy to $Destination" -Action { 
    Copy-Item -Path $Source -Destination $Destination -Force -Recurse:$Recurse 
  }
}

function Get-GitVersion {
  try {
    $tag = & git describe --tags --exact-match 2>$null
    if ($LASTEXITCODE -eq 0) {
      return $tag.Trim()
    }
    
    $shortHash = & git rev-parse --short HEAD 2>$null
    if ($LASTEXITCODE -eq 0) {
      $timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
      return "dev-$($shortHash.Trim())-$timestamp"
    }
  } catch {
    Write-Warning "Could not determine git version: $($_.Exception.Message)"
  }
  
  return "unknown-$(Get-Date -Format 'yyyyMMdd-HHmmss')"
}

function Get-PublishArgs([string]$projectPath, [string]$cfg, [string]$outPath, [string]$version) {
  $args = @(
    $projectPath,
    '-c', $cfg,
    '--nologo',
    '--verbosity', 'minimal',
    '/p:GenerateFullPaths=true',
    '/consoleloggerparameters:NoSummary;ForceNoAlign',
    "/p:Version=$version",
    "/p:AssemblyVersion=1.0.0.0",
    "/p:FileVersion=1.0.0.0"
  )
  
  if ($SelfContained) { 
    $args += @(
      '-r', $Runtime,
      '/p:PublishSingleFile=true',
      '/p:SelfContained=true',
      '/p:PublishTrimmed=false',
      '/p:PublishReadyToRun=false'
    ) 
  }
  
  $args += @('-o', $outPath)
  return $args
}

function Write-VersionInfo([string]$outPath, [string]$version, [string]$target) {
  $versionInfo = @{
    Version = $version
    Target = $target
    Configuration = $Configuration
    Runtime = $Runtime
    SelfContained = $SelfContained.IsPresent
    BuildTime = (Get-Date).ToString('yyyy-MM-dd HH:mm:ss UTC')
    BuildMachine = $env:COMPUTERNAME
  }
  
  $versionJson = $versionInfo | ConvertTo-Json -Depth 3
  Set-Content-Retry -Path (Join-Path $outPath 'version.json') -Value $versionJson -Encoding 'UTF8'
}

try {
  Write-Information "P2PLauncher Publish Script"
  Write-Information "Target: $Target, Configuration: $Configuration, Runtime: $Runtime"
  
  $root = Split-Path -Parent $MyInvocation.MyCommand.Path
  $root = Resolve-Path (Join-Path $root '..')
  Set-Location $root
  Write-Information "Working directory: $root"

  # Get version information from git
  $version = Get-GitVersion
  Write-Information "Publishing version: $version"

  $artifacts = Join-Path $root 'artifacts'
  $null = New-Item -ItemType Directory -Force -Path $artifacts -ErrorAction SilentlyContinue

  function Publish-ClientVariant($cfg, $suffix) {
    Write-Information "Publishing client ($cfg)..."
    $targetPath = Join-Path $artifacts 'client'
    $out = Join-Path $targetPath $cfg
    $null = New-Item -ItemType Directory -Force -Path $out -ErrorAction SilentlyContinue
    
    $args = Get-PublishArgs -projectPath 'P2PLauncher/P2PLauncher.csproj' -cfg $cfg -outPath $out -version $version
    Invoke-DotNet 'publish' $args "publish client $cfg"
    
    Write-VersionInfo -outPath $out -version $version -target 'client'
    
    $zip = Join-Path $targetPath "P2PLauncher-client-$suffix-$version.zip"
    Compress-Archive-Retry -Source (Join-Path $out '*') -Destination $zip
    Write-Information "Created: $zip"
  }

  function Publish-StandaloneVariant($cfg, $suffix) {
    Write-Information "Publishing standalone client ($cfg)..."
    $targetPath = Join-Path $artifacts 'standalone'
    $out = Join-Path $targetPath $cfg
    $null = New-Item -ItemType Directory -Force -Path $out -ErrorAction SilentlyContinue
    
    $args = Get-PublishArgs -projectPath 'apps/P2PLauncher.Standalone/P2PLauncher.Standalone.csproj' -cfg $cfg -outPath $out -version $version
    Invoke-DotNet 'publish' $args "publish standalone $cfg"
    
    Write-VersionInfo -outPath $out -version $version -target 'standalone'
    
    $zip = Join-Path $targetPath "P2PLauncher-standalone-$suffix-$version.zip"
    Compress-Archive-Retry -Source (Join-Path $out '*') -Destination $zip
    Write-Information "Created: $zip"
  }

  function Write-ServerBootScripts($out) {
    $bat = @(
      '@echo off',
      'REM P2PLauncher Server Bootstrap Script',
      'REM Adjust arguments as needed',
      'cd /d "%~dp0"',
      'if exist P2PLauncher.Server.exe (',
      '  echo Launching P2PLauncher.Server...',
      '  start "P2PLauncher Server" "%~dp0\P2PLauncher.Server.exe" %*',
      ') else (',
      '  echo ERROR: P2PLauncher.Server.exe not found in this folder.',
      '  echo Current directory: %CD%',
      '  pause',
      '  exit /b 1',
      ')'
    ) -join "`r`n"
    Set-Content-Retry -Path (Join-Path $out 'run-server.bat') -Value $bat -Encoding 'ASCII'

    $ps1 = @(
      'Param([String[]]$Args)',
      'Write-Host "Launching P2PLauncher.Server..." -ForegroundColor Green',
      'Push-Location $PSScriptRoot',
      'try {',
      '  if (Test-Path "P2PLauncher.Server.exe") {',
      '    if ($Args) {',
      '      & "./P2PLauncher.Server.exe" @Args',
      '    } else {',
      '      & "./P2PLauncher.Server.exe"',
      '    }',
      '    $exitCode = $LASTEXITCODE',
      '    Write-Host "Server exited with code: $exitCode" -ForegroundColor $(if($exitCode -eq 0){"Green"}else{"Red"})',
      '    exit $exitCode',
      '  } else {',
      '    Write-Error "P2PLauncher.Server.exe not found in $PSScriptRoot"',
      '    exit 1',
      '  }',
      '} finally {',
      '  Pop-Location',
      '}'
    ) -join "`r`n"
    Set-Content-Retry -Path (Join-Path $out 'run-server.ps1') -Value $ps1 -Encoding 'UTF8'
  }

  function Publish-ServerVariant($cfg) {
    Write-Information "Publishing server ($cfg)..."
    $targetPath = Join-Path $artifacts 'server'
    $out = Join-Path $targetPath $cfg
    $null = New-Item -ItemType Directory -Force -Path $out -ErrorAction SilentlyContinue
    
    $args = Get-PublishArgs -projectPath 'apps/P2PLauncher.Server/P2PLauncher.Server.csproj' -cfg $cfg -outPath $out -version $version
    Invoke-DotNet 'publish' $args "publish server $cfg"
    
    Write-VersionInfo -outPath $out -version $version -target 'server'
    Write-ServerBootScripts -out $out
    
    $zip = Join-Path $targetPath "P2PLauncher-server-$cfg-$version.zip"
    Compress-Archive-Retry -Source (Join-Path $out '*') -Destination $zip
    Write-Information "Created: $zip"
  }

  # Publish based on target
  if ($Target -eq 'client' -or $Target -eq 'all') {
    Publish-ClientVariant -cfg $Configuration -suffix $Configuration
  }

  if ($Target -eq 'standalone' -or $Target -eq 'all') {
    Publish-StandaloneVariant -cfg $Configuration -suffix $Configuration
  }

  if ($Target -eq 'server' -or $Target -eq 'all') {
    Publish-ServerVariant -cfg $Configuration
  }

  Write-Information "Publish completed successfully!"
  Write-Information "Artifacts directory: $artifacts"
  
  # List created artifacts
  $zipFiles = Get-ChildItem -Path $artifacts -Recurse -Filter "*.zip" | Select-Object -ExpandProperty FullName
  if ($zipFiles) {
    Write-Information "Created artifacts:"
    foreach ($zip in $zipFiles) {
      $size = [math]::Round((Get-Item $zip).Length / 1MB, 2)
      Write-Information "  $(Split-Path $zip -Leaf) ($size MB)"
    }
  }
}
catch {
  Write-Host "Publish failed." -ForegroundColor Red
  Write-ErrorDetail $_.Exception
  exit 1
}
