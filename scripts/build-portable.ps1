<#
.SYNOPSIS
  Builds the portable (zipped) distribution of CM.EDITAR+.

  Requires: pwsh (PowerShell 7+) or Windows PowerShell 5.1+, .NET 8 SDK,
            and signtool.exe from the Windows 10/11 SDK when signing is
            requested.

  Code-signing (optional):
    Pass -SignWith <path-to.pfx> to sign, or export the CI secrets:
      SIGN_PFX_BASE64    – base-64-encoded PFX file content
      SIGN_PFX_PASSWORD  – password for the PFX
    When neither -SignWith nor the env vars are present the step is skipped
    with a warning; unsigned builds will trigger SmartScreen on download.
#>
[CmdletBinding()]
param(
    [string]$Configuration = "Release",
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
# Helper: sign one file
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
    # 1. Publish UI + FileCreator into the portable staging dir
    $publishDir = Join-Path $root "dist/portable/CM.EDITAR+"
    if (Test-Path $publishDir) { Remove-Item $publishDir -Recurse -Force }
    New-Item -ItemType Directory -Force -Path $publishDir | Out-Null

    Write-Host "==> Publishing UI" -ForegroundColor Cyan
    dotnet publish src/CM.EDITAR.UI/CM.EDITAR.UI.csproj `
        -c $Configuration -r win-x64 --self-contained false -o $publishDir
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish UI failed." }

    Write-Host "==> Publishing FileCreator" -ForegroundColor Cyan
    dotnet publish src/CM.EDITAR.FileCreator/CM.EDITAR.FileCreator.csproj `
        -c $Configuration -r win-x64 --self-contained false -o $publishDir
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish FileCreator failed." }

    # 2. (Optional) Sign EXEs before zipping
    if ($willSign) {
        Write-Host "==> Signing EXEs in portable staging dir" -ForegroundColor Cyan
        Get-ChildItem $publishDir -Filter "*.exe" | ForEach-Object {
            Invoke-SignFile -FilePath $_.FullName -PfxPath $pfxPath -PfxPassword $pfxPassword -SignToolExe $signTool
        }
    }

    # 3. Zip
    $zipPath = Join-Path $root "dist/CM.EDITAR+_portable.zip"
    if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
    Compress-Archive -Path "$publishDir\*" -DestinationPath $zipPath
    Write-Host "==> Portable ZIP: $zipPath" -ForegroundColor Green

    if ($willSign) {
        Write-Host "==> EXEs in portable ZIP are code-signed." -ForegroundColor Green
    } else {
        Write-Warning "EXEs are NOT signed. SmartScreen will warn on download."
    }
}
finally {
    # Always remove the temp PFX written from the CI env var, even on error
    if ($tempPfx -and (Test-Path $tempPfx)) {
        Remove-Item $tempPfx -Force
    }
}
