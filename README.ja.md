<p align="center">
  <img src="docs/assets/readme-header.png" width="100%" alt="Cities: Skylines Agent Bridge header">
</p>

<h1 align="center">cities-skylines1-agent-skill</h1>

<p align="center">
  Cities: Skylines 1 の都市を AI エージェントから API 経由で調査・修復・建設・ゾーン設定・保存するための Codex Skill 兼 CS1 MOD です。
</p>

<p align="center">
  <a href="README.md">English README</a> ·
  <a href="https://sunwood-ai-labs.github.io/cities-skylines1-agent-skill/ja/">Docs 日本語</a> ·
  <a href="docs/ja/api.md">APIリファレンス</a> ·
  <a href="CONTRIBUTING.ja.md">コントリビュート</a>
</p>

<p align="center">
  <a href="https://github.com/Sunwood-ai-labs/cities-skylines1-agent-skill/actions/workflows/docs.yml"><img alt="Docs workflow" src="https://github.com/Sunwood-ai-labs/cities-skylines1-agent-skill/actions/workflows/docs.yml/badge.svg"></a>
  <a href="https://github.com/Sunwood-ai-labs/cities-skylines1-agent-skill/actions/workflows/pages.yml"><img alt="Pages workflow" src="https://github.com/Sunwood-ai-labs/cities-skylines1-agent-skill/actions/workflows/pages.yml/badge.svg"></a>
  <a href="LICENSE"><img alt="License: MIT" src="https://img.shields.io/badge/license-MIT-green.svg"></a>
  <img alt="Platform: Windows" src="https://img.shields.io/badge/platform-Windows-blue.svg">
  <img alt="Game: Cities Skylines 1" src="https://img.shields.io/badge/game-Cities%3A%20Skylines%201-2ec4b6.svg">
</p>

狙いはシンプルです。都市状態の把握をスクリーンショット認識に頼らず、Cities: Skylines 1 のデータをローカル API で返し、エージェントが「道路セグメントを消す」「道路を作る」「施設を置く」「ゾーンを塗る」「速度を変える」「保存する」といった小さな操作を積み上げられるようにします。

![Cities: Skylines 1 のAPI通知オーバーレイ](docs/assets/api-notification.jpg)

## ✨ できること

- CS1 MODとして `http://127.0.0.1:32123` にローカル HTTP API を立てます。
- 問題アイコン、施設、道路/水道/暖房/電線、道路異常、建物配置異常、セーブ、Prefab を API で取得できます。
- 道路/配管/電線作成、ゾーン設定、建物配置、建物移動、削除、速度変更、保存、簡易バッチを API で実行できます。
- API が実行されるたび、ゲーム画面左上に「何をしたか」の通知を表示します。
- ビルド、Resume 起動、新規マップ起動、都市検査、限定修復、保存用の Windows PowerShell スクリプトを含みます。
- [SKILL.md](SKILL.md) と [agents/openai.yaml](agents/openai.yaml) により Codex Skill として使えます。

## 🖼️ スクリーンショット

### API通知

ゲーム状態に触る API を叩くと、CS1 画面上に数秒だけ通知が出ます。

![API通知](docs/assets/api-notification.jpg)

### エージェントが作ったスターター都市

最新セーブを Resume し、API で問題を調べ、必要な箇所だけ修復して都市を継続開発できます。

![エージェントが作った都市](docs/assets/city-overview.jpg)

### 道路修復ワークフロー

道路の異常は画像認識ではなく、CS1 のネットワークデータから検出します。検出後は、削除 API と作成 API を分けて叩いて修復します。

## 🚀 クイックスタート

