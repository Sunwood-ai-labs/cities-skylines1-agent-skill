# v0.1.0 リリースノート

![v0.1.0 release header](/release-header-v0.1.0.svg)

`v0.1.0` は `cities-skylines1-agent-skill` の初回リリースです。過去タグは存在しないため、このノートはタグに含まれるリポジトリ全体を対象にしています。

## ハイライト

- Cities: Skylines 1 MOD として、`http://127.0.0.1:32123` で動くローカル HTTP ブリッジを追加しました。
- 都市概要、問題アイコン、施設、ネットワーク、道路異常、建物配置異常、セーブ、読み込み済み Prefab を読む API を追加しました。
- ネットワーク作成、ゾーン設定、建物配置・移動、ブルドーズ、シミュレーション速度、限定的なバッチ操作、保存のコマンド API を追加しました。
- CS1 のゲームスレッド上で状態変更を実行し、API 操作をゲーム内通知として表示します。
- `SKILL.md` と `agents/openai.yaml` により、Codex Skill として利用できる形にしました。
- 英日ガイド、API リファレンス、スクリーンショット、GitHub Pages ワークフローを含む VitePress docs を整備しました。

## Agent Bridge MOD

MOD 本体は `src/` にあります。`ApiServer.cs` がエンドポイントを登録し、`GameState.cs` が CS1 のシミュレーションデータを読み取ります。コマンドは、道路などのネットワーク、ゾーン、建物配置、ブルドーズ、保存、速度変更、限定的なバッチ操作に分かれています。

この API は「先に調べる」ことを重視しています。エージェントはスクリーンショット認識に頼らず、都市グラフや施設状態を読んだうえで、小さなコマンドを1つずつ実行し、結果を再確認できます。

## ツールと自動化

`scripts/` にはローカル運用のための PowerShell スクリプトが含まれます。

- `build.ps1`: `SkylinesAgentBridge.dll` をコンパイルしてインストールします。
- `start-resume.ps1` / `start-new-map.ps1`: CS1 の起動フローを進め、API の起動を待ちます。
- `smoke-test.ps1`: health、状態取得、Prefab、dry-run コマンドを確認します。
- `inspect-road-anomalies.ps1` / `repair-road-anomalies.ps1` / `repair-service-overlap.ps1`: 範囲を絞った修復ワークフローを支援します。
- `save-city.ps1`: 保存を要求し、`/state/saves` で保存ファイルを確認します。
- `check-doc-links.ps1`: CI で利用する docs リンク検証を行います。

## Docs とアセット

docs には次の内容が含まれます。

- 公開リポジトリ向けの `README.md` と `README.ja.md`。
- `docs/` 配下の VitePress docs。はじめに、エージェント運用、構成、トラブルシュート、API リファレンスを含みます。
- `docs/ja/` 配下の日本語 docs。
- ゲーム内 API 通知オーバーレイと、エージェントが作成した都市のスクリーンショット。
- `.github/workflows/` 配下の docs 検証と Pages デプロイ。

## 検証

このリリースノート作成では、`gh-release-notes` の collector を初回リリースモードで実行し、API、スクリプト、docs、workflow の主要ファイルを確認しました。SVG リリースアセットも検証しています。

今回実行した追加チェック:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File C:\Users\makim\.codex\skills\gh-release-notes\scripts\verify-svg-assets.ps1 -RepoPath . -Path docs/public/agent-bridge-icon.svg
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\check-doc-links.ps1
npm install
npm run build
```

このリリースノートは `v0.1.0` タグに含まれるリポジトリ状態をもとに作成しています。

## リンク

- [はじめに](/ja/guide/getting-started)
- [エージェント運用](/ja/guide/usage)
- [構成](/ja/guide/architecture)
- [APIリファレンス](/ja/api)
- [English release notes](/releases/v0.1.0/)
