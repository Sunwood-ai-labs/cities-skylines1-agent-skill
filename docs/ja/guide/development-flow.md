# 開発フロー

このプロジェクトでは、軽量な Git Flow を使います。

## ブランチ

- `main` はリリース済み、または公開可能な状態を置く本番ブランチです。
- `develop` は次リリースに向けた統合ブランチです。
- `codex/feature/*` は Codex が担当する通常機能ブランチです。
- `feature/*` は人間が担当する通常機能ブランチとして使えます。
- `release/*` はリリース安定化用です。
- `hotfix/*` は `main` から切る緊急修正用です。

## 通常の機能開発

機能ブランチは `develop` から切り、Pull Request も `develop` に向けます。

```powershell
git fetch --prune origin
git switch develop
git pull --ff-only origin develop
git switch -c codex/feature/<short-topic>
```

PR を出す前に、軽量なドキュメント検証を実行します。

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\check-doc-links.ps1
pushd docs
npm install
npm run build
popd
```

MOD の実行時挙動を変えた場合は、可能な範囲で Cities: Skylines 1 上のスモークテストも行います。

## レビューポリシー

通常の機能 PR は `develop` を向け先にし、次の情報を含めます。

- ユーザーから見える変更点
- 検証結果
- API レスポンス形状を変えた場合のメモ
- ChatGPT または Gemini のレビュー結果

AI レビューはリスクを見つけるための補助として使います。ゲーム実行時の安全性やリリース判断は、最終的にメンテナーが確認します。

## リリース

`release/<version>` は `develop` から切り、安定化、リリースノート、ドキュメント同期に変更範囲を絞ります。リリース PR は `main` にマージし、タグは `main` から作ります。最後にリリース中の修正が `develop` に戻っていることを確認します。

## ホットフィックス

`hotfix/<topic>` は `main` から切ります。`main` にマージしたあと、同じ修正を `develop` に戻して次リリースで再発しないようにします。
