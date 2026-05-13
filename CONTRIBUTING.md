# Contributing

[日本語](CONTRIBUTING.ja.md)

This repository uses a lightweight Git Flow model so CS1 mod changes, API surface changes, scripts, and documentation can be reviewed before they reach a release branch.

## Branch Model

- `main` is the production branch. It should match the latest released or publishable state.
- `develop` is the integration branch for the next release.
- `codex/feature/<short-topic>` or `feature/<short-topic>` branches are used for normal feature work.
- `release/<version>` branches are used only for stabilization, release notes, version updates, and final validation.
- `hotfix/<version-or-topic>` branches start from `main` for urgent production fixes, then get merged back into both `main` and `develop`.

Prefer `codex/feature/*` for agent-authored branches so automated work is easy to scan in GitHub.

## Feature Flow

1. Update local refs.

   ```powershell
   git fetch --prune origin
   ```

2. Start a feature branch from `develop`.

   ```powershell
   git switch develop
   git pull --ff-only origin develop
   git switch -c codex/feature/<short-topic>
   ```

3. Keep commits focused and rollback-friendly. Split docs, mod behavior, scripts, and test changes when they can stand alone.

4. Validate locally before opening a PR.

   ```powershell
   powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\check-doc-links.ps1
   pushd docs
   npm install
   npm run build
   popd
   ```

5. Push the feature branch and open a PR into `develop`.

   ```powershell
   git push -u origin codex/feature/<short-topic>
   gh pr create --base develop --head codex/feature/<short-topic>
   ```

6. Request human review plus at least one AI review pass from ChatGPT, Gemini, or both. Paste the AI review summary into the PR so the reasoning is auditable.

7. Apply review fixes on the same feature branch, re-run validation, and merge only after the PR checklist is complete.

## Release Flow

1. Create `release/<version>` from `develop`.
2. Limit changes to stabilization, release notes, documentation sync, and release-only fixes.
3. Open the release PR into `main`.
4. After merge, tag the release from `main`.
5. Merge or PR the release result back into `develop` if GitHub did not already keep it aligned.

## Hotfix Flow

1. Create `hotfix/<topic>` from `main`.
2. Make the smallest safe fix and validate it.
3. Open a PR into `main`.
4. After merge, back-merge the same fix into `develop`.

## AI Review Expectations

AI review is a second set of eyes, not a replacement for project ownership. Ask reviewers to focus on:

- runtime risks inside the CS1 mod and Unity API usage
- API response compatibility and JSON shape changes
- script safety on Windows PowerShell
- documentation drift between English and Japanese pages
- missing validation or release notes for user-facing behavior

When applying AI review comments, prefer small follow-up commits with clear titles instead of rewriting the whole branch history after review has started.
