# cities-skylines1-agent-skill

[English README](README.md)

Cities: Skylines 1 をAIエージェントから操作するための、Codex Skill兼CS1 MOD実験リポジトリです。

狙いは、スクリーンショット認識に頼らず、都市状態をAPIで取得し、エージェントが「この道路を削除」「ここに道路を作る」「施設を置く」「ゾーンを塗る」「保存する」といった小さな操作を組み合わせて都市を育てられるようにすることです。

![Cities: Skylines 1 のAPI通知オーバーレイ](docs/assets/api-notification.jpg)

## できること

- CS1 MODとして `http://127.0.0.1:32123` にローカルHTTP APIを立てます。
- 問題アイコン、施設、道路/水道/暖房/電線、道路異常、建物配置異常、セーブ、PrefabをAPIで取得できます。
- 道路/配管/電線作成、ゾーン設定、建物配置、建物移動、削除、速度変更、保存をAPIで実行できます。
- APIが実行されるたび、ゲーム画面左上に「何をしたか」の通知を表示します。
- ビルド、Resume起動、新規マップ起動、都市作成、検査、保存用のWindows PowerShellスクリプトを含みます。

## スクリーンショット

### API通知

ゲーム状態に触るAPIを叩くと、CS1画面上に数秒だけ通知が出ます。

![API通知](docs/assets/api-notification.jpg)

### エージェントが作ったスターター都市

最新セーブをResumeし、APIで問題を調べ、必要な箇所だけ修復して都市を継続開発できます。

![エージェントが作った都市](docs/assets/city-overview.jpg)

### 道路修復ワークフロー

道路の異常は画像認識ではなく、CS1のネットワークデータから検出します。検出後は、削除APIと作成APIを分けて叩いて修復します。

## 記事

- [CodexにCities: Skylinesの都市建設をやらせてみた！ AI市長が「村」を爆誕させる実験](docs/articles/building-cities-skylines-with-ai-agents-ja.md)

## API

取得API:

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

操作API:

- `POST /commands/build-network`
- `POST /commands/build-road` 互換エイリアス
- `POST /commands/set-zone`
- `POST /commands/place-building`
- `POST /commands/move-building`
- `POST /commands/bulldoze`
- `POST /commands/save`
- `POST /commands/set-simulation-speed`
- `POST /commands/batch` おまけの一括実行

詳細は [docs/api.md](docs/api.md) を参照してください。

## Skillとして使う

このリポジトリ自体がCodex Skillとして使える構成です。ルートの [SKILL.md](SKILL.md) に、エージェントがCS1をAPI操作するための手順を書いてあります。

プロンプト例:

```text
Use $cities-skylines1-agent-skill to resume my CS1 city, inspect current problems, repair road/infrastructure issues with separate API calls, save the city, and report what changed.
```

## MODをビルドする

CS1のインストール先が違う場合は `scripts/build.ps1` を調整してから実行します。

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\build.ps1
```

ビルド後、DLLはここへコピーされます。

```text
%LOCALAPPDATA%\Colossal Order\Cities_Skylines\Addons\Mods\SkylinesAgentBridge
```

CS1のコンテンツマネージャーでMODを有効化し、都市をロードしたら確認します。

```powershell
Invoke-RestMethod http://127.0.0.1:32123/health
Invoke-RestMethod http://127.0.0.1:32123/state/summary
```

## 最新セーブをResumeする

通常の修復ループはこちらです。Steam経由で起動し、Paradox LauncherのResumeをクリックし、APIが使えるまで待ちます。

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\start-resume.ps1
```

## 新規マップから始める

検証用にまっさらな都市を作る場合はこちらです。

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\start-new-map.ps1
```

## エージェント修復の基本方針

汎用性のため、魔法の一括修復APIではなく、小さなAPIを組み合わせます。

1. `/state/problems`、`/state/road-anomalies`、`/state/building-anomalies`、`/state/facilities`、`/state/networks` で調べます。
2. 悪いオブジェクトを `/commands/bulldoze` で消します。
3. `/commands/build-network`、`/commands/place-building`、`/commands/move-building`、`/commands/set-zone` で作り直します。
4. `/commands/set-simulation-speed` で少し時間を進めます。
5. もう一度状態APIを見ます。
6. `/commands/save` で保存し、`/state/saves` でファイル生成を確認します。

## 状態

CS1 on Windows向けの実験プロジェクトです。まずは捨てセーブで試してください。

## リポジトリ構成

```text
.
├── SKILL.md                 # Codex Skill手順
├── agents/openai.yaml       # Skill UIメタデータ
├── src/                     # CS1 MODソース
├── scripts/                 # ビルド、起動、検査、修復、保存スクリプト
├── docs/api.md              # APIリファレンス
└── docs/assets/             # README用スクリーンショット
```

## ライセンス

MIT。詳細は [LICENSE](LICENSE) を参照してください。
