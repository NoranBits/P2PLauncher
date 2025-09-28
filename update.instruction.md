---
applyTo: workspace
owner: NoranBits
repo: P2PLauncher
branch: newUpdated
updated: 2025-08-14
---
# P2PLauncher Modernization & Upgrade Solutions Playbook (net9)

Goal: Keep the app buildable, secure, and maintainable on current toolchains. Target .NET 9 (Windows), WPF for the client, and a headless server CLI. Enforce Microsoft quality rules, deterministic builds, and simple automation (scripts + CI).

Contents:
- 0) Current State Snapshot
- 1) Track Overview (A → legacy, B → current)
- 2) Immediate Cleanup (apply now)
- 3) Track B — .NET 9 WPF/CLI (current state + key settings)
- 4) Dependency Hygiene & Central Management
- 5) VS Code Config (tasks/launch)
- 6) CI/CD (GitHub Actions)
- 7) Testing, Formatting, and Analyzers
- 8) .NET Fundamentals Best Practices (Required)
- 9) UI Modernization (Applied)
- 10) Validation Checklist
- 11) Rollback & Recovery

## 0) Current State Snapshot
- Solution: `P2PLauncher.sln`
  - Client (WPF): `P2PLauncher/` → `net9.0-windows`, `<UseWPF>true</UseWPF>`
  - Server (CLI): `apps/P2PLauncher.Server/` → `net9.0-windows`, System.CommandLine + Serilog
  - Core (shared enums): `libs/P2PLauncher.Core/`
- Automation: PowerShell scripts under `scripts/` (clean/build/publish). VS Code tasks wired. CI on Windows builds and zips artifacts.
- Legacy item: `P2PLauncher/packages.config` remains from pre-migration (redundant under SDK-style). Keep removed from build; safe to delete.

## 1) Track Overview
- Track A (legacy .NET Framework 4.8): no longer primary. Only useful for historical reference.
- Track B (current): SDK-style .NET 9 projects for client, server, and core with PackageReference and analyzers.

## 2) Immediate Cleanup (apply now)
1) Remove legacy NuGet config in client project
   - Delete `P2PLauncher/packages.config` (SDK-style uses PackageReference).
2) Ensure ignores
   - `.gitignore` updated for bin/obj/.vs/artifacts/logs/keys.
3) Align metadata
   - Ensure `ApplicationIcon`, `AssemblyName`, `RootNamespace` set in each csproj.
4) Verify Windows-only packages
   - Client: `System.Management` 9.0.8, `System.ServiceProcess.ServiceController` 9.0.8.
5) Confirm scripts
   - `scripts/clean.ps1`, `scripts/build.ps1`, `scripts/publish.ps1` present and executable.

## 3) Track B — .NET 9 WPF/CLI (current state + key settings)
- Client csproj (WPF):
  - `<TargetFramework>net9.0-windows</TargetFramework>` and `<UseWPF>true</UseWPF>`.
  - PackageReference includes `Newtonsoft.Json (13.0.3)`, `System.Management (9.0.8)`, `System.ServiceProcess.ServiceController (9.0.8)`.
  - Nullable + implicit usings enabled.
- Server csproj (CLI):
  - References `System.CommandLine (beta)`, `Serilog`, `Serilog.Sinks.Console`, `Serilog.Sinks.File`, `System.Management`.
  - Global exception hooks and structured logging recommended.
- Core csproj:
  - Shared enums only. Add analyzers; avoid UI/settings/exception classes here.
- WPF specifics:
  - Do not enable trimming or single-file publish for the client. Prefer self-contained=false by default.

## 4) Dependency Hygiene & Central Management
- Optional central file at repo root: `Directory.Packages.props` to pin versions uniformly (Newtonsoft.Json, System.Management, ServiceController, analyzers).
- Audit/update regularly:
  - powershell -NoProfile -Command "dotnet list package --outdated"
- Prefer stable releases; keep `System.CommandLine` beta pinned until GA and track release notes.

## 5) VS Code Config (tasks/launch)
- Tasks call scripts:
  - Build all: use task `build:all` (Release)
  - Build client/server: `build:client`, `build:server`
  - Publish all: `publish:all` → zips under `artifacts/`
  - Self-contained server: `publish:server:self-contained` (win-x64)
- Launch:
  - `launch.json` points to `P2PLauncher/bin/Debug/net9.0-windows/P2PLauncher.exe`.

