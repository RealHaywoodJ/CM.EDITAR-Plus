# Contributing to CM.EDITAR+

Thanks for taking an interest in the project! CM.EDITAR+ is a Windows desktop
utility for safely managing the Windows Explorer **New** submenu — pull
requests, bug reports and design ideas are all welcome.

## Code of conduct

This project follows the [Contributor Covenant v2.1](CODE_OF_CONDUCT.md). By
participating you agree to abide by it.

## How to file a bug or feature request

1. Check existing issues first — there's a good chance someone already filed it.
2. Open a GitHub Issue with:
   - A clear, specific title.
   - Repro steps (what you did, what you expected, what happened).
   - Your Windows build, .NET runtime version, and CM.EDITAR+ version
     (Help → About).
   - Logs from `%LocalAppData%\CM.EDITAR+\Logs\` if relevant — please redact
     anything sensitive first.

## How to open a pull request

1. **Fork & branch** from `main`. Use a short, descriptive branch name
   (e.g. `feat/template-export`, `fix/snapshot-rollback`).
2. **Discuss first** for anything bigger than a small fix — open an Issue or
   Discussion so we can agree on the shape before you spend time coding.
3. **Build & test on Windows** before pushing:
   ```powershell
   dotnet restore CM.EDITAR.sln
   dotnet build   CM.EDITAR.sln -c Release
   dotnet test    CM.EDITAR.sln -c Release
   ```
4. **Tests are required.** Every PR must include unit or integration tests
   covering the change. Bug fixes need a regression test that fails on `main`
   and passes on your branch.
5. **Code style:** follow the existing C# conventions (see `.editorconfig`).
   Run `dotnet format` before committing.
6. **Commit messages** should be in the imperative mood
   (`Add export-pack command`, not `Added export-pack command`). Reference the
   issue number where relevant: `Fix #42 — restore snapshot on verify failure`.
7. **PR description must include:**
   - A short summary of the change.
   - The motivation (linked issue if applicable).
   - Any user-visible behaviour changes (screenshots welcome for UI work).
   - A note on the test coverage you added.
8. **Sign-off:** by submitting a PR you agree to license your contribution
   under the Apache License 2.0 — the same license as the project.

## Project layout

See the architecture overview in [README.md](README.md#architecture). Key
points:

- `src/CM.EDITAR.UI` — Avalonia MVVM desktop app.
- `src/CM.EDITAR.Registry` — HKCU registry I/O. Never widen the keypath guard.
- `src/CM.EDITAR.ApplyService` — snapshot → apply → verify → rollback pipeline.
- `src/CM.EDITAR.Templates` — template engine, sanitizer, and the
  `ExtensionCatalog.cs` single source of truth for the 400+ extension catalog.
- `src/CM.EDITAR.FileCreator` — CLI + DPAPI-authenticated named-pipe IPC.
- `tests/*.Tests` — xUnit test projects, one per `src/` project.

## Touching the extension catalog

`src/CM.EDITAR.Templates/ExtensionCatalog.cs` is the **single source of truth**
for every wired extension. After editing it run:

```bash
pnpm generate:catalog
```

This regenerates `artifacts/mockup-sandbox/src/components/mockups/cm-editar/catalog.generated.json`
and runs the parser's correctness gates (entry-count parity, enum validation,
high-risk-must-be-disabled). The xUnit invariants in
`tests/CM.EDITAR.Templates.Tests/ExtensionCatalogTests.cs` enforce the same
rules at build time.

## Reporting security issues

**Do not** open a public Issue for security vulnerabilities. Please email
<dev@eth-munson.com> — see [SECURITY.md](SECURITY.md) for details.
