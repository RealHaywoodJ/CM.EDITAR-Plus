# Release Notes Template

> Copy this file to `RELEASE_NOTES.vX.Y.Z.md` (or paste it into the GitHub
> Release body) and fill in the bracketed sections. Keep entries short — link
> to the relevant CHANGELOG section for detail.

## CM.EDITAR+ vX.Y.Z — `<headline summary>`

**Released:** YYYY-MM-DD
**Maintainer:** SirSHAmun5on12 — <dev@eth-munson.com>

### Highlights

- [ ] One-line summary of the marquee feature.
- [ ] Second highlight (optional).
- [ ] Third highlight (optional).

### Downloads

| Asset                              | SHA-256                                    |
| ---------------------------------- | ------------------------------------------ |
| `CM.EDITAR.Setup.msi` (signed)     | `<sha256>`                                 |
| `CM.EDITAR.Portable.zip` (signed)  | `<sha256>`                                 |

> Verify with `Get-FileHash <path> -Algorithm SHA256` on Windows.

### What's new

See the [CHANGELOG](CHANGELOG.md#xyz---yyyy-mm-dd) for the full list. In short:

- **Added** — `<bullet>`
- **Changed** — `<bullet>`
- **Fixed** — `<bullet>`
- **Security** — `<bullet>`

### Upgrade notes

- Backups are still HKCU-only and stored under
  `%LocalAppData%\CM.EDITAR+\Backups\`. The installer takes a snapshot before
  upgrading; uninstall restores it.
- If you have edited built-in templates, the upgrade flow will prompt you per
  template.

### Known issues

- [ ] `<known issue>` — workaround: `<...>`.

### Credits

Thanks to everyone who filed issues, tested release candidates, or sent
patches for this release. See [AUTHORS.md](AUTHORS.md) for the full list.
