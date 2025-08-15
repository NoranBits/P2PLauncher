# P2PLauncher Modernization Playbook (net9)

This document describes the standardized workflows for building, publishing, and operating the modernized P2PLauncher (WPF client + headless server).

## Prerequisites
- Windows 10/11 with PowerShell 5.1 or PowerShell 7+
- .NET SDK 9.0.x installed
- Git

## Architecture
- Client: WPF (.NET 9) with MVVM using CommunityToolkit.Mvvm.
  - ViewModels: `P2PLauncher/ViewModels/*`
  - Views: `P2PLauncher/View/*.xaml`
  - Commands: `[RelayCommand]` from MVVM Toolkit
- Server: CLI with System.CommandLine + Serilog logging to `logs/server.log`.

## Scripts
- Clean solution
  powershell -ExecutionPolicy Bypass -File scripts/clean.ps1

- Build client/server/all
  powershell -ExecutionPolicy Bypass -File scripts/build.ps1 -Target all -Configuration Release

- Publish artifacts (client/server/standalone)
  powershell -ExecutionPolicy Bypass -File scripts/publish.ps1 -Target all -Configuration Release

## Server status schema (JSON)
See README for the full shape. Includes `CurrentPeers` with assigned `Id` and default `Name` preserved during run.

## Coding standards
- Microsoft analyzers enabled via Directory.Build.props
- .editorconfig enforces code style; treat recommendations during PR review

## Next steps
- Wire `MainViewModel` to existing services for connection/persistence
- Add unit tests for ViewModels and status command
