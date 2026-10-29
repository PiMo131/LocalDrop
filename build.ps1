<#
.SYNOPSIS
  Builds LocalDrop: Windows setup.exe and/or Android APK.

.EXAMPLE
  .\build.ps1                # both
  .\build.ps1 -Windows       # only Windows installer -> dist\LocalDrop-Setup-<ver>.exe
  .\build.ps1 -Android       # only Android APK       -> dist\LocalDrop-<ver>.apk
#>
param(
    [switch]$Windows,
    [switch]$Android
)
$ErrorActionPreference = "Stop"
if (-not $Windows -and -not $Android) { $Windows = $true; $Android = $true }

$root = $PSScriptRoot
$proj = Join-Path $root "LocalDrop\LocalDrop.csproj"
$dist = Join-Path $root "dist"
New-Item -ItemType Directory -Force $dist | Out-Null

# Version from csproj
[xml]$csproj = Get-Content $proj
$version = ($csproj.Project.PropertyGroup | Where-Object { $_.ApplicationDisplayVersion } | Select-Object -First 1).ApplicationDisplayVersion

if ($Windows) {
    Write-Host "== Windows publish ==" -ForegroundColor Cyan
    dotnet publish $proj -f net8.0-windows10.0.19041.0 -c Release -p:Platform=x64
    if ($LASTEXITCODE) { throw "dotnet publish (windows) failed" }

    $publish = Join-Path $root "LocalDrop\bin\x64\Release\net8.0-windows10.0.19041.0\win-x64\publish"
    if (-not (Test-Path (Join-Path $publish "LocalDrop.exe"))) { throw "publish output not found: $publish" }

    $iscc = @(
        "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe",
        "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe"
    ) | Where-Object { Test-Path $_ } | Select-Object -First 1
    if (-not $iscc) { throw "Inno Setup (ISCC.exe) not found. winget install JRSoftware.InnoSetup" }

    Write-Host "== Inno Setup ==" -ForegroundColor Cyan
    & $iscc "/DPublishDir=$publish" "/DAppVersion=$version" (Join-Path $root "installer\LocalDrop.iss")
    if ($LASTEXITCODE) { throw "ISCC failed" }
}

if ($Android) {
    Write-Host "== Android publish ==" -ForegroundColor Cyan
    $jdk = Get-ChildItem "C:\Program Files\Microsoft\jdk-17*" -Directory -ErrorAction SilentlyContinue | Select-Object -First 1
    $sdk = if ($env:ANDROID_HOME) { $env:ANDROID_HOME } else { Join-Path $env:LOCALAPPDATA "Android\Sdk" }
    $extra = @()
    if ($jdk) { $extra += "-p:JavaSdkDirectory=$($jdk.FullName)" }
    if (Test-Path $sdk) { $extra += "-p:AndroidSdkDirectory=$sdk" }
    dotnet publish $proj -f net8.0-android -c Release @extra
    if ($LASTEXITCODE) { throw "dotnet publish (android) failed" }

    $apk = Get-ChildItem (Join-Path $root "LocalDrop\bin\Release\net8.0-android\publish") -Filter "*-Signed.apk" | Select-Object -First 1
    if (-not $apk) { throw "signed APK not found" }
    Copy-Item $apk.FullName (Join-Path $dist "LocalDrop-$version.apk") -Force
}

Write-Host "`nOutput in $dist" -ForegroundColor Green
Get-ChildItem $dist | Format-Table Name, @{n="MB";e={[math]::Round($_.Length/1MB,1)}}
