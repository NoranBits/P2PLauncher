# Changelog

## 2025-08-15 — Standalone client scaffolding (newUpdated)
- Added `apps/P2PLauncher.Standalone/` project that references the main WPF project for UI reuse.
- Updated `scripts/publish.ps1` to support publishing the standalone client via `-Target standalone`.
- Updated README with standalone client notes and packaging guidance.

## 2025-08-15 — Version & upstream comparison (newUpdated)
- Upstream reference: https://github.com/kacper-serewis/P2PLauncher (latest upstream release: 0.4.3.debug as of 2021-01-27).
- Change summary vs upstream: migrated runtime/platform to .NET 9, added analyzers and CI, introduced a headless server CLI, added diagnostics UI, and added PowerShell automation and packaging.
- Version adjustment: runtime shift, analyzer fixes, and project layout changes.
- Credits: this fork modernizes and builds on the original P2PLauncher by Kacper "K4CZP3R" Serewis and Noah "Ndo360" Cline. See upstream: https://github.com/kacper-serewis/P2PLauncher

## 2025-08-15 — Version 0.5.0 (Modernized)
- Bumped project version to 0.5.0 to reflect modernization and breaking changes from the original codebase.
- Summary of changes vs original (kacper-serewis/P2PLauncher):
  - Migrated client from .NET Framework to .NET 9 (WPF, net9.0-windows) — modern SDK-style csproj.
  - Split headless server CLI into `apps/P2PLauncher.Server/` (net9); added `status` JSON command and Serilog-based logging.
  - Added `libs/P2PLauncher.Core/` to host shared enums and keep UI/core separation.
  - Introduced PowerShell scripts (`scripts/clean.ps1`, `build.ps1`, `publish.ps1`) and GitHub Actions CI for Windows builds.
  - Added analyzers (Microsoft.CodeAnalysis.NetAnalyzers) and `.editorconfig` to enforce quality rules.
  - Implemented Diagnostics UI (`DiagnosticsWindow`) with connection checks and `FeedbackService` logging.
  - Scaffolding for a standalone one-click client: `apps/P2PLauncher.Standalone/` project which references the UI project for packaging.
  - Replaced several legacy APIs and added package references for .NET 9 compatibility (e.g., `System.Management`, `System.ServiceProcess.ServiceController`).

Notes:
- This fork is a modernization and contains breaking changes relative to the original repository by Kacper Serewis (https://github.com/kacper-serewis/P2PLauncher). Credit and sources are noted in README.
- Recommended next steps: add unit tests, add CI gating for analyzers, add a release pipeline to produce signed installers.

