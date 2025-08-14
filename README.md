# P2PLauncher (modernized for .NET 9)

P2PLauncher connects peers into a virtual LAN using FreeLAN with a WPF client and a headless server CLI.

## Quick start

Prerequisites:
- .NET 9 SDK (https://dotnet.microsoft.com)
- Windows 10/11 for WPF client

Build and publish (PowerShell):

- Build all
  powershell -ExecutionPolicy Bypass -File scripts/build.ps1 -Target all -Configuration Release

- Publish zipped artifacts
  powershell -ExecutionPolicy Bypass -File scripts/publish.ps1 -Target all -Configuration Release

- Publish standalone client (Self-contained)
  powershell -ExecutionPolicy Bypass -File scripts/publish.ps1 -Target standalone -Configuration Release -SelfContained -Runtime win-x64

See `docs/INSTRUCTIONS.md` for step-by-step details.

---

## What changed in this fork

This repository is a modernization fork of the original P2PLauncher project by Kacper "K4CZP3R" Serewis and Noah "Ndo360" Cline. Key changes in this fork:
- Migrated projects to SDK-style and .NET 9 (WPF client uses net9.0-windows).
- Added Microsoft analyzers and `.editorconfig` to enforce code quality rules.
- Introduced a headless server CLI in `apps/P2PLauncher.Server/` with structured status output.
- Added `FeedbackService` for best-effort diagnostics, a Diagnostics UI, and connection tests (TCP/ICMP).
- Added PowerShell automation scripts and a GitHub Actions CI workflow for Windows builds & artifact uploads.
- Created `apps/P2PLauncher.Standalone/` as a minimal one-click client packaging target.

## Credits and upstream
This project builds on the original P2PLauncher (GPL-3.0) by:
- Kacper "K4CZP3R" Serewis — Lead programmer
- Noah "Ndo360" Cline — Project lead

Upstream: https://github.com/kacper-serewis/P2PLauncher (release 0.4.3.debug)

All original code remains under GPL-3.0. See `LICENSE`.

---

## Diagnostics & testing
- `FeedbackService` writes best-effort diagnostics to `logs/diagnostics.log`.
- Client diagnostics window supports TCP connect and ICMP ping tests.

---

## Versioning
- Fork version: 0.5.0 (breaking changes from upstream due to runtime upgrade and layout changes)

