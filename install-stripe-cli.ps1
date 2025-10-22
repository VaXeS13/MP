# Stripe CLI Installation Script for Windows
# This script downloads and installs Stripe CLI to a local directory

$ErrorActionPreference = "Stop"

Write-Host "Installing Stripe CLI..." -ForegroundColor Green

# Create installation directory in user profile (no admin needed)
$installDir = "$env:USERPROFILE\.stripe"
$binDir = "$installDir\bin"

if (!(Test-Path $installDir)) {
    New-Item -ItemType Directory -Path $installDir | Out-Null
}

if (!(Test-Path $binDir)) {
    New-Item -ItemType Directory -Path $binDir | Out-Null
}

# Download Stripe CLI for Windows (version 1.22.0)
$downloadUrl = "https://github.com/stripe/stripe-cli/releases/download/v1.22.0/stripe_1.22.0_windows_x86_64.zip"
$zipPath = "$installDir\stripe.zip"

Write-Host "Downloading Stripe CLI from GitHub..." -ForegroundColor Yellow
Invoke-WebRequest -Uri $downloadUrl -OutFile $zipPath

# Extract the archive
Write-Host "Extracting files..." -ForegroundColor Yellow
Expand-Archive -Path $zipPath -DestinationPath $binDir -Force

# Clean up
Remove-Item $zipPath

# Add to PATH for current session
$env:Path += ";$binDir"

Write-Host "`n✅ Stripe CLI installed successfully!" -ForegroundColor Green
Write-Host "`nLocation: $binDir\stripe.exe" -ForegroundColor Cyan
Write-Host "`nTo use Stripe CLI in future sessions, add to your PATH:" -ForegroundColor Yellow
Write-Host "  $binDir" -ForegroundColor White
Write-Host "`nOr run this command now (in PowerShell as Admin):" -ForegroundColor Yellow
Write-Host "  [Environment]::SetEnvironmentVariable('Path', `$env:Path + ';$binDir', 'User')" -ForegroundColor White

Write-Host "`nVerifying installation..." -ForegroundColor Yellow
& "$binDir\stripe.exe" --version

Write-Host "`n✅ Ready! You can now use: stripe login" -ForegroundColor Green
