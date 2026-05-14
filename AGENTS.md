# Agent Instructions

This repository uses a lightweight Git Flow development model. Agents must follow it for all repository changes.

## Required Git Flow

- Treat `main` as the production branch and `develop` as the integration branch for the next release.
- Before starting any change, fetch the remote state and create a working branch from `develop`.
- Use `codex/feature/<short-topic>` for normal agent-authored work.
- Use `release/<version>` only for release stabilization work.
- Use `hotfix/<version-or-topic>` only for urgent production fixes that start from `main`; merge finished hotfixes into `main`, then back-merge them into `develop`.
- Do not commit directly to `main` or `develop` for normal development.
- If `develop` is missing in a fresh clone or new repository setup, stop normal feature work and create or restore `develop` from the current production branch before continuing.

## Normal Agent Workflow

1. Start from an up-to-date `develop`.

   ```powershell
   git fetch --prune origin
   git switch develop
   git pull --ff-only origin develop
   git switch -c codex/feature/<short-topic>
   ```

   If `develop` does not exist yet, initialize it before creating feature work.

   ```powershell
   git fetch --prune origin
   git switch main
   git pull --ff-only origin main
   git switch -c develop
   git push -u origin develop
   ```

2. Prefer a dedicated Git worktree when the main working tree is dirty, when another branch is already checked out, or when parallel agent work is useful.

   ```powershell
   git fetch --prune origin
   git worktree add ..\<repo-name>-<short-topic> -b codex/feature/<short-topic> origin/develop
   cd ..\<repo-name>-<short-topic>
   ```

3. Make the requested changes on the feature branch or in the dedicated worktree.
4. Commit early and often so the branch stays easy to review, bisect, and roll back.
5. Run the relevant local validation before review.
6. Push the branch and open a pull request targeting `develop`.

   ```powershell
   git push -u origin codex/feature/<short-topic>
   gh pr create --base develop --head codex/feature/<short-topic>
   ```

7. Request review before merging. Include human review when available and use an AI review pass from ChatGPT, Gemini, or both. Paste the AI review summary into the PR so the reasoning is auditable.
8. Apply review feedback on the same feature branch, re-run validation, and update the PR.
9. Merge into `develop` only after the PR is approved and the validation checklist is complete.

## Git Worktree Usage

- Use `git worktree` to keep feature work isolated from the primary checkout, especially when local uncommitted work exists.
- Create each worktree from `origin/develop` for normal feature work so it follows the same Git Flow base as a regular feature branch.
- Keep one task per worktree and one feature branch per task.
- Remove finished worktrees only after the PR is merged and the branch is no longer needed.

   ```powershell
   git worktree remove ..\<repo-name>-<short-topic>
   git branch -d codex/feature/<short-topic>
   ```

## Commit Discipline

- Prefer small, focused commits over large end-of-task commits.
- Commit after each coherent step, such as docs wording, implementation logic, test coverage, or review feedback.
- Keep every commit in a state that is understandable, buildable, and easy to revert.
- Use extra commits as recovery points before risky edits or broad mechanical changes.
- Do not mix unrelated code, docs, scripts, and generated assets in one commit when they can be separated safely.
- Write commit messages in English.
- Prefix each commit title with an emoji that makes the change type easy to scan.
- Make the commit message understandable to a third-party reviewer who did not watch the work happen.
- Use a short title plus a bullet-list body for non-trivial commits so the reason, scope, and validation are easy to read.
- Mention validation results, skipped checks, or follow-up risks in the commit body when they matter.

   ```text
   📝 Document frequent agent commits

   - Explain why agents should commit in small recovery points.
   - Clarify that commits should stay buildable for bisecting.
   - Validation: scripts/check-doc-links.ps1.
   ```

## Review Notes

- Every PR should summarize user-visible behavior, validation results, and any changed API response shapes.
- Documentation changes should keep English and Japanese docs aligned when both surfaces are affected.
- Runtime changes should be smoke-tested in Cities: Skylines 1 using `scripts/smoke-test.ps1` when possible.
- If a requested action conflicts with this flow, stop and explain the safest Git Flow-compatible path.
