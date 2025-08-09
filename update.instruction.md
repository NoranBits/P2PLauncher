---
applyTo: workspace
owner: NoranBits
repo: P2PLauncher
branch: codex/upgrade-dependencies-for-better-performance
updated: 2025-08-10
---
# P2PLauncher Modernization & Upgrade Guide

Goal: Make the app buildable, runnable, and maintainable on current toolchains; update dependencies; resolve merge conflicts; optionally migrate from .NET Framework 4.8 WPF to .NET 8 (Windows) WPF.

Read this entire guide before making changes. Run each step and commit in small, reversible increments.

## 0) Current State Snapshot
- Solution: `P2PLauncher.sln` (single WPF app).
- Target framework: .NET Framework 4.8 (`TargetFrameworkVersion` v4.8 in `P2PLauncher.csproj`).
- Package manager: mixed (legacy `packages.config` + a `PackageReference` for Newtonsoft.Json). This is inconsistent.
- Dependencies: `Newtonsoft.Json` 13.0.3 (latest as of 2025-08). Uses Windows-only APIs: `System.Management`, `System.ServiceProcess`.
- CI: `.github/workflows/dev_build.yml` uses old actions (checkout@v2, set-output), MSBuild path step, and zip action.
- VS Code: placeholder `.vscode/launch.json` and basic tasks; no debug exe path.
- Merge conflicts present in:
  - `P2PLauncher\P2PLauncher.csproj`
  - `P2PLauncher\packages.config`
  - `changelog.md`
- Code notes: `Model\NetworkAdapter.Disable()` contains an ellipsis placeholder; needs implementation. Settings saved via `Properties.Settings.Default` are fine; avoid repeated Upgrade().

## 1) Strategy & Options
- Option A (Fast, minimal risk): Stay on .NET Framework 4.8; clean csproj; standardize to PackageReference; keep MSBuild on Windows hosts. Pros: least code churn. Cons: framework is legacy, fewer future updates.
- Option B (Recommended): Migrate to .NET 8 WPF (TFM `net8.0-windows`) with SDK-style project and `UseWPF=true`. Pros: supported long-term, modern toolchain, cross-CI via `dotnet` CLI, easier dependency mgmt. Cons: requires project file rewrite, add missing Windows packages, retest.

Pick one per your timeline. This guide covers both; do A first (stabilize) then B (modernize).

## 2) Prerequisites
- Windows with latest .NET SDK 8.x installed: `dotnet --info`.
- Git clean working tree. Create a branch.
- Admin PowerShell when testing service/adapter operations.

Commands are for Windows PowerShell 5.1.

## 3) Immediate Fixes (all options)
1) Resolve merge conflicts
   - Files: `P2PLauncher\P2PLauncher.csproj`, `P2PLauncher\packages.config`, `changelog.md`.
   - Keep Newtonsoft.Json at 13.0.3 consistently (either PackageReference or packages.config, not both).
2) Repair `NetworkAdapter.Disable()`
   - Replace the ellipsis with WMI `Disable` call mirroring `Enable()`, with `netsh` fallback.
3) Update VS Code launch
   - Point to the actual built exe. For .NET Framework: `bin/Debug/P2PLauncher.exe`. For .NET 8: `bin/Debug/net8.0-windows/P2PLauncher.exe`.

## 4) Option A — Stabilize on .NET Framework 4.8
1) Convert to PackageReference (recommended even on net48)
   - Remove `packages.config` and the `<Reference Include="Newtonsoft.Json" ...>` with `HintPath`.
   - Ensure a single `<ItemGroup><PackageReference Include="Newtonsoft.Json" Version="13.0.3" /></ItemGroup>` in the csproj.
   - Delete `packages/` folder from repo; restore on build.
2) Clean classic csproj
   - Keep `ToolsVersion` project but remove duplicate/legacy references and any merge markers.
   - Keep WPF imports and `ProjectTypeGuids` as-is.
3) Build & test locally
   - `dotnet restore`; if MSBuild is required use `msbuild P2PLauncher.sln /t:Build /p:Configuration=Debug`.
4) CI
   - Use `actions/setup-dotnet@v4` to install 8.x SDK (for CLI tasks) and `microsoft/setup-msbuild@v2` to build net48.
   - Replace deprecated set-output; use `$env:GITHUB_OUTPUT` file.

Pros: minimum code changes, quick win.

## 5) Option B — Migrate to .NET 8 WPF (Recommended)
1) Create SDK-style project
   - New top of `P2PLauncher\P2PLauncher.csproj`:
     - `<Project Sdk="Microsoft.NET.Sdk">`
     - `<PropertyGroup>`:
       - `<TargetFramework>net8.0-windows</TargetFramework>`
       - `<UseWPF>true</UseWPF>`
       - `<Nullable>enable</Nullable>`
       - `<ImplicitUsings>enable</ImplicitUsings>`
       - Optional: `<AssemblyName>P2PLauncher</AssemblyName>`, `<RootNamespace>P2PLauncher</RootNamespace>`
     - Remove `ToolsVersion`, `ProjectTypeGuids`, explicit `References`, `Import Microsoft.CSharp.targets`, and old `ApplicationDefinition` item grammar (SDK handles WPF automatically; keep `App.xaml` as `ApplicationDefinition`).
