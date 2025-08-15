$ErrorActionPreference = 'Stop'

function Write-ErrorDetail([System.Exception]$ex) {
  Write-Error ($ex.Message)
  if ($ex.InnerException) { Write-Host "Inner: $($ex.InnerException.Message)" -ForegroundColor Yellow }
}

try {
  $root = Split-Path -Parent $MyInvocation.MyCommand.Path
  $root = Resolve-Path (Join-Path $root '..')
  Set-Location $root

  Write-Host "Cleaning solution..." -ForegroundColor Cyan
  & dotnet clean 'P2PLauncher.sln' --nologo /p:GenerateFullPaths=true '"/consoleloggerparameters:NoSummary;ForceNoAlign"'
  if ($LASTEXITCODE -ne 0) { throw "dotnet clean failed (exit $LASTEXITCODE)" }

  Write-Host "Removing bin/obj/artifacts..." -ForegroundColor Cyan
  Get-ChildItem -Path $root -Include bin,obj,artifacts -Directory -Recurse -ErrorAction SilentlyContinue |
    ForEach-Object {
      try { Remove-Item -Recurse -Force -LiteralPath $_.FullName -ErrorAction Stop }
      catch { Write-Host "Warn: failed to remove $($_.FullName): $($_.Exception.Message)" -ForegroundColor Yellow }
    }

  Write-Host "Clean complete." -ForegroundColor Green
}
catch {
  Write-Host "Clean failed." -ForegroundColor Red
  Write-ErrorDetail $_.Exception
  exit 1
}
