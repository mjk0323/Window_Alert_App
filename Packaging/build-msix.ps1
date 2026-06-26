#Requires -Version 5.1
<#
.SYNOPSIS
    Window Alert App MSIX 패키지 빌드 스크립트
.DESCRIPTION
    1. dotnet publish (self-contained, win-x64)
    2. MSIX Assets 복사
    3. makeappx.exe 로 .msix 생성
    4. 자가 서명 인증서 생성 (최초 1회) 및 signtool 로 서명
.NOTES
    Windows SDK 설치 필요 (makeappx.exe, signtool.exe)
    실행: .\Packaging\build-msix.ps1
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# ── 경로 설정 ──────────────────────────────────────────────────────────────
$ProjectRoot   = Split-Path -Parent $PSScriptRoot
$PublishDir    = Join-Path $ProjectRoot "publish"
$PackagingDir  = $PSScriptRoot
$OutputMsix    = Join-Path $ProjectRoot "WindowAlertApp.msix"
$CertSubject   = "CN=WindowAlertApp"
$CertFile      = Join-Path $PackagingDir "WindowAlertApp_Certificate.cer"
$PfxFile       = Join-Path $PackagingDir "WindowAlertApp_Certificate.pfx"

# Windows SDK 경로 (버전이 다르면 수정)
$SdkRoot = "C:\Program Files (x86)\Windows Kits\10\bin"
$SdkBin  = Get-ChildItem $SdkRoot -Directory |
            Where-Object { $_.Name -match '^\d+\.\d+' } |
            Sort-Object Name -Descending |
            Select-Object -First 1 |
            ForEach-Object { Join-Path $_.FullName "x64" }

$MakeAppx = Join-Path $SdkBin "makeappx.exe"
$SignTool  = Join-Path $SdkBin "signtool.exe"

if (-not (Test-Path $MakeAppx)) {
    Write-Error "makeappx.exe 를 찾을 수 없습니다. Windows SDK 를 설치하세요.`n탐색 경로: $MakeAppx"
}

# ── 1. dotnet publish ──────────────────────────────────────────────────────
Write-Host "[1/4] dotnet publish ..." -ForegroundColor Cyan
if (Test-Path $PublishDir) { Remove-Item $PublishDir -Recurse -Force }
dotnet publish "$ProjectRoot\Window_Alert_App.csproj" `
    -c Release -r win-x64 --self-contained true `
    -p:PublishSingleFile=false `
    -o $PublishDir
Write-Host "      완료 → $PublishDir" -ForegroundColor Green

# ── 2. appxmanifest + Assets 복사 ─────────────────────────────────────────
Write-Host "[2/4] 패키지 파일 준비 ..." -ForegroundColor Cyan
$AssetsDir = Join-Path $PublishDir "Assets"
New-Item -ItemType Directory -Force -Path $AssetsDir | Out-Null

# 아이콘 파일이 없으면 빈 플레이스홀더 생성 (실제 배포 시 교체 필요)
$iconFiles = @(
    "Square44x44Logo.png",
    "Square150x150Logo.png",
    "Wide310x150Logo.png",
    "StoreLogo.png",
    "SplashScreen.png"
)
foreach ($icon in $iconFiles) {
    $dest = Join-Path $AssetsDir $icon
    if (-not (Test-Path $dest)) {
        # 1x1 투명 PNG (placeholder)
        $pngBytes = [byte[]](
            0x89,0x50,0x4E,0x47,0x0D,0x0A,0x1A,0x0A,
            0x00,0x00,0x00,0x0D,0x49,0x48,0x44,0x52,
            0x00,0x00,0x00,0x01,0x00,0x00,0x00,0x01,
            0x08,0x06,0x00,0x00,0x00,0x1F,0x15,0xC4,
            0x89,0x00,0x00,0x00,0x0A,0x49,0x44,0x41,
            0x54,0x78,0x9C,0x62,0x00,0x00,0x00,0x02,
            0x00,0x01,0xE5,0x27,0xDE,0xFC,0x00,0x00,
            0x00,0x00,0x49,0x45,0x4E,0x44,0xAE,0x42,
            0x60,0x82
        )
        [System.IO.File]::WriteAllBytes($dest, $pngBytes)
        Write-Host "      ⚠ 플레이스홀더 생성: $icon (실제 아이콘으로 교체 필요)" -ForegroundColor Yellow
    }
}

# Secrets 폴더 복사 (client_secrets.json 필요)
$SecretsDir = Join-Path $ProjectRoot "Secrets"
if (Test-Path $SecretsDir) {
    Copy-Item $SecretsDir (Join-Path $PublishDir "Secrets") -Recurse -Force
}

# appxmanifest 복사
Copy-Item (Join-Path $PackagingDir "Package.appxmanifest") $PublishDir -Force
Write-Host "      완료" -ForegroundColor Green

# ── 3. MSIX 생성 ──────────────────────────────────────────────────────────
Write-Host "[3/4] MSIX 생성 ..." -ForegroundColor Cyan
if (Test-Path $OutputMsix) { Remove-Item $OutputMsix -Force }
& $MakeAppx pack /d $PublishDir /p $OutputMsix /overwrite
Write-Host "      완료 → $OutputMsix" -ForegroundColor Green

# ── 4. 인증서 서명 ────────────────────────────────────────────────────────
Write-Host "[4/4] 코드 서명 ..." -ForegroundColor Cyan

if (-not (Test-Path $PfxFile)) {
    Write-Host "      자가 서명 인증서 생성 중..." -ForegroundColor Yellow
    $cert = New-SelfSignedCertificate `
        -Type CodeSigningCert `
        -Subject $CertSubject `
        -KeyAlgorithm RSA `
        -KeyLength 2048 `
        -CertStoreLocation "Cert:\CurrentUser\My" `
        -FriendlyName "Window Alert App Code Signing"

    # .cer 내보내기 (사용자가 신뢰 루트에 설치할 파일)
    Export-Certificate -Cert $cert -FilePath $CertFile | Out-Null

    # .pfx 내보내기 (서명용, 비밀번호 없음)
    $emptyPwd = New-Object System.Security.SecureString
    Export-PfxCertificate -Cert $cert -FilePath $PfxFile -Password $emptyPwd | Out-Null

    Write-Host "      인증서 생성 완료:" -ForegroundColor Green
    Write-Host "        .cer → $CertFile" -ForegroundColor Green
    Write-Host "        .pfx → $PfxFile" -ForegroundColor Green
    Write-Host ""
    Write-Host "  ★ 사용자 설치 전 안내 ★" -ForegroundColor Yellow
    Write-Host "  WindowAlertApp_Certificate.cer 를 더블클릭 →" -ForegroundColor Yellow
    Write-Host "  [인증서 설치] → 로컬 컴퓨터 → 신뢰할 수 있는 루트 인증 기관" -ForegroundColor Yellow
}

& $SignTool sign /fd SHA256 /p7 . /p7co 1.2.840.113549.1.7.1 /p7ce DetachedSignedData `
    /f $PfxFile /p "" $OutputMsix
Write-Host "      서명 완료" -ForegroundColor Green

Write-Host ""
Write-Host "=====================================================" -ForegroundColor Cyan
Write-Host "  빌드 완료!" -ForegroundColor Green
Write-Host "  패키지: $OutputMsix" -ForegroundColor Green
Write-Host "  인증서: $CertFile (사용자에게 함께 배포)" -ForegroundColor Green
Write-Host "=====================================================" -ForegroundColor Cyan
