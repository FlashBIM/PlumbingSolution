#requires -Version 5.1
<#
.SYNOPSIS
    Build PlumbingSolution cho Revit 2024 va/hoac 2025, dong goi thanh thu muc add-in san
    dung, va (tuy chon) cai thang vao %APPDATA%\Autodesk\Revit\Addins\<version>\ de test.

.DESCRIPTION
    Khong tao bo cai MSI (bo cai .vdproj can Visual Studio
    Installer Projects extension, khong build duoc bang dotnet/MSBuild thuan) - script
    nay chi phuc vu TEST cuc bo: build DLL, ghep cung hwid.dll + Icon + dependency, dung
    bo cuc thu muc ma file .addin tro toi.

.PARAMETER RevitVersion
    2024, 2025, hoac All (mac dinh). 2024 = net48, 2025 = net8.0-windows - khac
    TargetFramework nen KHONG build chung mot lenh msbuild duoc, script build rieng tung cai.

.PARAMETER Configuration
    Debug hoac Release (mac dinh Release). Ghep voi version thanh "Release_2024"/"Release_2025".

.PARAMETER Install
    Copy ket qua build thang vao %APPDATA%\Autodesk\Revit\Addins\<version>\. Khong chi dinh
    thi chi dong goi vao Output\Deploy\<version>\ - ban tu copy khi san sang.

.EXAMPLE
    .\build-and-deploy.ps1 -RevitVersion 2025 -Install
    Build ban 2025 va cai luon de mo Revit 2025 test duoc ngay.

.EXAMPLE
    .\build-and-deploy.ps1
    Build ca 2024 lan 2025, chi dong goi vao Output\Deploy\, khong dung toi Addins that.
#>
param(
    [ValidateSet('2024', '2025', 'All')]
    [string]$RevitVersion = 'All',

    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',

    [switch]$Install
)

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot

# ---- Tim MSBuild cua Visual Studio qua vswhere. Vi sao khong dung "dotnet build":
# Cac form WinForms co .resx nhi phan
# (Non-string resources) - dotnet build thieu GenerateResourceUsePreserializedResources
# nen loi MSB3822/MSB3823. MSBuild day du cua VS khong bi loi nay. ----
function Find-MSBuild {
    $vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
    if (-not (Test-Path $vswhere)) {
        throw "Khong tim thay vswhere.exe - can cai Visual Studio (kem workload .NET desktop) de build project net48."
    }

    $msbuild = & $vswhere -latest -requires Microsoft.Component.MSBuild -find "MSBuild\**\Bin\MSBuild.exe" | Select-Object -First 1
    if (-not $msbuild -or -not (Test-Path $msbuild)) {
        throw "vswhere khong tim thay MSBuild.exe. Cai Visual Studio workload '.NET desktop development'."
    }

    return $msbuild
}

function Build-Version {
    param(
        [string]$Year,
        [string]$MSBuildPath
    )

    $config = "${Configuration}_${Year}"
    $csproj = Join-Path $root "PlumbingSolution$Year\PlumbingSolution$Year.csproj"

    if (-not (Test-Path $csproj)) {
        throw "Khong tim thay $csproj"
    }

    Write-Host "=== Build Revit $Year ($config) ===" -ForegroundColor Cyan
    # Out-Host thay vi de output chay thang: goi ben trong ham co gia tri tra ve se
    # khien PowerShell gom ca dong log cua msbuild.exe vao return value, lam hong
    # $outDir o duoi ham (tung gap loi thuc te: "Cannot find drive '...  Determining
    # projects to restore...'"). Out-Host van hien log ra man hinh, chi khong "leak"
    # vao output stream.
    & $MSBuildPath $csproj "-p:Configuration=$config" -v:minimal -nologo -restore | Out-Host
    if ($LASTEXITCODE -ne 0) {
        throw "Build Revit $Year that bai (exit code $LASTEXITCODE)."
    }

    $outDir = Join-Path $root "Output\$Year\$config"
    if (-not (Test-Path (Join-Path $outDir 'PlumbingSolution.dll'))) {
        throw "Build bao thanh cong nhung khong thay PlumbingSolution.dll tai $outDir - kiem tra lai cau hinh BaseOutputPath."
    }

    return $outDir
}

