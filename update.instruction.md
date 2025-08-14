---
applyTo: workspace
owner: NoranBits
repo: P2PLauncher
branch: updated
updated: 2025-08-10
---
# P2PLauncher Modernization & Upgrade Solutions Playbook

Goal: Make the app buildable, runnable, and maintainable on current toolchains; update dependencies; resolve inconsistencies; and migrate to .NET 9 (Windows) WPF following .NET Fundamentals best practices.

Contents:
- 0) Current State Snapshot
- 1) Decision Matrix (Track A: net48 stabilize, Track B: .NET 9 migrate)
- 2) Quick Fix Now (unblocks builds immediately)
- 3) Track B — .NET 9 WPF Migration (recommended)
- 4) Dependency Hygiene & Central Management
- 5) VS Code Config (launch/tasks)
- 6) CI/CD (GitHub Actions)
- 7) Testing & Formatting
- 8) .NET Fundamentals Best Practices (Required)
- 9) UI Modernization (Applied)
- 10) Validation Checklist
- 11) Rollback & Recovery

## 0) Current State Snapshot
- Solution: `P2PLauncher.sln` (single WPF app)
- Target framework: .NET Framework 4.8 (`TargetFrameworkVersion=v4.8`)
- Packages: `packages.config` pins `Newtonsoft.Json` 12.0.3, but repo includes `packages/Newtonsoft.Json.13.0.3` → mismatch
- Project references: classic `HintPath` pointing to 12.0.3
- VS Code: `.vscode/launch.json` is a placeholder (invalid WARNINGxx props), `.vscode/tasks.json` not invoking solution properly
- CI: workflows use old actions (`checkout@v2`, `setup-msbuild@v1.0.2`) and incomplete steps

## 1) Decision Matrix
- Track A (fast): keep .NET Framework 4.8; fix package mismatch; standardize to PackageReference; modernize CI; keep code unchanged otherwise
- Track B (recommended): migrate to SDK-style .NET 9 WPF (`net9.0-windows` + `UseWPF=true`); add Windows packages; modernize CI fully

Pick Track A to unblock quickly, then proceed to Track B on a feature branch.

## 2) Quick Fix Now (apply first)
Purpose: make builds deterministic in current state.

1) Align Newtonsoft.Json to 13.0.3
- Update `P2PLauncher/packages.config` to:
```
<?xml version="1.0" encoding="utf-8"?>
<packages>
  <package id="Newtonsoft.Json" version="13.0.3" targetFramework="net48" />
</packages>
```
- Update `P2PLauncher/P2PLauncher.csproj` HintPath to point to `..\packages\Newtonsoft.Json.13.0.3\lib\net45\Newtonsoft.Json.dll`

2) Fix VS Code launch.json (remove WARNINGxx, set program path)
- For net48 (temporary), use a simple external launch:
```
{
  "version": "0.2.0",
  "configurations": [
    {
      "name": "Run P2PLauncher (net48 exe)",
      "type": "csharp",
      "request": "launch",
      "program": "${workspaceFolder}/P2PLauncher/bin/Debug/P2PLauncher.exe",
      "cwd": "${workspaceFolder}/P2PLauncher",
      "console": "externalTerminal"
    }
  ]
}
```
Note: Some VS Code C# extensions only support .NET (Core). If debugging doesn't attach on net48, run without debugger or attach to process. Track B resolves this.

3) Fix tasks.json to call the solution explicitly
```
{
  "version": "2.0.0",
  "tasks": [
    {
      "label": "build",
      "type": "process",
      "command": "dotnet",
      "args": ["build", "${workspaceFolder}/P2PLauncher.sln", "/property:GenerateFullPaths=true", "/consoleloggerparameters:NoSummary;ForceNoAlign"],
      "problemMatcher": "$msCompile"
    },
    {
      "label": "publish",
      "type": "process",
      "command": "dotnet",
      "args": ["publish", "${workspaceFolder}/P2PLauncher.sln", "/property:GenerateFullPaths=true", "/consoleloggerparameters:NoSummary;ForceNoAlign"],
      "problemMatcher": "$msCompile"
    }
  ]
}
```

