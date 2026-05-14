# CM.EDITAR+ — Context Menu / ShellNew Manager

> © 2026 **SirSHAmun5on12** ([SHAmun.fyi](https://SHAmun.fyi))
> Licensed under the [Apache License 2.0](LICENSE)
> Maintainer: **SirSHAmun5on12** — <dev@eth-munson.com>

[![License: Apache 2.0](https://img.shields.io/badge/License-Apache_2.0-blue.svg)](LICENSE)
![Platform: Windows](https://img.shields.io/badge/platform-Windows-0078D6.svg)
![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4.svg)
![Avalonia 11.2](https://img.shields.io/badge/Avalonia-11.2-883fd2.svg)
![Version](https://img.shields.io/badge/version-v1.3.0-22c55e.svg)

CM.EDITAR+ is a Windows desktop utility that gives users full, safe, auditable control over the
Windows Explorer **New** submenu and provides an optional per-user **New+** submenu for curated
templated file types. Every change is HKCU-scoped, snapshotted before write, verified after,
and reversible from a built-in undo stack.

It ships a **comprehensive 400+ extension catalog** across 11 wired categories
(AI/Automation, Archives, CAD/3D, Cloud Docs, Legacy, Media, Office/Docs,
Omega Database, Power User, System, Text/Data) sourced from a single C# file
(`ExtensionCatalog.cs`) and surfaced in both Category and A–Z views. High-risk
executable extensions (`.exe`, `.bat`, `.ps1`, `.crx`, `.msi`, `.scr`, …) are
classified `risk: high` and ship **disabled by default** — enforced by build-time
invariants in both the C# tests and the catalog generator.

> **Status:** **v1.3.0** — public preview. All projects compile on Windows; the engine
> layers (Core / Registry / Apply / Templates / FileCreator) are substantively implemented.
> The Avalonia UI is wired to the engine via a single `MainViewModel`. Windows-specific
> calls (registry, DPAPI, SHChangeNotify) are isolated behind interfaces so the solution
> remains cross-compilable on Linux/macOS for CI authoring; runtime registry I/O only
> executes on Windows.
>
> A **built-in starter pack** (29 templates) is seeded into `%AppData%\CM.EDITAR+\Templates\`
> on first run so the Template Manager is immediately useful out-of-the-box.

If you find CM.EDITAR+ useful, **tips welcome**: <https://SHAmun.fyi/tips>.

---

## Quick start (Windows)

You'll need **Windows 10 or 11** and the **.NET 8 SDK or any newer SDK
(.NET 9, .NET 10 …)** — the `global.json` accepts any SDK ≥ 8.0.100, so a
single up-to-date SDK install is enough. Grab one from
<https://dotnet.microsoft.com/download> if you don't have it.

```powershell
# 1. Clone
git clone https://github.com/RealHaywoodJ/CM.EDITAR-Plus.git
cd CM.EDITAR-Plus

# 2. Build & test
dotnet build CM.EDITAR.sln -c Release
dotnet test  CM.EDITAR.sln -c Release

# 3. Run the desktop app
dotnet run --project src/CM.EDITAR.UI -c Release
```

On first launch the app seeds **29 ready-to-use templates** (Markdown, JSON,
YAML, Python, PowerShell, HTML, CSV, Dockerfile and more) into
`%AppData%\CM.EDITAR+\Templates\`. Open the **Manager**, toggle the file
types you want in your Windows **New** submenu, hit **Apply Changes**
(green button) — your registry is snapshotted first, then only your personal
HKCU keys are written, and Explorer is refreshed automatically. If anything
looks wrong, **Undo Last** restores the snapshot.

---

## Architecture

```
┌──────────────────────────────────────────────────────────────────┐
│  CM.EDITAR.UI  (Avalonia MVVM, .NET 8)                            │
│  MainViewModel · TemplateManagerViewModel · PreflightViewModel    │
└──────────────────────────────────────────────────────────────────┘
            │                    │                    │
            ▼                    ▼                    ▼
┌─────────────────┐  ┌──────────────────┐  ┌────────────────────┐
│ ApplyService    │  │ TemplateService  │  │ FileCreator (CLI)  │
│  snapshot →     │  │  CRUD · render · │  │  --create/--preview│
│  apply →        │  │  placeholders ·  │  │  --serve (named    │
│  SHChangeNotify │  │  Cmd sanitizer   │  │  pipe + DPAPI auth)│
│  → verify →     │  └──────────────────┘  └────────────────────┘
│  rollback       │            │                     │
└─────────────────┘            │                     │
            │                  ▼                     ▼
            ▼          ┌──────────────────────────────────┐
┌─────────────────┐    │ CM.EDITAR.Core (interfaces +     │
│ RegistryService │◄───┤ models · enums · DTOs)           │
│  ProgID resolve │    └──────────────────────────────────┘
│  HKCU writes    │
│  P/Invoke       │
└─────────────────┘
            │
            ▼
   Windows Registry (HKCU only at runtime)
```

### Project layout

| Project                      | Purpose                                                       |
| ---------------------------- | ------------------------------------------------------------- |
| `CM.EDITAR.Core`             | Models, enums, DTOs, service interfaces, on-disk path helpers |
| `CM.EDITAR.Registry`         | Discovery, ProgID + UserChoice resolution, snapshots, P/Invoke |
| `CM.EDITAR.ApplyService`     | Snapshot → apply → verify → rollback pipeline + signed audit |
| `CM.EDITAR.Templates`        | Template CRUD, placeholder engine, Command sanitizer          |
| `CM.EDITAR.FileCreator`      | Atomic-write CLI + named-pipe server with DPAPI auth          |
| `CM.EDITAR.UI`               | Avalonia MVVM desktop app                                     |
| `CM.EDITAR.Installer`        | WiX 4 MSI (per-user) + installer snapshot/uninstall restore   |
| `tests/*.Tests`              | xUnit + FluentAssertions for Registry, Templates, FileCreator |

### Starter pack

On first launch CM.EDITAR+ automatically seeds 29 ready-to-use templates into the user's
`%AppData%\CM.EDITAR+\Templates\` folder (no templates are overwritten if any already exist):

| Template            | Extension(s)      | Type     | Placeholders used                  |
| ------------------- | ----------------- | -------- | ---------------------------------- |
| Markdown Notebook   | `.md`             | FileName | `%TITLE%`, `%USERNAME%`, `%DATE%`  |
| Blank Text File     | `.txt`            | FileName | `%DATE%`, `%USERNAME%`             |
| JSON Skeleton       | `.json`           | FileName | `%TITLE%`, `%DATE%`, `%USERNAME%`  |
| YAML Config         | `.yaml`, `.yml`   | FileName | `%PROJECT%`, `%USERNAME%`, `%DATE%`|
| HTML Page           | `.html`, `.htm`   | FileName | `%TITLE%`, `%USERNAME%`, `%DATE%`  |
| CSV with Headers    | `.csv`            | FileName | `%DATE%`                           |
| Blank File          | (none)            | NullFile | —                                  |
| PowerShell Script   | `.ps1`            | FileName | `%DATE%`, `%USERNAME%`             |
| Python Script       | `.py`             | FileName | `%DATE%`, `%USERNAME%`             |
| Environment Vars    | `.env`            | FileName | `%DATE%`, `%USERNAME%`             |
| Dockerfile          | `.dockerfile`     | FileName | `%DATE%`, `%USERNAME%`             |
| TOML Config         | `.toml`           | FileName | `%PROJECT%`, `%USERNAME%`, `%DATE%`|
| README              | `.readme`         | FileName | `%TITLE%`, `%DATE%`, `%USERNAME%`  |
| Config File         | `.cfg`            | FileName | `%DATE%`, `%USERNAME%`             |
| Batch Script        | `.bat`            | FileName | `%DATE%`, `%USERNAME%`             |
| Internet Shortcut   | `.url`            | FileName | `%URL%`                            |
| Lua Script          | `.lua`            | FileName | `%DATE%`, `%USERNAME%`             |
| Shell Script        | `.sh`             | FileName | `%DATE%`, `%USERNAME%`             |
| Gitignore           | `.gitignore`      | FileName | `%DATE%`, `%USERNAME%`             |
| Gitattributes       | `.gitattributes`  | FileName | `%DATE%`, `%USERNAME%`             |
| Rich Text Document  | `.rtf`            | FileName | `%DATE%`, `%USERNAME%`             |
| INI Config          | `.ini`            | FileName | `%DATE%`, `%USERNAME%`             |
| Log File            | `.log`            | FileName | `%DATE%`, `%TIME%`, `%USERNAME%`   |
| Java Properties     | `.properties`     | FileName | `%DATE%`, `%USERNAME%`             |
| PDF Document        | `.pdf`            | NullFile | —                                  |
| Binary File         | `.bin`            | NullFile | —                                  |
| Shortcut            | `.lnk`            | NullFile | —                                  |
| Command Script      | `.cmd`            | FileName | `%DATE%`, `%USERNAME%`             |
| Cron Schedule       | `.cron`           | FileName | `%DATE%`, `%USERNAME%`             |

`NullFile` templates create an empty file with the registered extension — used for binary
formats (`.pdf`, `.bin`, `.lnk`) where a templated body wouldn't be a valid file, and for the
generic blank-file entry.

The source files live in `templates/starter-pack/<id>/` and are copied to the output directory
at build time. The importer (`StarterPackImporter`) is wired into `App.axaml.cs` and runs
asynchronously on a background thread at startup so it never delays the UI; the Template
Manager awaits the import on first launch so the list is always populated when it opens.

### Storage paths

| Purpose         | Path                                              |
| --------------- | ------------------------------------------------- |
| Templates       | `%AppData%\CM.EDITAR+\Templates\<id>\`            |
| Snapshots       | `%LocalAppData%\CM.EDITAR+\Backups\<stamp>.reg` (+ `.json` sidecar) |
| Audit log       | `%LocalAppData%\CM.EDITAR+\Audit\changes.log`     |
| Diagnostic logs | `%LocalAppData%\CM.EDITAR+\Logs\<timestamp>.log`  |
| IPC secret      | `%LocalAppData%\CM.EDITAR+\Secrets\filecreator.secret` (DPAPI-encrypted on Windows) |

---

## Building

Requires the **.NET 8 SDK or any newer SDK** (.NET 9, .NET 10 …). The
`global.json` uses `rollForward: latestMajor`, so any SDK ≥ 8.0.100 will
pick up the build — you do **not** need to downgrade or install an extra
SDK if you already have a newer one. On Windows, also install the
**WiX Toolset 4** SDK NuGet (restored automatically).

> **WiX `<Files Include=…>` works on WiX 4.0.5.** The auto-harvest element
> the installer uses (`<Files Include="$(var.UIPublishDir)\**" />`) is
> available across WiX v4 *and* v5 — there is no need to upgrade. The
> NuGet `WixToolset.Sdk/4.0.5` pin is intentional for reproducible builds.

```powershell
# Restore + build + run unit tests
pwsh ./scripts/build.ps1

# Build the MSI installer
pwsh ./scripts/build-installer.ps1

# Build the portable ZIP
pwsh ./scripts/build-portable.ps1
```

Manual:

```powershell
dotnet restore CM.EDITAR.sln
dotnet build   CM.EDITAR.sln -c Release
dotnet test    CM.EDITAR.sln -c Release
```

The solution also restores and builds on Linux/macOS for CI authoring; only the
Windows-only services (registry I/O, DPAPI) become no-ops or throw.

---

## How Apply works (and why it's safe)

1. **Stage** edits in the UI. Nothing is written.
2. **Preflight** generates an `ApplyManifest` containing every concrete `RegistryOperation`
   (Add / Modify / Delete) the batch will perform. The keypath is shown verbatim.
3. **Snapshot** — every key the manifest will touch is exported to a timestamped `.reg`
   file under `%LocalAppData%\CM.EDITAR+\Backups\` along with a JSON sidecar containing
   the SHA-256, exported keys, user SID, and manifest ID. **Snapshots are never overwritten.**
4. **Apply** — every operation is funneled through `RegistryService.ApplyOperation`,
   which refuses any non-HKCU keypath at the entry point.
5. **`SHChangeNotify(SHCNE_ASSOCCHANGED)`** asks Explorer to refresh.
6. **Verify** — the manifest is re-read against the live registry. If any operation
   didn't take, **rollback** runs immediately by importing the snapshot.
7. **Audit** — a signed (HMAC-SHA256, per-install secret) JSON line is appended to
   `changes.log` for every Apply / Undo, success or failure.

### Rollback / Undo

- **Undo Last** restores the most recent snapshot.
- **Undo All** restores every snapshot in reverse-chronological order.
- Snapshots are also imported manually with `regedit /s <snapshot>.reg`.

---

## Threat model summary

| Asset                         | Threat                                       | Mitigation                                          |
| ----------------------------- | -------------------------------------------- | --------------------------------------------------- |
| Windows registry integrity    | Rogue HKLM / system-wide writes              | All writes funneled through `RegistryService.ApplyOperation` which throws on non-HKCU paths |
| Snapshots                     | Tampering with rollback files                | SHA-256 stored in JSON sidecar; restore verifies before importing |
| Audit log                     | After-the-fact forgery                       | Each line signed with HMAC-SHA256 keyed by per-install DPAPI secret |
| FileCreator IPC               | Other users invoking the pipe                | Named-pipe ACL restricted to the current user SID |
| FileCreator IPC               | Replay / brute-force tokens                  | DPAPI-protected per-install secret + constant-time validation + 10 req/sec throttle |
| Command templates             | Drive-by code execution                      | Two-gate sanitizer: `commandApproved=true` AND no disallowed tokens (network paths, `Invoke-Expression`, etc.) |
| Templates                     | Malicious payload                            | Static-by-default; only Command templates can execute and require explicit user approval |
| Snapshot files on disk        | Local exfiltration                           | Stored under per-user `%LocalAppData%`; never uploaded automatically |

---

## Code signing

Unsigned binaries trigger Windows SmartScreen warnings on download and at install time. Both
`build-installer.ps1` and `build-portable.ps1` include an optional signing step that runs
`signtool.exe` (from the Windows 10/11 SDK) against every EXE and, for the MSI build, the
installer itself.

### Obtaining a certificate

For production releases use an **Extended Validation (EV) code-signing certificate** from a
publicly trusted CA (e.g. DigiCert, Sectigo, GlobalSign). EV certificates establish SmartScreen
reputation immediately. Standard OV certificates also work but may require download count to
build reputation. Self-signed certificates are supported for internal/test builds only.

Export the certificate as a **password-protected PFX** (`.pfx` / `.p12`) file.

### Local developer signing

```powershell
# Sign the MSI (and the EXEs embedded in it)
pwsh ./scripts/build-installer.ps1 -SignWith "C:\certs\my-cert.pfx"

# Sign the EXEs in the portable ZIP
pwsh ./scripts/build-portable.ps1 -SignWith "C:\certs\my-cert.pfx"
```

If the PFX is password-protected, set the password via the environment variable before running:

```powershell
$env:SIGN_PFX_PASSWORD = "your-pfx-password"
pwsh ./scripts/build-installer.ps1 -SignWith "C:\certs\my-cert.pfx"
```

### CI / CD secrets contract

The build scripts read two environment variables when `-SignWith` is not passed. Configure these
as **secrets** in your CI system (GitHub Actions, Azure Pipelines, etc.):

| Secret name        | Value                                                            |
| ------------------ | ---------------------------------------------------------------- |
| `SIGN_PFX_BASE64`  | Base-64-encoded content of the PFX file (`base64 -w0 cert.pfx`) |
| `SIGN_PFX_PASSWORD`| Password that protects the PFX file                             |

The scripts write the decoded PFX to a temp file, sign all targets, then immediately delete the
temp file. When both variables are absent the signing step is **skipped** and the build
completes with a warning — unsigned CI artifacts are still produced so the pipeline never
breaks on a missing certificate.

Example GitHub Actions step:

```yaml
- name: Build signed MSI
  shell: pwsh
  env:
    SIGN_PFX_BASE64:    ${{ secrets.SIGN_PFX_BASE64 }}
    SIGN_PFX_PASSWORD:  ${{ secrets.SIGN_PFX_PASSWORD }}
  run: pwsh ./scripts/build-installer.ps1
```

### Timestamp authority

Both scripts use the DigiCert RFC 3161 timestamp server
(`http://timestamp.digicert.com`, SHA-256). Timestamping ensures the signature remains valid
after the certificate's validity period expires. To use a different TSA, edit the `/tr`
argument in the `Invoke-SignFile` helper inside each script.

---

## Verification commands

```powershell
dotnet build CM.EDITAR.sln
dotnet test  CM.EDITAR.sln

# Discovery (Windows only):
dotnet run --project src/CM.EDITAR.UI -- # then click Refresh

# FileCreator CLI:
.\src\CM.EDITAR.FileCreator\bin\Debug\net8.0\CM.EDITAR.FileCreator.exe --preview --template-id <guid>
.\src\CM.EDITAR.FileCreator\bin\Debug\net8.0\CM.EDITAR.FileCreator.exe --create  --template-id <guid> --target "C:\Temp\test.md"
.\src\CM.EDITAR.FileCreator\bin\Debug\net8.0\CM.EDITAR.FileCreator.exe --serve

# Installer:
msiexec /i .\dist\installer\CM.EDITAR.Setup.msi

# UI end-to-end smoke flows (Windows only; needs WinAppDriver running):
dotnet publish src/CM.EDITAR.UI -c Release -o publish/ui
$env:CM_EDITAR_UI_EXE = (Resolve-Path .\publish\ui\CM.EDITAR.UI.exe)
dotnet test tests/CM.EDITAR.UI.E2E -c Release
```

The UI E2E suite covers discovery, stage+apply round-trip, and undo. It builds
on every platform but only executes when WinAppDriver is reachable and
`CM_EDITAR_UI_EXE` is set; the dedicated `ui-e2e.yml` GitHub Actions lane
runs it on `windows-latest` and uploads screenshots/logs on failure. See
[tests/CM.EDITAR.UI.E2E/README.md](tests/CM.EDITAR.UI.E2E/README.md) for
local setup details.

---

## License

Copyright 2026 **SirSHAmun5on12** ([SHAmun.fyi](https://SHAmun.fyi)).

CM.EDITAR+ is licensed under the **Apache License, Version 2.0** — see
[LICENSE](LICENSE) for the full text and [NOTICE](NOTICE) for attribution
requirements. The Apache 2.0 license is permissive (you can use, modify and
redistribute the code, including in commercial / closed-source projects) and
includes an explicit patent grant.

## Contributing & community

- Bug reports and feature requests: open a GitHub Issue.
- Pull requests: see [CONTRIBUTING.md](CONTRIBUTING.md). All PRs must include
  tests and a short description of the change.
- Code of conduct: [CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md) (Contributor
  Covenant v2.1).
- Security disclosures: see [SECURITY.md](SECURITY.md) — please email
  <dev@eth-munson.com> rather than opening a public issue.
- Maintainers: [MAINTAINERS.md](MAINTAINERS.md) · contributors:
  [AUTHORS.md](AUTHORS.md).
- Release history: [CHANGELOG.md](CHANGELOG.md).

## Support the project

- **Maintainer site:** <https://SHAmun.fyi>
- **Tips:** <https://SHAmun.fyi/tips>
- **Contact:** <dev@eth-munson.com>
