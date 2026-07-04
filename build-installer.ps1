# Build script for SettingsIE installer
# Requires .NET SDK 8+ and Inno Setup 6+

Write-Host "=== Building SettingsIE installer ===" -ForegroundColor Cyan

# Step 1: Publish as self-contained single-file
Write-Host "[1/2] Publishing application..." -ForegroundColor Yellow
$pubArgs = @(
    "publish", "-c", "Release", "-r", "win-x64",
    "--self-contained", "true",
    "-p:PublishSingleFile=true",
    "-p:IncludeNativeLibrariesForSelfExtract=true",
    "-p:DebugType=none",
    "-p:TrimMode=link"
)

$process = Start-Process -FilePath "dotnet" -ArgumentList $pubArgs -NoNewWindow -Wait -PassThru
if ($process.ExitCode -ne 0) {
    Write-Host "Publish failed!" -ForegroundColor Red
    exit $process.ExitCode
}
Write-Host "Publish OK" -ForegroundColor Green

# Step 2: Compile Inno Setup script
Write-Host "[2/2] Compiling installer..." -ForegroundColor Yellow
$isccPaths = @(
    "${env:ProgramFiles}\Inno Setup 6\iscc.exe",
    "${env:ProgramFiles(x86)}\Inno Setup 6\iscc.exe",
    "${env:ProgramFiles}\Inno Setup 5\iscc.exe",
    "${env:ProgramFiles(x86)}\Inno Setup 5\iscc.exe"
)

$iscc = $null
foreach ($p in $isccPaths) {
    if (Test-Path $p) { $iscc = $p; break }
}

if (-not $iscc) {
    Write-Host "Inno Setup not found!" -ForegroundColor Red
    Write-Host "Download from https://jrsoftware.org/isdl.php" -ForegroundColor Yellow
    exit 1
}

$process2 = Start-Process -FilePath $iscc -ArgumentList "installer.iss" -NoNewWindow -Wait -PassThru
if ($process2.ExitCode -ne 0) {
    Write-Host "Installer compilation failed!" -ForegroundColor Red
    exit $process2.ExitCode
}

Write-Host "=== Done! ===" -ForegroundColor Cyan
Write-Host "Installer: .\publish\SettingsIE_setup.exe" -ForegroundColor Green