# ---- Dong goi: .addin (Revit tu do trong Addins\<year>\*.addin) + thu muc con chua DLL,
# DUNG bo cuc Assembly="PlumbingSolution\PlumbingSolution.dll" ma .addin khai. ----
function Publish-AddinPackage {
    param(
        [string]$Year,
        [string]$BuildOutputDir
    )

    $deployDir = Join-Path $root "Output\Deploy\$Year"
    $assemblyDir = Join-Path $deployDir 'PlumbingSolution'

    if (Test-Path $deployDir) {
        Remove-Item $deployDir -Recurse -Force
    }
    New-Item -ItemType Directory -Path $assemblyDir -Force | Out-Null

    # DLL chinh + moi thu build sinh ra canh no (pdb, PlumbingSolutionResources.dll,
    # hwid.dll, .deps.json cua net8...). KHONG copy RevitAPI*.dll/AdWindows.dll/
    # UIFramework.dll - Revit tu co ban cua no, mang theo ban dong goi de lech version.
    $skipNames = @('RevitAPI.dll', 'RevitAPIUI.dll', 'AdWindows.dll', 'UIFramework.dll')
    Get-ChildItem -Path $BuildOutputDir | Where-Object { -not $_.PSIsContainer -and $skipNames -notcontains $_.Name } |
        Copy-Item -Destination $assemblyDir -Force

    # Icon: GetIconFolder() trong App.cs doc "<thu muc assembly>\Icon" luc chay - khong
    # co buoc MSBuild nao copy thu muc nay, phai lam tay o day (VDPROJ installer cung
    # tu lam viec nay, script chi mo phong lai cho moi truong test).
    $iconSrc = Join-Path $root 'Icon'
    if (Test-Path $iconSrc) {
        Copy-Item $iconSrc (Join-Path $assemblyDir 'Icon') -Recurse -Force
    }
    else {
        Write-Warning "Khong thay thu muc Icon nguon ($iconSrc) - ribbon se thieu icon nhung van chay duoc."
    }

    $addinContent = @"
<?xml version="1.0" encoding="utf-8"?>
<RevitAddIns>
  <AddIn Type="Application">
    <Name>PlumbingSolution</Name>
    <Assembly>PlumbingSolution\PlumbingSolution.dll</Assembly>
    <FullClassName>PlumbingSolution.App</FullClassName>
    <ClientId>aa652485-85bb-455a-a039-8e93f1fa1294</ClientId>
    <VendorId>00de36d0-f3bd-4536-81e8-ece5df5eda8f</VendorId>
    <VendorDescription>PlumbingSolution</VendorDescription>
  </AddIn>
</RevitAddIns>
"@
    Set-Content -Path (Join-Path $deployDir 'PlumbingSolution.addin') -Value $addinContent -Encoding UTF8

    Write-Host "  Dong goi xong: $deployDir" -ForegroundColor Green
    return $deployDir
}

function Install-AddinPackage {
    param(
        [string]$Year,
        [string]$DeployDir
    )

    $addinsRoot = Join-Path $env:APPDATA "Autodesk\Revit\Addins\$Year"
    New-Item -ItemType Directory -Path $addinsRoot -Force | Out-Null

    # Chi ghi de DUNG nhung gi cua PlumbingSolution - add-in khac da co trong thu muc nay
    # (vd AddinDiritProject-dev.addin) khong duoc dung toi.
    Copy-Item (Join-Path $DeployDir 'PlumbingSolution.addin') $addinsRoot -Force
    $targetAssemblyDir = Join-Path $addinsRoot 'PlumbingSolution'
    if (Test-Path $targetAssemblyDir) {
        Remove-Item $targetAssemblyDir -Recurse -Force
    }
    Copy-Item (Join-Path $DeployDir 'PlumbingSolution') $targetAssemblyDir -Recurse -Force

    Write-Host "  Da cai vao: $addinsRoot" -ForegroundColor Green
    Write-Host "  (Dong Revit $Year hoan toan truoc khi mo lai neu dang chay - Revit khoa DLL add-in luc dang mo.)" -ForegroundColor Yellow
}

# ---- main ----
$msbuild = Find-MSBuild
Write-Host "MSBuild: $msbuild`n"

$years = if ($RevitVersion -eq 'All') { @('2024', '2025') } else { @($RevitVersion) }

foreach ($year in $years) {
    $outDir = Build-Version -Year $year -MSBuildPath $msbuild
    $deployDir = Publish-AddinPackage -Year $year -BuildOutputDir $outDir

    if ($Install) {
        Install-AddinPackage -Year $year -DeployDir $deployDir
    }
    else {
        $addinsTarget = Join-Path $env:APPDATA "Autodesk\Revit\Addins\$year"
        Write-Host "  Chua cai. Cai tay: copy noi dung '$deployDir' vao '$addinsTarget'" -ForegroundColor Yellow
        Write-Host "  Hoac chay lai kem -Install." -ForegroundColor Yellow
    }
    Write-Host ""
}

Write-Host "Xong." -ForegroundColor Cyan
