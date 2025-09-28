# P2PLauncher Build System Usage Guide

This document provides comprehensive usage instructions for the P2PLauncher build system.

## Prerequisites

- **Windows OS** (required for WPF components)
- **.NET 9 SDK** (automatically managed via global.json)
- **PowerShell Core 7.0+** (recommended) or Windows PowerShell 5.1+
- **Git** (for version stamping)

## Quick Start

```powershell
# Build everything in Release mode
.\scripts\build.ps1

# Publish everything with self-contained deployment
.\scripts\publish.ps1 -SelfContained
```

## Build Script Usage

### Basic Examples

```powershell
# Build all projects in Release mode
.\scripts\build.ps1

# Build only the client in Debug mode
.\scripts\build.ps1 -Target client -Configuration Debug

# Build with clean first
.\scripts\build.ps1 -Clean

# Build server only for specific runtime
.\scripts\build.ps1 -Target server -Runtime win-x86
```

### Parameters

| Parameter | Values | Default | Description |
|-----------|--------|---------|-------------|
| `-Target` | `all`, `client`, `server`, `standalone` | `all` | Which components to build |
| `-Configuration` | `Debug`, `Release` | `Release` | Build configuration |
| `-Runtime` | `win-x64`, `win-x86`, etc. | `win-x64` | Target runtime identifier |
| `-SelfContained` | Switch | `false` | Enable self-contained deployment |
| `-Clean` | Switch | `false` | Clean before building |

## Publish Script Usage

### Basic Examples

```powershell
# Publish all components in Release mode
.\scripts\publish.ps1

# Publish only client as self-contained
.\scripts\publish.ps1 -Target client -SelfContained

# Publish server for x86
.\scripts\publish.ps1 -Target server -Runtime win-x86
```

### Parameters

| Parameter | Values | Default | Description |
|-----------|--------|---------|-------------|
| `-Target` | `all`, `client`, `server`, `standalone` | `all` | Which components to publish |
| `-Configuration` | `Debug`, `Release` | `Release` | Build configuration |
| `-Runtime` | `win-x64`, `win-x86`, etc. | `win-x64` | Target runtime identifier |
| `-SelfContained` | Switch | `false` | Create self-contained deployment |

### Output Structure

Artifacts are created in the `artifacts/` directory:

```
artifacts/
├── client/
│   ├── Release/                    # Client binaries
│   └── P2PLauncher-client-Release-{version}.zip
├── server/
│   ├── Release/                    # Server binaries
│   ├── run-server.bat             # Windows batch launcher
│   ├── run-server.ps1             # PowerShell launcher
│   └── P2PLauncher-server-Release-{version}.zip
└── standalone/
    ├── Release/                    # Standalone binaries
    └── P2PLauncher-standalone-Release-{version}.zip
```

## Version Stamping

The build system automatically generates version information from Git:

- **Tagged commits**: Uses the exact tag (e.g., `v1.2.3`)
- **Development commits**: Uses format `dev-{short-hash}-{timestamp}`
- **No Git**: Uses format `unknown-{timestamp}`

Each published artifact includes a `version.json` file with build metadata.

## CI/CD Pipeline

The GitHub Actions workflow (`.github/workflows/build.yml`) automatically:

1. Sets up .NET 9 on Windows
2. Restores dependencies with caching
3. Builds all components
4. Runs tests
5. Publishes self-contained artifacts
6. Uploads build artifacts with retention

### Manual Workflow Trigger

```bash
# Trigger via GitHub CLI
gh workflow run build.yml

# Or use the GitHub web interface
```

## Local Development Workflow

### Initial Setup

```powershell
# Clone the repository
git clone https://github.com/NoranBits/P2PLauncher.git
cd P2PLauncher

# Verify .NET SDK
dotnet --version  # Should match global.json

# Initial build
.\scripts\build.ps1 -Clean
```

### Daily Development

```powershell
# Quick build for testing
.\scripts\build.ps1 -Target client -Configuration Debug

# Full build before commit
.\scripts\build.ps1 -Clean

# Create deployable artifacts
.\scripts\publish.ps1 -SelfContained
```

## Smoke Test Checklist

### Prerequisites Check
- [ ] Windows OS confirmed
- [ ] .NET 9 SDK installed and matches global.json
- [ ] PowerShell available (Core 7.0+ recommended)
- [ ] Git available for version stamping

### Build Verification
- [ ] `.\scripts\build.ps1` completes without errors
- [ ] All project targets build successfully
- [ ] No compilation warnings in Release mode
- [ ] `artifacts/` directory created

### Publish Verification
- [ ] `.\scripts\publish.ps1` completes without errors
- [ ] ZIP artifacts created in correct locations
- [ ] `version.json` files contain correct information
- [ ] Server launcher scripts included

### Runtime Verification
- [ ] Client executable launches (if on Windows)
- [ ] Server executable runs with `--help` flag
- [ ] Standalone executable functions independently
- [ ] Self-contained deployments work without .NET runtime

## Common Issues and Solutions

### SDK Version Mismatch

**Problem**: `NETSDK1045: The current .NET SDK does not support targeting .NET 9.0`

**Solution**:
```powershell
# Check installed SDKs
dotnet --list-sdks

# Install .NET 9 SDK if missing
# Download from: https://dotnet.microsoft.com/download/dotnet/9.0

# Verify global.json is respected
dotnet --version
```

### Locked Files During Build

**Problem**: `Access to the path 'file.exe' is denied`

**Solution**:
```powershell
# Stop any running instances
Get-Process P2PLauncher* | Stop-Process -Force

# Clean build directory
.\scripts\build.ps1 -Clean

# Use Process Explorer to identify locking processes
```

### Missing Git Version Information

**Problem**: Version shows as "unknown"

**Solution**:
```powershell
# Ensure Git is available
git --version

# Check repository status
git status

# For CI/CD, ensure fetch-depth: 0 in checkout action
```

### PowerShell Execution Policy

**Problem**: `Execution policy does not allow running scripts`

**Solution**:
```powershell
# Check current policy
Get-ExecutionPolicy

# Set policy for current user
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser

# Or bypass for single session
powershell -ExecutionPolicy Bypass -File .\scripts\build.ps1
```

### Antivirus False Positives

**Problem**: Antivirus deletes or quarantines executables

**Solution**:
- Add `artifacts/` directory to antivirus exclusions
- Use Windows Defender exclusions for development folder
- Whitelist specific executable signatures

### Network/NuGet Package Restore Issues

**Problem**: Package restore fails with network errors

**Solution**:
```powershell
# Clear NuGet caches
dotnet nuget locals all --clear

# Use specific NuGet source
dotnet restore --source https://api.nuget.org/v3/index.json

# Check proxy/firewall settings
```

## Advanced Scenarios

### Custom Runtime Targeting

```powershell
# Build for ARM64
.\scripts\publish.ps1 -Runtime win-arm64 -SelfContained

# Multiple runtimes
@('win-x64', 'win-x86', 'win-arm64') | ForEach-Object {
    .\scripts\publish.ps1 -Runtime $_ -SelfContained
}
```

### Integration with Other Tools

```powershell
# With MSBuild directly
msbuild P2PLauncher.sln /p:Configuration=Release /p:Platform="Any CPU"

# With dotnet CLI
dotnet build P2PLauncher.sln -c Release --verbosity detailed
```

## Support

For build system issues:
1. Check this documentation first
2. Verify prerequisites and common issues
3. Create GitHub issue with:
   - Operating system version
   - .NET SDK version (`dotnet --version`)
   - PowerShell version (`$PSVersionTable.PSVersion`)
   - Complete error output
   - Steps to reproduce