2) Add required Windows-only packages
   - Add PackageReference:
     - `System.Management`
     - `System.ServiceProcess.ServiceController`
     - `Newtonsoft.Json` (13.0.3) or migrate to `System.Text.Json` if desired.
3) App config
   - `App.config` continues to work on .NET 8 for config file redirection; if using `Settings.settings`, they remain valid. Consider `UserSecrets` only if needed.
4) Code audit for API changes
   - `ServiceController` and WMI usage remain; ensure admin prompts when needed (manifest or elevation path). Keep fallbacks to `net` and `netsh`.
5) Build & run
   - `dotnet restore; dotnet build -c Debug`.
   - Output: `P2PLauncher\bin\Debug\net8.0-windows\P2PLauncher.exe`.
6) Publishing options
   - Framework-dependent: `dotnet publish -c Release -r win-x64`.
   - Self-contained single-file (optional): `-p:PublishSingleFile=true -p:PublishTrimmed=false`.

## 6) Dependency Management
- Pin versions via `Directory.Packages.props` (optional) for centralized management.
- Check outdated: `dotnet list package --outdated`.
- Newtonsoft.Json at 13.0.3 is current; reevaluate yearly.
- If migrating to `System.Text.Json`, plan a separate refactor with converter parity.

## 7) CI/CD Modernization (`.github/workflows/dev_build.yml`)
- Replace with:
  - `actions/checkout@v4`.
  - `actions/setup-dotnet@v4` with `dotnet-version: 8.x`.
  - If Option A: add `microsoft/setup-msbuild@v2` and run `msbuild`.
  - If Option B: `dotnet build` and `dotnet publish`.
  - Zip using PowerShell `Compress-Archive` instead of third-party action.
  - Release with `softprops/action-gh-release@v2` or `gh release` CLI.
- Replace deprecated `set-output` with `$GITHUB_OUTPUT`.

## 8) VS Code Configuration
- `.vscode/launch.json` (WPF exe):
  - `type: coreclr`, `request: launch`, `program: <path to exe>`, `cwd: ${workspaceFolder}/P2PLauncher`.
- `.vscode/tasks.json`:
  - Build: `dotnet build P2PLauncher.sln`.
  - Watch (Option B): `dotnet watch --project P2PLauncher/P2PLauncher.csproj`.

## 9) Code Quality & Language Features
- Enable nullable and implicit usings (Option B).
- Add analyzers: `Microsoft.CodeAnalysis.NetAnalyzers` and enable `AnalysisMode AllEnabledByDefault`.
- Run `dotnet format` in CI.

## 10) Security & Privileges
- Service/adapter operations require elevation. Consider an app manifest requesting admin or add explicit elevation flow when needed.
- Sanitize any user-provided paths; avoid executing arbitrary commands.

## 11) Tests & Diagnostics
- Add basic unit tests around settings serialization and helper utilities.
- For integration, guard tests requiring admin with `Trait("RequiresAdmin", true)` and skip in CI.
- Logging: add a lightweight logger (e.g., `Microsoft.Extensions.Logging`) for diagnostics.

## 12) Step-by-Step Plan (Checklists)
A. Stabilize (Day 0)
- [ ] Resolve merge conflicts in csproj, packages.config, changelog.
- [ ] Choose Option A or B; create feature branch accordingly.
- [ ] Fix `NetworkAdapter.Disable()` implementation.
- [ ] Build locally with current target; update VS Code launch to point to exe.

B. Modernize (Day 1–2)
- [ ] If Option A: migrate to PackageReference, remove `packages/`.
- [ ] If Option B: rewrite csproj to SDK-style, set `net8.0-windows`, add `UseWPF`, add required packages.
- [ ] Enable nullable and analyzers.
- [ ] Update CI workflow to modern actions and `dotnet build/publish`.

C. Harden (Day 3)
- [ ] Add `dotnet list package --outdated` gate in CI.
- [ ] Add `dotnet test` with placeholder tests.
- [ ] Consider packaging options (self-contained vs framework-dependent).

D. Release
- [ ] Tag prerelease and verify artifact starts on a clean Windows VM.
- [ ] Update `README.md` and `changelog.md` with migration notes.

## 13) Known Pitfalls
- WPF on .NET 8 requires `UseWPF=true`; missing this causes XAML build failures.
- `System.Management` and `System.ServiceProcess.ServiceController` must be NuGet packages on .NET 8.
- Classic `App.config` binding redirects are not needed the same way on .NET 8.
- Admin elevation: without it, service/adapter commands will fail silently or throw.

## 14) Rollback Strategy
- Each step is a separate commit. To rollback, revert the last commit or re-target `net48` and restore the classic csproj.
- Keep `Option A` branch as a fallback if `Option B` hits unexpected blockers.

## 15) Ownership & Next Actions
- create agent as: one engineer for project structure, one for CI, one for runtime testing.
- Open issues:
  - Implement `NetworkAdapter.Disable()` WMI call.
  - Decide on Option A vs B.
  - Update `.vscode/launch.json` to real exe path.
  - Replace deprecated actions in workflow.

References:
- .NET 8 WPF docs: https://learn.microsoft.com/dotnet/desktop/wpf/
- SDK-style projects: https://learn.microsoft.com/dotnet/core/project-sdk/overview
- Windows-specific TFMs: https://learn.microsoft.com/dotnet/core/project-sdk/overview#use-platform-specific-apis
- Newtonsoft.Json: https://www.newtonsoft.com/json
