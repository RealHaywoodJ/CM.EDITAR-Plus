# Security Policy

CM.EDITAR+ writes to the Windows Registry and ships a privileged-feeling tool,
so we take vulnerability reports seriously. Please follow the process below
rather than opening a public issue.

## Reporting a vulnerability

Email **<dev@eth-munson.com>** with:

- A description of the issue.
- The version of CM.EDITAR+ affected (Help → About → version string).
- Repro steps or a proof of concept.
- Your assessment of impact (e.g. local privilege escalation, registry
  corruption, IPC bypass, signature bypass).

You can expect:

- An acknowledgement within **3 business days**.
- A triage decision within **7 business days**.
- A fix or mitigation, plus a coordinated disclosure timeline, within
  **30 days** of triage for confirmed High/Critical issues.

## Do not

- Open a public GitHub Issue for an unfixed vulnerability.
- Post repro details on social media or developer forums until a fix has
  shipped.
- Test on machines that are not your own.

## Supported versions

The latest released minor version (currently **v1.3.x**) receives security
fixes. Older minor versions are best-effort only.

## Scope

In scope:

- The desktop UI (`CM.EDITAR.UI`) and its registry I/O.
- The `CM.EDITAR.FileCreator` CLI and its DPAPI-authenticated named-pipe IPC.
- The MSI installer produced by `scripts/build-installer.ps1`.
- The template Command sanitizer in `CM.EDITAR.Templates`.

Out of scope:

- Vulnerabilities in upstream Windows components, .NET, or Avalonia — please
  report those to their respective vendors.
- Issues that require an attacker to already have administrative access on
  the target machine.

## Hall of fame

Reporters who responsibly disclose confirmed vulnerabilities are credited in
release notes (with their consent). If you'd prefer to remain anonymous, say
so in your report.