## 6) CI/CD (GitHub Actions)
- Workflow: `.github/workflows/ci.yml`
  - Runner: `windows-latest`, `actions/setup-dotnet@v4` with `dotnet-version: 9.0.x`.
  - Steps: restore → build (Release) → publish client/server → zip → upload artifacts.
- Extensions:
  - Add release job on tags to attach zips to GitHub Releases (optional).

## 7) Testing, Formatting, and Analyzers (refined)
Align analyzer and testing guidance to the official .NET Fundamentals guidance (code-analysis, diagnostics, and quality rules). See: https://learn.microsoft.com/dotnet/fundamentals/

1) Enable analyzers across projects
- Add to client, server, and core projects:
  - <PackageReference Include="Microsoft.CodeAnalysis.NetAnalyzers" Version="9.0.0" PrivateAssets="all" />
  - Set `<AnalysisMode>AllEnabledByDefault</AnalysisMode>` in Core and Client (or via Directory.Build.props)

2) EditorConfig (root `.editorconfig`) — concrete, actionable snippet
- Add a root `.editorconfig` to control rule severities and coding styles. Example:

```
root = true

[*.{cs}]
# treat style diagnostics as warnings by default
dotnet_analyzer_diagnostic.category-Style.severity = warning
# escalate globalization and reliability categories to warnings
dotnet_analyzer_diagnostic.category-Globalization.severity = warning
dotnet_analyzer_diagnostic.category-Reliability.severity = warning

# Individual overrides
dotnet_diagnostic.CA1822.severity = none # allow instance methods where analyzer suggests static
dotnet_diagnostic.CA1031.severity = warning # prefer specific exception catches; warn instead of fail
dotnet_diagnostic.CA2100.severity = warning # SQL injection / command text diagnostic (if applicable)

# code style examples
csharp_style_var_for_built_in_types = true:suggestion
csharp_prefer_braces = true:warning

# nullable and null check guidance
dotnet_diagnostic.CA1062.severity = warning

```

3) Map common rules to actions (practical remediation)
- CA1031 (catch-all Exception): replace broad catches with specific exceptions (IOException, UnauthorizedAccessException, JsonException, HttpRequestException) or rethrow; preserve logging and minimal user-facing messaging. When unavoidable, rethrow after logging:
  try { ... } catch (SpecificException ex) { Log.Warning(...); throw; }
- CA1305/CA1304/CA1307 (Globalization & StringComparison): use CultureInfo.InvariantCulture for logs/parsing; use StringComparison.Ordinal/OrdinalIgnoreCase overloads for string operations.
- CA1001/CA2000 (Dispose): make types owning unmanaged resources implement IDisposable and dispose of streams/processes/timers using `using` or explicit Dispose.
- CA1002 (ICollection API design): expose `IReadOnlyList<T>` instead of `List<T>` where appropriate.

4) CI integration (enforcement strategy)
- On PRs: run `dotnet build -c Release` and `dotnet test` (when tests added).
- Optional gating: run `dotnet format --verify-no-changes` and `dotnet build` with analyzers enabled; allow existing warnings to remain while failing on newly introduced critical analyzer errors (use baseline strategy).

5) Testing guidance
- Start with unit tests for core logic (NetworkAdapters, AddressHelper, EnvHelper, EnumHelper). Use xUnit or MSTest.
- Add integration tests for FreeLanService behaviors using a test double or local FreeLAN binary in CI (optional). Keep tests small and deterministic.
- Use code coverage tooling (coverlet) and target ≥80% for critical modules over time.

6) Diagnostics & Logging (align with .NET fundamentals)
- Follow .NET guidance for logging and diagnostics: centralize logs (Serilog for server; in-client use a lightweight logger).
- Structure logs with fields (host, peerId, operation, duration). Avoid logging secrets (passwords, private keys). Use `CultureInfo.InvariantCulture` for message formatting.
- Add health-check and diagnostic endpoints or commands (server CLI) that return JSON status and recent events.

7) Performance and reliability
- Prefer async/await for IO (HttpClient with single instance) and avoid blocking calls on UI thread.
- Use `Array.Empty<T>()`, `Span<T>`/`Memory<T>` where appropriate, and memoize expensive computations.

## 7.1) Formatting, Analyzers & CI enforcement (concrete)

Follow .NET fundamentals for formatting and code-analysis. Add these developer commands and CI steps to keep the tree clean and predictable:

