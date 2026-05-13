# コントリビュート

[English](CONTRIBUTING.md)

このリポジトリでは、CS1 MOD、API、スクリプト、ドキュメントの変更をリリース前に確認しやすくするため、軽量な Git Flow を使います。

## ブランチモデル

- `main` は本番ブランチです。最新リリース済み、または公開可能な状態を置きます。
- `develop` は次リリースに向けた統合ブランチです。
- `codex/feature/<short-topic>` または `feature/<short-topic>` は通常の機能開発に使います。
- `release/<version>` は安定化、リリースノート、バージョン更新、最終検証に限定します。
- `hotfix/<version-or-topic>` は `main` から切る緊急修正用です。完了後は `main` と `develop` の両方へ戻します。

Codex が担当する作業は、GitHub 上で見分けやすいように `codex/feature/*` を優先します。

## 機能開発フロー

1. ローカルの参照を更新します。

   ```powershell
   git fetch --prune origin
   ```

2. `develop` から機能ブランチを切ります。

   ```powershell
   git switch develop
   git pull --ff-only origin develop
   git switch -c codex/feature/<short-topic>
   ```

3. コミットは小さく戻しやすい単位にします。ドキュメント、MOD 挙動、スクリプト、検証の変更は、自然に分けられるなら分けます。

4. PR 前にローカル検証を実行します。

   ```powershell
   powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\check-doc-links.ps1
   pushd docs
   npm install
   npm run build
   popd
   ```

5. 機能ブランチを push し、`develop` 向け PR を作ります。

   ```powershell
   git push -u origin codex/feature/<short-topic>
   gh pr create --base develop --head codex/feature/<short-topic>
   ```

6. 人間のレビューに加えて、ChatGPT または Gemini の AI レビューを少なくとも1回通します。AI レビュー結果は PR に貼り付け、判断の流れを追えるようにします。

7. レビュー修正は同じ機能ブランチに積み、再検証してから、PR チェックリスト完了後にマージします。

## リリースフロー

1. `develop` から `release/<version>` を切ります。
2. 変更は安定化、リリースノート、ドキュメント同期、リリース専用修正に絞ります。
3. `main` 向けのリリース PR を作ります。
4. マージ後、`main` からリリースタグを作ります。
5. GitHub 上で自動的に揃っていない場合は、リリース結果を `develop` に戻します。

## ホットフィックスフロー

1. `main` から `hotfix/<topic>` を切ります。
2. 最小限の安全な修正を入れて検証します。
3. `main` 向け PR を作ります。
4. マージ後、同じ修正を `develop` に戻します。

## AI レビューの観点

AI レビューはもう一つの目として使いますが、プロジェクトの最終判断を置き換えるものではありません。特に次を見てもらいます。

- CS1 MOD と Unity API の実行時リスク
- API レスポンス互換性と JSON 形状の変更
- Windows PowerShell スクリプトの安全性
- 英日ドキュメント間のずれ
- ユーザー向け挙動に対する検証やリリースノートの不足

AI レビュー後の修正は、レビュー開始後に履歴を大きく書き換えるより、小さな追加コミットとして積むことを優先します。
