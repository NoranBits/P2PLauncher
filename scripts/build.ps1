param(
  [ValidateSet('client','server','all')][string]$Target = 'all',
  [ValidateSet('Debug','Release')][string]$Configuration = 'Release'
)
$ErrorActionPreference = 'Stop'

# Resolve solution root
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$root = Resolve-Path (Join-Path $root '..')
Set-Location $root

$artifacts = Join-Path $root 'artifacts'
if (!(Test-Path $artifacts)) { New-Item -ItemType Directory -Path $artifacts | Out-Null }

Write-Host "Restoring packages..." -ForegroundColor Cyan
& dotnet restore 'P2PLauncher.sln'

$projects = @()
if ($Target -eq 'client' -or $Target -eq 'all') { $projects += 'P2PLauncher/P2PLauncher.csproj' }
if ($Target -eq 'server' -or $Target -eq 'all') { $projects += 'apps/P2PLauncher.Server/P2PLauncher.Server.csproj' }

foreach ($proj in $projects) {
  Write-Host "Building $proj ($Configuration)..." -ForegroundColor Cyan
  & dotnet build $proj -c $Configuration --nologo /p:GenerateFullPaths=true "/consoleloggerparameters:NoSummary;ForceNoAlign"
}

Write-Host "Build complete." -ForegroundColor Green
