# Changelog

All notable changes to CM.EDITAR+ are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Changed
- **Renamed** the `Automation/Data` catalog category to **`AI/Automation`**
  to reflect that every entry in it (model weights, tokenizers, prompts,
  embeddings, vector indices, notebooks, workflows) is part of the local-AI
  workflow. No entries moved between categories — only the label changed,
  and the sidebar now lists `AI/Automation` first (alphabetical).

### Fixed
- **FileCreator now restores cleanly on Windows.** Added an explicit
  `System.Security.Cryptography.ProtectedData` NuGet `PackageReference` to
  `CM.EDITAR.FileCreator.csproj`. DPAPI lives in a separate package on
  .NET 8; without it the DPAPI wrap/unwrap used by `SecretStore.cs` failed
  to compile on a clean restore.
- **`App.axaml.cs`** now imports `Avalonia.Controls` explicitly so the
  startup hand-off compiles against future Avalonia versions where the
  transitive type re-export drops.

## [1.3.0] — 2026-05-13

### Added
- **Comprehensive 400+ extension catalog** spanning 11 wired categories —
  AI/Automation, Archives, CAD/3D, Cloud Docs, Legacy, Media, Office/Docs,
  Omega Database, Power User, System and Text/Data — sourced from a single
  C# file (`ExtensionCatalog.cs`) and consumed by both the desktop UI and
  the design-mockup sandbox via a generated JSON artifact.
- **A–Z view** in the Manager with sticky per-letter section headers
  (`#`, `0`–`9`, `A`–`Z`) and a Category / A–Z toggle in the table header.
- **Built-in starter pack** of 29 ready-to-use templates seeded into
  `%AppData%\CM.EDITAR+\Templates\` on first launch — Markdown, JSON,
  YAML/TOML, HTML, CSV-with-headers, Python, PowerShell, Dockerfile,
  Gitignore/Gitattributes, README/INI/.env, and more.
- **Selective upgrade flow** with body-hash drift detection: the Template
  Manager now flags edited built-ins on upgrade and lets the user keep,
  overwrite, or merge.
- **Template pack Import / Export** in the Template Manager with a single-pack
  ZIP format and a Cmd-template quarantine review step on import.
- **Help / Support menubar polish** — wired About / Help / Support menu, Help
  pane with keyboard shortcuts, and a Support pane with maintainer / donation
  / source-repo links and QR codes.
- **Bright-green Apply Changes flow** with an explicit pre-apply backup
  confirmation dialog (default snapshot path acknowledged or manual snapshot
  saved before the registry is touched).
- **Signed installer pipeline** — `scripts/build-installer.ps1` and
  `scripts/build-portable.ps1` accept a `-SignWith <pfx>` flag and read
  `SIGN_PFX_BASE64` / `SIGN_PFX_PASSWORD` for CI signing, with timestamping
  via the DigiCert RFC 3161 TSA.
- **Repository bootstrap** — Apache 2.0 LICENSE, NOTICE, AUTHORS,
  MAINTAINERS, CONTRIBUTING, CODE_OF_CONDUCT, SECURITY, .editorconfig,
  CHANGELOG, RELEASE_NOTES template and a basic CI workflow.

### Changed
- The Manager sidebar is now data-driven from the generated catalog — adding
  a category to `ExtensionCatalog.cs` automatically surfaces it in the
  sidebar with the correct count.
- Default selection in the Manager opens on `.md` (Markdown Document) so the
  inspector demonstrates the templated-file flow out of the box.
- High-risk extensions (`.exe`, `.bat`, `.cmd`, `.com`, `.ps1`, `.vbs`,
  `.wsf`, `.crx`, `.msi`, `.scr`, `.hta`, `.pif`, `.sys`) are classified
  `risk: high` and ship `state: disabled` by default — enforced by both the
  catalog generator and the xUnit invariants.

### Security
- Apply pipeline still refuses any non-HKCU keypath at the entry point.
- FileCreator IPC uses a per-install DPAPI-protected secret with constant-time
  validation and a 10 req/sec throttle.
- Command templates remain static-by-default; execution requires explicit
  `commandApproved=true` and passes the two-gate sanitizer.

## [1.2.0] — 2026-04-20

### Added
- Snapshot → apply → verify → rollback pipeline with signed audit log.
- DPAPI-authenticated named-pipe IPC for `CM.EDITAR.FileCreator`.
- Optional `New+` per-user submenu.
- WiX 4 MSI installer with per-user install scope.

## [1.1.0] — 2026-03-15

### Added
- Initial Avalonia UI wired to the registry / templates / apply services.
- Snapshot / undo support.
- Cross-compile support on Linux / macOS for CI authoring (Windows-only
  services become no-ops or throw at runtime).

## [1.0.0] — 2026-02-10

### Added
- First public preview: read-only discovery of the Windows `New` submenu and
  static templated file types.

[1.3.0]: https://github.com/RealHaywoodJ/CM.EDITAR-Plus/releases/tag/v1.3.0
[1.2.0]: https://github.com/RealHaywoodJ/CM.EDITAR-Plus/releases/tag/v1.2.0
[1.1.0]: https://github.com/RealHaywoodJ/CM.EDITAR-Plus/releases/tag/v1.1.0
[1.0.0]: https://github.com/RealHaywoodJ/CM.EDITAR-Plus/releases/tag/v1.0.0
