# Pending workflow activation

  This directory holds the project's CI workflow as published via the GitHub
  REST API. The OAuth scope used for the v1.3.0 publish did not include
  `workflow`, so GitHub rejected the file at the canonical
  `.github/workflows/` path.

  ## To activate CI

  ```bash
  git mv .github/workflows-pending .github/workflows
  git commit -m "ci: move workflow into .github/workflows to activate"
  git push
  ```

  This will run the existing `ci.yml` (dotnet build/test on Windows + Linux,
  pnpm typecheck of the design mockup, and a catalog-drift gate) on every push
  and PR to `main`.
  