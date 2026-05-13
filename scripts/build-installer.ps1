<#
.SYNOPSIS
  Builds the WiX MSI installer for CM.EDITAR+.

  Requires: pwsh (PowerShell 7+) or Windows PowerShell 5.1+, .NET 8 SDK,
            WiX 4 SDK NuGet (restored automatically), and signtool.exe from
            the Windows 10/11 SDK when signing is requested.

  Pipeline:
    1. `dotnet publish` UI and FileCreator (framework-dependent, win-x64).
       The WiX project sources every file under each project's
       bin/<Cfg>/net8.0/win-x64/publish folder via the <Files> element,
       so the publish output and WiX DefineConstants stay in sync.
    2. `dotnet build` the WiX project — produces CM.EDITAR.Setup.msi.
    3. Optionally sign the EXEs and the MSI with signtool.exe.
    4. Copy the resulting MSI to dist/installer/.

  Code-signing (optional):
    Pass -SignWith <path-to.pfx> to sign, or export the CI secrets:
      SIGN_PFX_BASE64    – base-64-encoded PFX file content
      SIGN_PFX_PASSWORD  – password for the PFX
    When neither -SignWith nor the env vars are present the step is skipped
    with a warning; unsigned builds will trigger SmartScreen on Windows.
#>
[CmdletBinding()]
param(
    [string]$Configuration = "Release",
    [string]$Rid           = "win-x64",
    [string]$SignWith       = ""        # path to a .pfx file; optional
)

$ErrorActionPreference = "Stop"
$root = Resolve-Path (Join-Path $PSScriptRoot "..")
Set-Location $root

# ---------------------------------------------------------------------------
# Helper: resolve signtool.exe from Windows SDK (best-effort)
# ---------------------------------------------------------------------------
function Find-SignTool {
    $candidates = @(
        (Get-Command signtool.exe -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Source -ErrorAction SilentlyContinue),
        "$env:ProgramFiles(x86)\Windows Kits\10\bin\x64\signtool.exe",
        "$env:ProgramFiles(x86)\Windows Kits\10\bin\x86\signtool.exe"
    ) | Where-Object { $_ -and (Test-Path $_) }
    if ($candidates.Count -eq 0) { return $null }
    return $candidates[0]
}

# ---------------------------------------------------------------------------
# Helper: sign one file; $pfxPath + $pfxPassword already resolved
# ---------------------------------------------------------------------------
function Invoke-SignFile {
    param([string]$FilePath, [string]$PfxPath, [string]$PfxPassword, [string]$SignToolExe)
    Write-Host "  Signing $FilePath" -ForegroundColor DarkCyan
    & $SignToolExe sign `
        /fd  SHA256 `
        /td  SHA256 `
        /tr  "http://timestamp.digicert.com" `
        /f   $PfxPath `
        /p   $PfxPassword `
        $FilePath
    if ($LASTEXITCODE -ne 0) { throw "signtool failed (exit $LASTEXITCODE) for $FilePath" }
}

# ---------------------------------------------------------------------------
# Resolve signing material
#   Priority: -SignWith param > CI env vars > skip
# ---------------------------------------------------------------------------
$pfxPath     = ""
$pfxPassword = ""
$tempPfx     = $null

if ($SignWith -ne "") {
    if (-not (Test-Path $SignWith)) { throw "PFX not found: $SignWith" }
    $pfxPath     = (Resolve-Path $SignWith).Path
    $pfxPassword = if ($env:SIGN_PFX_PASSWORD) { $env:SIGN_PFX_PASSWORD } else { "" }
    Write-Host "==> Code signing: using PFX at $pfxPath" -ForegroundColor Cyan
}
elseif ($env:SIGN_PFX_BASE64) {
    $tempPfx = Join-Path ([System.IO.Path]::GetTempPath()) "cm-editar-sign-$([Guid]::NewGuid()).pfx"
    [System.IO.File]::WriteAllBytes($tempPfx, [Convert]::FromBase64String($env:SIGN_PFX_BASE64))
    $pfxPath     = $tempPfx
    $pfxPassword = if ($env:SIGN_PFX_PASSWORD) { $env:SIGN_PFX_PASSWORD } else { "" }
    Write-Host "==> Code signing: using PFX from SIGN_PFX_BASE64 env var" -ForegroundColor Cyan
}
else {
    Write-Warning "No signing material provided (set SIGN_PFX_BASE64 + SIGN_PFX_PASSWORD or pass -SignWith). Build will be unsigned."
}

