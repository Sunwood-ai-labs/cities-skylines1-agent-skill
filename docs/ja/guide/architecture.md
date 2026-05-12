# 構成

このリポジトリは、ローカル API を公開する CS1 MOD と、その API を安全に使うための Codex Skill で構成されています。

## 実行時の流れ

```text
Codex agent
  -> PowerShell scripts
  -> http://127.0.0.1:32123
  -> SkylinesAgentBridge mod
  -> CS1 game thread queue
  -> game managers and save panel
```

API サーバーはローカル HTTP リクエストを受け取ります。状態取得と変更コマンドはゲームスレッドキューに渡され、CS1 のシミュレーションオブジェクトを適切なスレッドで触ります。

## ソース構成

```text
src/
├── ApiServer.cs
├── GameState.cs
├── RoadCommands.cs
├── BuildingCommands.cs
├── ZoneCommands.cs
├── BulldozeCommands.cs
├── SimulationCommands.cs
├── SaveCommands.cs
├── BatchCommands.cs
└── AgentBridge*.cs
```

`ApiServer.cs` は HTTP パスを状態生成またはコマンドハンドラーへ割り当てます。`GameState.cs` は都市状態の JSON を作ります。各 command ファイルは変更処理を分け、エージェントが1つの明確な操作を選べるようにしています。

## ゲーム内通知

ゲーム状態に触る API 呼び出しは、CS1 内に短いオーバーレイ通知を表示します。これにより、エージェント作業中もリクエストが MOD に届いていることを確認できます。

`/health` は都市ロード前にも呼ばれるため、通知対象から外しています。

## Skill メタデータ

`SKILL.md` にはエージェントの運用手順があります。`agents/openai.yaml` は Codex skill surface 向けの表示名、デフォルトプロンプト、暗黙呼び出し設定です。
