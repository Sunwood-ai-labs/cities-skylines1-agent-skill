# Development Flow

This project uses a lightweight Git Flow model.

## Branches

- `main` is the production branch for released or publishable code.
- `develop` is the integration branch for the next release.
- `codex/feature/*` is preferred for Codex-authored work.
- `feature/*` is fine for human-authored feature branches.
- `release/*` is for release stabilization.
- `hotfix/*` is for urgent fixes that start from `main`.

## Normal Feature Work

Create feature branches from `develop` and open pull requests back into `develop`.

```powershell
git fetch --prune origin
git switch develop
git pull --ff-only origin develop
git switch -c codex/feature/<short-topic>
```

Before opening the PR, run the lightweight documentation checks:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\check-doc-links.ps1
pushd docs
npm install
npm run build
popd
```

Runtime changes should also be smoke-tested in Cities: Skylines 1 when possible.

## Review Policy

Every feature PR should target `develop` and include:

- a short summary of user-visible behavior
- validation results
- notes about changed API response shapes
- ChatGPT or Gemini review notes pasted into the PR

Use AI review for risk discovery. Keep the final decision with the maintainer, especially for game runtime behavior and release timing.

## Releases

Create `release/<version>` from `develop`, then restrict changes to stabilization, release notes, and documentation sync. Merge release PRs into `main`, tag the release from `main`, and make sure `develop` receives the final release fixes.

## Hotfixes

Create `hotfix/<topic>` from `main`. After the hotfix is merged into `main`, back-merge it into `develop` so the next release does not regress.