Commit: "fix: align newtonsoft 13.0.3, repair launch/tasks for local build"

---

## 3) Track B — .NET 9 WPF Migration (SDK-style)
Objective: long-term support and clean tooling.

1) Replace `P2PLauncher.csproj` with SDK-style
```
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0-windows</TargetFramework>
    <UseWPF>true</UseWPF>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <AssemblyName>P2PLauncher</AssemblyName>
    <RootNamespace>P2PLauncher</RootNamespace>
    <ApplicationIcon>community-symbol.ico</ApplicationIcon>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
    <PackageReference Include="System.Management" Version="9.0.0" />
    <PackageReference Include="System.ServiceProcess.ServiceController" Version="9.0.0" />
    <PackageReference Include="Microsoft.CodeAnalysis.NetAnalyzers" Version="9.0.0" PrivateAssets="all" />
  </ItemGroup>
  <ItemGroup>
    <Resource Include="community-symbol.ico" />
  </ItemGroup>
</Project>
```
Notes:
- SDK-style auto-includes `*.cs`, `*.xaml`. Remove explicit `<Compile>`/`<Page>` lists from classic file
- Keep `App.config` and `Settings.settings` (they work; adjust if needed later)

2) Build & run
- `dotnet restore; dotnet build -c Debug`
- Output: `P2PLauncher/bin/Debug/net9.0-windows/P2PLauncher.exe`

3) Update VS Code launch for .NET 9
```
{
  "version": "0.2.0",
  "configurations": [
    {
      "name": "Run P2PLauncher (.NET 9)",
      "type": "coreclr",
      "request": "launch",
      "program": "${workspaceFolder}/P2PLauncher/bin/Debug/net9.0-windows/P2PLauncher.exe",
      "cwd": "${workspaceFolder}/P2PLauncher",
      "console": "internalConsole"
    }
  ]
}
```
4) Validate Windows-only APIs
- `System.Management` and `ServiceController` come via NuGet packages as referenced above
- Admin tasks: If service/adapter operations fail, run the exe elevated (manifest or prompt)

Commit: "feat: migrate to SDK-style net9.0-windows WPF"

---

## 4) Dependency Hygiene & Central Management
- Centralize with optional `Directory.Packages.props` at repo root:
```
<Project>
  <ItemGroup>
    <PackageVersion Include="Newtonsoft.Json" Version="13.0.3" />
    <PackageVersion Include="System.Management" Version="9.0.0" />
    <PackageVersion Include="System.ServiceProcess.ServiceController" Version="9.0.0" />
    <PackageVersion Include="Microsoft.CodeAnalysis.NetAnalyzers" Version="9.0.0" />
  </ItemGroup>
</Project>
```
- Check updates regularly: `dotnet list package --outdated`

## 5) VS Code Config
- launch.json: Use the snippets above for your chosen track
- tasks.json: use explicit solution args (see Quick Fix)

## 6) CI/CD (GitHub Actions)
Replace both `dev_build.yml` and `prod_build.yml` with a single parameterized workflow (example simplified):
```
name: CI
on:
  push:
    branches: [ updated ]
    tags:
      - "*.debug"
      - "*.prod"
  workflow_dispatch:

jobs:
  build:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '9.0.x'
      - name: Build
        shell: pwsh
        run: |
          dotnet restore .\P2PLauncher.sln
          dotnet build .\P2PLauncher.sln -c Release --nologo
      - name: Publish (win-x64)
        if: startsWith(github.ref, 'refs/tags/')
        shell: pwsh
        run: |
          dotnet publish .\P2PLauncher\P2PLauncher.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=false -o out
          Compress-Archive -Path out\* -DestinationPath artifact.zip -Force
      - name: Upload Artifact
        if: startsWith(github.ref, 'refs/tags/')
        uses: actions/upload-artifact@v4
        with:
          name: P2PLauncher
          path: artifact.zip
```
For Track A add `microsoft/setup-msbuild@v2` and call `msbuild` if `dotnet build` fails.

