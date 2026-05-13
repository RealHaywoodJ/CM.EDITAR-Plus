<#
.SYNOPSIS
  Restores, builds, and tests the entire CM.EDITAR+ solution.

.DESCRIPTION
  Designed to be the single entry point used by both local Windows developers and CI.
  Runs on Windows PowerShell 5.1+ or PowerShell 7+. Fails fast on any non-zero exit code.
#>
[CmdletBinding()]
param(
    [string]$Configuration = "Release",
    [switch]$SkipTests
)

$ErrorActionPreference = "Stop"
$root = Resolve-Path (Join-Path $PSScriptRoot "..")
Set-Location $root

Write-Host "==> dotnet --info" -ForegroundColor Cyan
dotnet --info

Write-Host "==> Restoring solution" -ForegroundColor Cyan
dotnet restore CM.EDITAR.sln

Write-Host "==> Building ($Configuration)" -ForegroundColor Cyan
dotnet build CM.EDITAR.sln --configuration $Configuration --no-restore

if (-not $SkipTests) {
    Write-Host "==> Running unit tests" -ForegroundColor Cyan
    dotnet test CM.EDITAR.sln --configuration $Configuration --no-build --logger "trx;LogFileName=test-results.trx"
}

Write-Host "==> Build complete." -ForegroundColor Green
