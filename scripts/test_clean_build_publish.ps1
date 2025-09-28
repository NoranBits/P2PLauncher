param(
  [ValidateSet('client','server','all')][string]$Target = 'all',
  [ValidateSet('Debug','Release')][string]$Configuration = 'Release',
  [switch]$SelfContained,
  [string]$Runtime = 'win-x64'
)
$ErrorActionPreference = 'Stop'

function Write-ErrorDetail([System.Exception]$ex) {
  Write-Error ($ex.Message)
  if ($ex.InnerException) { Write-Host "Inner: $($ex.InnerException.Message)" -ForegroundColor Yellow }
}

try {
  $root = Split-Path -Parent $MyInvocation.MyCommand.Path
  $root = Resolve-Path (Join-Path $root '..')
  Set-Location $root

  Write-Host "=== CLEAN ===" -ForegroundColor Cyan
  & ./scripts/clean.ps1

  Write-Host "=== BUILD ===" -ForegroundColor Cyan
  & ./scripts/build.ps1 -Target $Target -Configuration $Configuration -Clean

  Write-Host "=== PUBLISH ===" -ForegroundColor Cyan
  if ($SelfContained) {
    & ./scripts/publish.ps1 -Target $Target -Configuration $Configuration -SelfContained -Runtime $Runtime
  } else {
    & ./scripts/publish.ps1 -Target $Target -Configuration $Configuration
  }

  Write-Host "All steps completed successfully." -ForegroundColor Green
}
catch {
  Write-Host "Test pipeline failed." -ForegroundColor Red
  Write-ErrorDetail $_.Exception
  exit 1
}
