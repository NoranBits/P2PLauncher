# Build and run P2PLauncher.Client
param(
    [switch]$Run
)

Write-Host "Building P2PLauncher.Client..." -ForegroundColor Green

# Build the project
$buildResult = dotnet build "apps/P2PLauncher.Client/P2PLauncher.Client.csproj" --configuration Debug

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}

Write-Host "Build successful!" -ForegroundColor Green

if ($Run) {
    Write-Host "Running P2PLauncher.Client..." -ForegroundColor Yellow
    dotnet run --project "apps/P2PLauncher.Client/P2PLauncher.Client.csproj"
}

Write-Host "Done!" -ForegroundColor Green