CS1 のインストール先が違う場合は `scripts/build.ps1` を調整してから、MOD をビルドしてインストールします。

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\build.ps1
```

ビルド後、DLL はここへコピーされます。

```text
%LOCALAPPDATA%\Colossal Order\Cities_Skylines\Addons\Mods\SkylinesAgentBridge
```

CS1 のコンテンツマネージャーで MOD を有効化し、都市をロードしたら確認します。

```powershell
Invoke-RestMethod http://127.0.0.1:32123/health
Invoke-RestMethod http://127.0.0.1:32123/state/summary
```

開発では軽量な Git Flow を使います。通常の機能ブランチは `develop` に向け、リリースとホットフィックスは `main` に向けます。ブランチ運用と AI レビューの流れは [CONTRIBUTING.ja.md](CONTRIBUTING.ja.md) を参照してください。

通常のエージェントループでは、最新セーブを Resume します。

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\start-resume.ps1
```

検証用にまっさらな都市を作る場合はこちらです。

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\start-new-map.ps1
```

## 🧭 エージェント修復の基本方針

汎用性のため、魔法の一括修復 API ではなく、小さな API を組み合わせます。

1. `/state/problems`、`/state/road-anomalies`、`/state/building-anomalies`、`/state/facilities`、`/state/networks` で調べます。
2. 悪いオブジェクトを `/commands/bulldoze` で消します。
3. `/commands/build-network`、`/commands/place-building`、`/commands/move-building`、`/commands/set-zone` で作り直します。
4. `/commands/set-simulation-speed` で少し時間を進めます。
5. もう一度状態 API を見ます。
6. `/commands/save` または `scripts/save-city.ps1` で保存し、`/state/saves` でファイル生成を確認します。

## 🔌 API

取得 API:

- `GET /health`
- `GET /state/summary`
- `GET /state/problems`
- `GET /state/facilities`
- `GET /state/networks`
- `GET /state/road-anomalies`
- `GET /state/building-anomalies`
- `GET /state/saves`
- `GET /prefabs/roads`
- `GET /prefabs/networks`
- `GET /prefabs/buildings`

操作 API:

- `POST /commands/build-network`
- `POST /commands/build-road` 互換エイリアス
- `POST /commands/set-zone`
- `POST /commands/place-building`
- `POST /commands/move-building`
- `POST /commands/bulldoze`
- `POST /commands/save`
- `POST /commands/set-simulation-speed`
- `POST /commands/batch` おまけの一括実行

詳細は [docs/ja/api.md](docs/ja/api.md) を参照してください。

## 🧩 Skillとして使う

このリポジトリ自体が Codex Skill として使える構成です。ルートの [SKILL.md](SKILL.md) に、エージェントが CS1 を API 操作するための手順を書いてあります。

プロンプト例:

```text
Use $cities-skylines1-agent-skill to resume my CS1 city, inspect current problems, repair road/infrastructure issues with separate API calls, save the city, and report what changed.
```

## 📚 ドキュメント

- [Docs 日本語](https://sunwood-ai-labs.github.io/cities-skylines1-agent-skill/ja/)
- [はじめに](docs/ja/guide/getting-started.md)
- [エージェント運用](docs/ja/guide/usage.md)
- [構成](docs/ja/guide/architecture.md)
- [トラブルシュート](docs/ja/guide/troubleshooting.md)
- [APIリファレンス](docs/ja/api.md)
- [実験記事: CodexにCities: Skylinesの都市建設をやらせてみた](docs/articles/building-cities-skylines-with-ai-agents-ja.md)

## 🗂️ リポジトリ構成

```text
.
├── SKILL.md                 # Codex Skill手順
├── agents/openai.yaml       # Skill UIメタデータ
├── src/                     # CS1 MODソース
├── scripts/                 # ビルド、起動、検査、修復、保存、QAスクリプト
├── docs/                    # VitePress docs と API reference
└── .github/workflows/       # Docs検証と Pages deployment
```

## ⚠️ 状態

CS1 on Windows 向けの実験プロジェクトです。まずは捨てセーブで試してください。ブリッジはゲームスレッドに積んだコマンドで CS1 の live simulation object を変更するため、小さく変更して毎回確認してください。

## 📄 ライセンス

MIT。詳細は [LICENSE](LICENSE) を参照してください。