## 7) Testing & Formatting
- Add test project later (MSTest or xUnit); for now add format/analyzers in CI:
  - `dotnet tool update -g dotnet-format`
  - `dotnet format --verify-no-changes` (or `--severity info` initially)
- Enable analyzers via PackageReference (included above) and consider ruleset:
```
<PropertyGroup>
  <AnalysisMode>AllEnabledByDefault</AnalysisMode>
</PropertyGroup>
```

## 8) .NET Fundamentals Best Practices (Required)
Adopt these across the solution. References: dotnet/fundamentals (globalization, diagnostics, code analysis, performance).

- Code Analysis & EditorConfig
  - Keep `<AnalysisMode>AllEnabledByDefault</AnalysisMode>` and add a root `.editorconfig` to set severity (e.g., CA1305/CA1307 warning, CA2000 error, CA1822 silent). Enable nullable and implicit usings (already set in csproj).
- Globalization & Culture
  - Use `CultureInfo.InvariantCulture` for formatting/parsing not intended for UI. Prefer `string.Contains(..., StringComparison.Ordinal)` and explicit `OrdinalIgnoreCase` where needed.
- Networking
  - Replace `WebClient` with a single, long-lived `HttpClient` (done: `EnvHelper.GetPublicAddress` now uses `HttpClient` with Uri overload). Set `Timeout` and catch `HttpRequestException`/`TaskCanceledException`.
- Resource Management & Dispose
  - Implement proper dispose patterns for types owning timers/processes/streams (done: `MainLauncherWindow` + `FreeLanService`). Prefer `using` on `Process.Start` results.
- Exceptions & Input Validation
  - Validate public parameters (use `ArgumentNullException.ThrowIfNull`). Avoid broad `catch (Exception)`; catch specific exceptions.
- Localization
  - Add `NeutralResourcesLanguage("en-US")` to assembly (AssemblyInfo or csproj). Use resources for UI text if localizing later.
- Packaging
  - Prefer `SelfContained=false` for WPF; consider `PublishSingleFile=false` to avoid loading issues with WPF/XAML on trim. Do not enable trimming for WPF.

## 9) UI Modernization (Applied)
Simplify flows and reduce user error.

- Separate Host vs Client tabs with clear labels. Removed advanced Hub tab from main flow.
- One-click client start: Host IPv4, Id, Password, Relay checkbox, Start button.
- Convenience: "Paste" button for IPv4; auto-prefill from Clipboard if it looks like IPv4.
- Persistence: Added `UserPreferencesService` (JSON) to save client defaults (Host/Password/Id/Relay/Debug) when "Save for next time" is checked.
- Diagnostics: Optional Debug screen, Open logs button.

Follow-ups:
- Style pass with modern WPF themes (e.g., MahApps/FluentWPF) if desired.
- Add basic validation highlights (red border) for invalid IPv4/Id ranges.

## 10) Validation Checklist
- [ ] Local build succeeds (Debug/Release)
- [ ] App launches and basic UI renders
- [ ] Newtonsoft.Json resolves from NuGet, no `packages/` in repo (if using PackageReference)
- [ ] VS Code debug/run works for chosen track
- [ ] CI builds on `windows-latest` and uploads artifact on tag

## 11) Rollback & Recovery
- Each major step in its own commit/branch
- To rollback migration, restore classic csproj from prior commit and retarget `net48`
- Keep Track A branch as a long-lived fallback until Track B is proven

---

Notes & Pitfalls
- WPF on .NET 9 requires `<UseWPF>true</UseWPF>`; missing this breaks XAML build
- Ensure `System.Management` and `System.ServiceProcess.ServiceController` are explicitly referenced via NuGet on .NET 9
- Admin privileges: service/network operations often require elevation; test as Admin

References
- WPF on .NET: https://learn.microsoft.com/dotnet/desktop/wpf/
- SDK-style projects: https://learn.microsoft.com/dotnet/core/project-sdk/overview
- Windows-specific TFMs: https://learn.microsoft.com/dotnet/core/project-sdk/overview#use-platform-specific-apis
- Newtonsoft.Json: https://www.newtonsoft.com/json
