<!--
Thanks for sending a PR! Please fill out the sections below.
For larger changes, please open an issue first to agree on the shape — see
CONTRIBUTING.md.
-->

## Summary

<!-- What does this PR do, in 1–3 sentences? -->

## Motivation

<!--
Why is this change needed? Link any related issues with "Fixes #123" or
"Refs #123" so they auto-close on merge where appropriate.
-->

## User-visible changes

<!--
If this changes anything users will see (UI, behaviour, defaults, file
locations, etc.), describe it here. Screenshots are very welcome for UI work.
-->

## Test coverage

<!--
What tests did you add or update? For bug fixes, please include a regression
test that fails on `main` and passes on this branch.
-->

## Checklist

- [ ] Built and tested on Windows (`dotnet build CM.EDITAR.sln -c Release` and
      `dotnet test CM.EDITAR.sln -c Release` both pass).
- [ ] Added or updated unit / integration tests covering the change.
- [ ] Ran `dotnet format` and the change matches existing style
      (see `.editorconfig`).
- [ ] Updated `CHANGELOG.md` under the Unreleased / next-version section if
      this is a user-visible change.
- [ ] If this touches `src/CM.EDITAR.Templates/ExtensionCatalog.cs`, also ran
      `pnpm generate:catalog` and committed the regenerated
      `catalog.generated.json`.
- [ ] My commit messages are in the imperative mood
      (`Add foo`, not `Added foo`).
- [ ] By submitting this PR I agree to license my contribution under the
      Apache License 2.0.
