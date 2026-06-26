#Requires -Version 5.1
<#
.SYNOPSIS
    Window Alert App Inno Setup 인스톨러 빌드 스크립트
.DESCRIPTION
    1. dotnet publish (self-contained, win-x64, Release)
    2. Secrets 폴더 복사 (client_secrets.json)
    3. ISCC.exe 로 WindowAlertApp_Setup.exe 생성
.NOTES
    Inno Setup 6 설치 필요: https://jrsoftware.org/isdl.php
    실행: .\Packaging\build-innosetup.ps1
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$ProjectRoot = Split-Path -Parent $PSScriptRoot
$PublishDir  = Join-Path $ProjectRoot "publish"

# ── 1. dotnet publish ──────────────────────────────────────────────────────
Write-Host "[1/3] dotnet publish ..." -ForegroundColor Cyan
if (Test-Path $PublishDir) { Remove-Item $PublishDir -Recurse -Force }
dotnet publish "$ProjectRoot\Window_Alert_App.csproj" `
    -c Release -r win-x64 --self-contained true `
    -p:PublishSingleFile=false `
    -o $PublishDir
Write-Host "      완료 → $PublishDir" -ForegroundColor Green

# ── 2. Secrets 복사 ────────────────────────────────────────────────────────
Write-Host "[2/3] Secrets 복사 ..." -ForegroundColor Cyan
$SecretsDir = Join-Path $ProjectRoot "Secrets"
if (Test-Path $SecretsDir) {
    Copy-Item $SecretsDir (Join-Path $PublishDir "Secrets") -Recurse -Force
    Write-Host "      완료" -ForegroundColor Green
} else {
    Write-Warning "Secrets 폴더를 찾을 수 없습니다: $SecretsDir"
}

# ── 3. Inno Setup 빌드 ────────────────────────────────────────────────────
Write-Host "[3/3] Inno Setup 빌드 ..." -ForegroundColor Cyan
$ISCC = "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe"
if (-not (Test-Path $ISCC)) {
    $ISCC = "${env:ProgramFiles}\Inno Setup 6\ISCC.exe"
}
if (-not (Test-Path $ISCC)) {
    Write-Error "Inno Setup 6를 찾을 수 없습니다.`n설치 후 재실행: https://jrsoftware.org/isdl.php"
}
& $ISCC "$PSScriptRoot\WindowAlertApp.iss"
Write-Host "      완료" -ForegroundColor Green

Write-Host ""
Write-Host "=====================================================" -ForegroundColor Cyan
Write-Host "  빌드 완료!" -ForegroundColor Green
Write-Host "  인스톨러: $ProjectRoot\WindowAlertApp_Setup.exe" -ForegroundColor Green
Write-Host "=====================================================" -ForegroundColor Cyan
