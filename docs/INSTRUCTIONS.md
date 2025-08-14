# P2PLauncher Modernization Playbook (net9)

This document describes the standardized workflows for building, publishing, and operating the modernized P2PLauncher (WPF client + headless server).

## Prerequisites
- Windows 10/11 with PowerShell 5.1 or PowerShell 7+
- .NET SDK 9.0.x installed
- Git (optionally GitHub account for CI)

## Scripts
Scripts live in `scripts/` and wrap common operations:

- Clean solution
  powershell -ExecutionPolicy Bypass -File scripts/clean.ps1

- Build client/server/all
  powershell -ExecutionPolicy Bypass -File scripts/build.ps1 -Target client -Configuration Release
  powershell -ExecutionPolicy Bypass -File scripts/build.ps1 -Target server -Configuration Release
  powershell -ExecutionPolicy Bypass -File scripts/build.ps1 -Target all -Configuration Release

- Publish (zips into `artifacts/`)
  powershell -ExecutionPolicy Bypass -File scripts/publish.ps1 -Target client -Configuration Release
  powershell -ExecutionPolicy Bypass -File scripts/publish.ps1 -Target server -Configuration Release
  powershell -ExecutionPolicy Bypass -File scripts/publish.ps1 -Target all -Configuration Release
  # Self-contained single-file example
  powershell -ExecutionPolicy Bypass -File scripts/publish.ps1 -Target server -Configuration Release -SelfContained -Runtime win-x64

Artifacts:
- Client: `artifacts/client/P2PLauncher-client-Release.zip`
- Server: `artifacts/server/P2PLauncher-server-Release.zip`

## CI (GitHub Actions)
A Windows workflow builds and publishes artifacts on pushes/PRs to `main` and `dev`.
- File: `.github/workflows/ci.yml`
- Outputs: zipped client/server under the workflow artifacts named `build-artifacts`.

## Logging, Exceptions, Security
- Logging: Serilog is configured in the server; client uses invariant-culture formatting and avoids secrets in logs.
- Exceptions: Avoid broad catch; prefer specific exceptions; surface friendly messages; log details for diagnostics.
- Security: No secrets committed; `.gitignore` covers keys/certs; validate user inputs; avoid echoing passwords.

## Local Development Tips
- Use the VS Code tasks or run scripts directly in PowerShell.
- Ensure FreeLAN is installed and detected by the client or set the path manually in settings.
- For hosting, ensure UDP 12000 is open/forwarded.

## Diagnostics & Feedback
- The client includes `FeedbackService` writing non-blocking logs to `logs/diagnostics.log`.
- Add tests in the UI via the "Diagnostics" panel: ping host, UDP port check (12000), adapter checks, and a log viewer.
- Server exposes a CLI status command that prints JSON health and recent warning/events. Integrate into CI to assert basic health.

## Standalone client
- To publish a self-contained client (Windows x64):
  powershell -ExecutionPolicy Bypass -File scripts/publish.ps1 -Target client -Configuration Release -SelfContained -Runtime win-x64
- Note: WPF single-file and trimming can break resource loading; prefer SelfContained=false or test thoroughly before enabling trimming.

## Next Steps (post-scripts/CI/docs)
- Implement live dashboard in client to display host/peers status.
- Enhance server CLI (password prompt/env variables, status tailing, JSON-structured logs).
- Expand tests and static analysis gating in CI.
