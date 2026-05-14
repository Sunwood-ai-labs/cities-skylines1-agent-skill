# Agent Instructions

This repository uses a lightweight Git Flow development model. Agents must follow it for all repository changes.

## Required Git Flow

- Treat `main` as the production branch and `develop` as the integration branch for the next release.
- Before starting any change, fetch the remote state and create a working branch from `develop`.
- Use `codex/feature/<short-topic>` for normal agent-authored work.
- Use `release/<version>` only for release stabilization work.
- Use `hotfix/<version-or-topic>` only for urgent production fixes that start from `main`; back-merge finished hotfixes into `develop`.
- Do not commit directly to `main` or `develop` for normal development.

## Normal Agent Workflow

1. Start from an up-to-date `develop`.

   ```powershell
   git fetch --prune origin
   git switch develop
   git pull --ff-only origin develop
   git switch -c codex/feature/<short-topic>
   ```

2. Make the requested changes on the feature branch.
3. Keep commits focused and rollback-friendly.
4. Run the relevant local validation before review.
5. Push the branch and open a pull request targeting `develop`.

   ```powershell
   git push -u origin codex/feature/<short-topic>
   gh pr create --base develop --head codex/feature/<short-topic>
   ```

6. Request review before merging. Include human review when available and use an AI review pass from ChatGPT, Gemini, or both when useful.
7. Apply review feedback on the same feature branch, re-run validation, and update the PR.
8. Merge into `develop` only after the PR is approved and the validation checklist is complete.

## Review Notes

- Every PR should summarize user-visible behavior, validation results, and any changed API response shapes.
- Documentation changes should keep English and Japanese docs aligned when both surfaces are affected.
- Runtime changes should be smoke-tested in Cities: Skylines 1 when possible.
- If a requested action conflicts with this flow, stop and explain the safest Git Flow-compatible path.
