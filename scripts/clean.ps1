$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$root = Resolve-Path (Join-Path $root '..')
Set-Location $root

Write-Host "Cleaning solution..." -ForegroundColor Cyan
& dotnet clean 'P2PLauncher.sln' --nologo /p:GenerateFullPaths=true "/consoleloggerparameters:NoSummary;ForceNoAlign"

Write-Host "Removing bin/obj/artifacts..." -ForegroundColor Cyan
Get-ChildItem -Path $root -Include bin,obj,artifacts -Directory -Recurse -ErrorAction SilentlyContinue | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue

Write-Host "Clean complete." -ForegroundColor Green
