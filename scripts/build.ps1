param(
  [ValidateSet('all','client','server','standalone')][string]$Target = 'all',
  [ValidateSet('Debug','Release')][string]$Configuration = 'Release',
  [switch]$SelfContained,
  [string]$Runtime = 'win-x64',
  [switch]$Clean
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

function Get-GitVersion {
  try {
    $tag = & git describe --tags --exact-match 2>$null
    if ($LASTEXITCODE -eq 0) {
      return $tag.Trim()
    }
    
    $shortHash = & git rev-parse --short HEAD 2>$null
    if ($LASTEXITCODE -eq 0) {
      return "dev-$($shortHash.Trim())"
    }
  } catch {
    Write-Warning "Could not determine git version: $($_.Exception.Message)"
  }
  
  return "unknown"
}

try {
  Write-Information "P2PLauncher Build Script"
  Write-Information "Target: $Target, Configuration: $Configuration, Runtime: $Runtime"
  
  # Resolve solution root
  $root = Split-Path -Parent $MyInvocation.MyCommand.Path
  $root = Resolve-Path (Join-Path $root '..')
  Set-Location $root
  Write-Information "Working directory: $root"

  # Get version information
  $version = Get-GitVersion
  Write-Information "Build version: $version"

  # Ensure artifacts root exists
  $artifacts = Join-Path $root 'artifacts'
  if (!(Test-Path $artifacts)) { 
    New-Item -ItemType Directory -Path $artifacts | Out-Null 
    Write-Information "Created artifacts directory: $artifacts"
  }

  if ($Clean) {
    Write-Information "Cleaning solution..." 
    Invoke-DotNet 'clean' @(
      'P2PLauncher.sln',
      '--nologo',
      '--verbosity', 'minimal',
      '/p:GenerateFullPaths=true',
      '/consoleloggerparameters:NoSummary;ForceNoAlign'
    ) 'dotnet clean'
  }

  Write-Information "Restoring packages..."
  Invoke-DotNet 'restore' @(
    'P2PLauncher.sln',
    '--nologo',
    '--verbosity', 'minimal'
  ) 'dotnet restore'

  # Build projects based on target
  $projects = @()
  
  if ($Target -eq 'client' -or $Target -eq 'all') { 
    $projects += @{
      Path = 'P2PLauncher/P2PLauncher.csproj'
      Name = 'Client'
    }
  }
  
  if ($Target -eq 'server' -or $Target -eq 'all') { 
    $projects += @{
      Path = 'apps/P2PLauncher.Server/P2PLauncher.Server.csproj'
      Name = 'Server'
    }
  }
  
  if ($Target -eq 'standalone' -or $Target -eq 'all') { 
    $projects += @{
      Path = 'apps/P2PLauncher.Standalone/P2PLauncher.Standalone.csproj'
      Name = 'Standalone'
    }
  }

  foreach ($proj in $projects) {
    Write-Information "Building $($proj.Name): $($proj.Path) ($Configuration)..."
    
    $buildArgs = @(
      $proj.Path,
      '-c', $Configuration,
      '--nologo',
      '--verbosity', 'minimal',
      '/p:GenerateFullPaths=true',
      '/consoleloggerparameters:NoSummary;ForceNoAlign',
      "/p:Version=$version",
      "/p:AssemblyVersion=1.0.0.0",
      "/p:FileVersion=1.0.0.0"
    )
    
    if ($SelfContained) {
      $buildArgs += @('-r', $Runtime, '/p:SelfContained=true')
    }
    
    Invoke-DotNet 'build' $buildArgs "build $($proj.Name)"
  }

  Write-Information "Build completed successfully for target: $Target" 
  Write-Information "Artifacts directory: $artifacts"
}
catch {
  Write-Host "Build failed." -ForegroundColor Red
  Write-ErrorDetail $_.Exception
  exit 1
}