$willSign = $pfxPath -ne ""
$signTool = $null
if ($willSign) {
    $signTool = Find-SignTool
    if (-not $signTool) { throw "signtool.exe not found. Install the Windows 10/11 SDK." }
}

# ---------------------------------------------------------------------------
# Main build + signing — temp PFX is always cleaned up in the finally block
# ---------------------------------------------------------------------------
try {
    # 1. Publish UI + FileCreator
    Write-Host "==> Publishing UI ($Rid, framework-dependent)" -ForegroundColor Cyan
    dotnet publish src/CM.EDITAR.UI/CM.EDITAR.UI.csproj `
        -c $Configuration -r $Rid --self-contained false
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish UI failed." }

    Write-Host "==> Publishing FileCreator ($Rid, framework-dependent)" -ForegroundColor Cyan
    dotnet publish src/CM.EDITAR.FileCreator/CM.EDITAR.FileCreator.csproj `
        -c $Configuration -r $Rid --self-contained false
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish FileCreator failed." }

    # 2. (Optional) Sign EXEs before WiX embeds them
    if ($willSign) {
        Write-Host "==> Signing published EXEs" -ForegroundColor Cyan
        $publishDirs = @(
            "src/CM.EDITAR.UI/bin/$Configuration/net8.0/$Rid/publish",
            "src/CM.EDITAR.FileCreator/bin/$Configuration/net8.0/$Rid/publish"
        )
        foreach ($dir in $publishDirs) {
            $absDir = Join-Path $root $dir
            if (-not (Test-Path $absDir)) { Write-Warning "Publish dir not found: $absDir"; continue }
            Get-ChildItem $absDir -Filter "*.exe" | ForEach-Object {
                Invoke-SignFile -FilePath $_.FullName -PfxPath $pfxPath -PfxPassword $pfxPassword -SignToolExe $signTool
            }
        }
    }

    # 3. Build MSI (WiX 4)
    Write-Host "==> Building MSI (WiX 4)" -ForegroundColor Cyan
    dotnet build src/CM.EDITAR.Installer/CM.EDITAR.Installer.wixproj `
        -c $Configuration -p:PublishRid=$Rid
    if ($LASTEXITCODE -ne 0) { throw "WiX build failed." }

    $out = Join-Path $root "src/CM.EDITAR.Installer/bin/$Configuration"
    $msi = Get-ChildItem $out -Filter "*.msi" -Recurse | Select-Object -First 1
    if (-not $msi) { throw "MSI not produced under $out." }

    # 4. (Optional) Sign the MSI
    if ($willSign) {
        Write-Host "==> Signing MSI" -ForegroundColor Cyan
        Invoke-SignFile -FilePath $msi.FullName -PfxPath $pfxPath -PfxPassword $pfxPassword -SignToolExe $signTool
    }

    # 5. Copy to dist/
    $dist = Join-Path $root "dist/installer"
    New-Item -ItemType Directory -Force -Path $dist | Out-Null
    Copy-Item $msi.FullName -Destination $dist -Force
    Write-Host "==> MSI published: $(Join-Path $dist $msi.Name)" -ForegroundColor Green

    if ($willSign) {
        Write-Host "==> Binaries are code-signed." -ForegroundColor Green
    } else {
        Write-Warning "Binaries are NOT signed. SmartScreen will warn on download/install."
    }
}
finally {
    # Always remove the temp PFX written from the CI env var, even on error
    if ($tempPfx -and (Test-Path $tempPfx)) {
        Remove-Item $tempPfx -Force
    }
}