- Install dotnet-format (if not present):
  dotnet tool install -g dotnet-format

- Local checks (developer):
  dotnet format            # apply formatter and remove unused usings
  dotnet format --verify-no-changes  # CI-style check that fails if formatting differs
  dotnet build -c Release
  dotnet test               # run tests when added

- Recommended CI snippets (GitHub Actions):
  - name: Install dotnet-format
    run: dotnet tool install -g dotnet-format
  - name: Verify formatting
    run: dotnet format --verify-no-changes
  - name: Build (Release)
    run: dotnet build P2PLauncher.sln -c Release --no-restore

- Analyzer strategy
  - Enable Microsoft.CodeAnalysis.NetAnalyzers across projects and prefer `AnalysisMode=AllEnabledByDefault` in `Directory.Build.props`.
  - Use a baseline strategy for long-running legacy warnings: generate a suppressions file for historical warnings and enforce NO NEW critical diagnostics in CI.
  - Escalate reliability and security categories to fail CI when possible.

Rationale: This mirrors guidance in the .NET fundamentals docs (formatting, analyzers, and CI). Use `dotnet format` in pre-commit hooks or CI to keep code consistent and to reduce noisy IDE-only diffs.

References:
- https://learn.microsoft.com/dotnet/fundamentals/code-analysis/overview
- https://learn.microsoft.com/dotnet/core/project-sdk/overview

## 8) .NET Fundamentals Best Practices (Selected rules)
- Globalization: prefer `CultureInfo.InvariantCulture` for logs and parsing that isn't user-facing (CA1305).
- Use recommended `StringComparison` overloads (CA1307).
- Avoid broad `catch (Exception)` (CA1031) — catch specific exceptions or rethrow.
- Ensure disposable fields implement IDisposable or are disposed (CA1001/CA2000).
- Prefer `IReadOnlyList<T>` over `List<T>` in public APIs (CA1002).
- Use `ArgumentNullException.ThrowIfNull` for parameter validation.

## 9) UI Modernization (Applied)
- Clear Host vs Client flows, validation hints, optional debug/log surface.
- Persist client defaults via `UserPreferencesService` (JSON). Avoid storing secrets in plain text for production scenarios.
- Future: adopt a lightweight theme package if desired.

## 10) Validation Checklist
- [ ] Local build succeeds (Debug/Release) via scripts and VS Code tasks
- [ ] Client launches, FreeLAN path detection works, adapters/services lists render
- [ ] Server CLI runs with logging and exits cleanly on Ctrl+C
- [ ] Artifacts zipped under `artifacts/client|server`
- [ ] CI green on PRs to `main`/`dev`
- [ ] Analyzers produce no new critical warnings

## 11) Rollback & Recovery
- Branch-per-change with clear commit messages. Use tags for known good versions.
- To revert migration, restore classic csproj from history and retarget `net48` (Track A). Keep as last resort only.

## Diagnostics, Feedback, and Standalone client
- FeedbackService (client): writes best-effort logs to `logs/diagnostics.log`. It catches filesystem/permission issues and avoids crashing the UI.
- Client diagnostics panel: add a new WPF `DiagnosticsWindow` offering:
  - Ping host (ICMP or TCP handshake fallback) with a latency histogram and warning when packet loss > 5%.
  - UDP 12000 port tester to check reachability toward host (best-effort; may require server cooperation).
  - Adapter checks and TAP health indicators.
  - "Run all tests" button that executes the checks and aggregates a pass/fail result.
- Server health checks: implement a `--status` command that returns JSON with recent peer events, open ports, and last error messages. Log at Warning level on connectivity failures.

## Changelog guidance
- Update `changelog.md` with an entry for this modernization: analyzers, scripts, diagnostics panel, server CLI, standalone client.

Notes & Pitfalls
- WPF requires `<UseWPF>true</UseWPF>`; missing it breaks XAML compile.
- Some adapter/service operations require elevation. Test as Administrator.
- Avoid trimming/single-file for WPF; it often breaks resource loading.

References
- WPF: https://learn.microsoft.com/dotnet/desktop/wpf/
- SDK-style projects: https://learn.microsoft.com/dotnet/core/project-sdk/overview
- Platform-specific APIs: https://learn.microsoft.com/dotnet/core/project-sdk/overview#use-platform-specific-apis
- Newtonsoft.Json: https://www.newtonsoft.com/json
- Code Guidelines: https://learn.microsoft.com/dotnet/fundamentals/