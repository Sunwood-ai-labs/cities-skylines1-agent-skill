# APIリファレンス

Base URL:

```text
http://127.0.0.1:32123
```

都市ロード中にゲーム状態へ触る API を呼ぶと、CS1 の UI に数秒間の通知が表示されます。`/health` は都市ロード前にも呼ばれるため通知対象外です。

![API通知オーバーレイ](../assets/api-notification.jpg)

## エージェントワークフロー

1. API から状態を読む。
2. 必要な小さな変更を決める。
3. 1つの command API を呼ぶ。
4. シミュレーションを少し進める。
5. 状態を再取得する。
6. 保存して、保存ファイルを確認する。

![エージェントが作ったスターター都市](../assets/city-overview.jpg)

## GET /health

都市ロードを必要とせず、ブリッジ状態を返します。

## GET /state/summary

ゲーム時刻、build index、ネットワーク数、人口、需要などの小さな都市サマリーを返します。

## GET /prefabs/roads

道路として扱える読み込み済み `NetInfo` prefab を返します。

## GET /prefabs/networks

読み込み済み network prefab を返します。任意で service filter を指定できます。

```http
GET /prefabs/networks?service=Water
```

## GET /prefabs/buildings

読み込み済み building prefab を返します。任意で service filter を指定できます。

```http
GET /prefabs/buildings?service=Electricity
```

## GET /state/problems

スクリーンショットや画像認識ではなく、CS1 データから問題アイコンを返します。

```http
GET /state/problems?limit=200
```

レスポンスには従来の結合済み `problems` 文字列に加えて、個別判定しやすい
`problemNames`、`problem1Raw`、`problem2Raw`、`countsByProblem` も含まれます。
これにより `TaxesTooHigh` のようなアラートを、major/fatal と組み合わさって
いても拾えます。
building では `Abandoned`、`BurnedDown`、`Collapsed`、`Flooded`、
`RoadAccessFailed` のようなアラート相当のフラグも拾います。

対象 entity type:

- `building`
- `netNode`
- `netSegment`

## GET /state/economy

住宅・商業・産業・オフィス系ゾーンの税率を返します。

```powershell
Invoke-RestMethod http://127.0.0.1:32123/state/economy
```

## GET /state/facilities

現在の建物を CS1 service ごとに返します。施設には prefab footprint と回転も含まれるため、道路を貫通するような配置を避けやすくなります。

通常は `Water Pipe Junction` や `Heating Pipe Junction` のような内部ヘルパー建物を除外します。必要な場合だけ `includeMapObjects=true` を渡します。

```powershell
Invoke-RestMethod http://127.0.0.1:32123/state/facilities?limit=500
Invoke-RestMethod http://127.0.0.1:32123/state/facilities?service=HealthCare
Invoke-RestMethod http://127.0.0.1:32123/state/facilities?includeMapObjects=true
```

## GET /state/networks

道路、配管、暖房管、電線などのネットワークセグメントを返します。

```powershell
Invoke-RestMethod http://127.0.0.1:32123/state/networks?service=Road
Invoke-RestMethod http://127.0.0.1:32123/state/networks?limit=1000
```

## GET /state/road-anomalies

画面上は接続して見えても、CS1 の道路グラフ上は接続していない形状を検出します。
短い不要スタブ、近接した未接続端点、同じノード間に重複した道路に加えて、同じ高さでほぼ重なっている道路や、
同じ高さで交差しているのにノード共有していない道路も検出します。

```powershell
Invoke-RestMethod "http://127.0.0.1:32123/state/road-anomalies?nearMissDistance=18&shortSegmentLength=32&includeDeadEnds=true"
```

## GET /state/building-anomalies

サービス施設の footprint が道路セグメントと交差していないかを検出します。

```powershell
Invoke-RestMethod http://127.0.0.1:32123/state/building-anomalies?limit=200
```

## POST /commands/build-network

道路、配管、暖房管、電線を作成する汎用 network 作成 API です。初期互換のため、request field は `roadPrefab` のままです。

```json
{
  "dryRun": true,
  "roadPrefab": "Basic Road",
  "start": { "x": 0, "z": 0 },
  "end": { "x": 80, "z": 0 },
  "name": "Agent Test Road"
}
```

## POST /commands/build-road

`/commands/build-network` の互換エイリアスです。

## POST /commands/set-zone

既存の zoning block にゾーンを塗ります。

```json
{
  "dryRun": true,
  "zone": "ResidentialLow",
  "center": { "x": 40, "z": 0 },
  "radius": 32
}
```

## POST /commands/place-building

指定 prefab の建物を配置します。

```json
{
  "dryRun": true,
  "buildingPrefab": "Wind Turbine",
  "position": { "x": 300, "z": 200 },
  "angleDegrees": 0
}
```

## POST /commands/move-building

既存建物を同じ prefab で新しい位置へ作り直し、古い建物を削除します。

```json
{
  "dryRun": false,
  "id": 123,
  "position": { "x": 340, "z": 200 },
  "angleDegrees": 180
}
```

## POST /commands/bulldoze

問題のある building、netSegment、netNode を削除します。

```powershell
$body = @{
  entityType = "netSegment"
  id = 21778
  keepNodes = $false
} | ConvertTo-Json

Invoke-RestMethod -Method Post -Uri http://127.0.0.1:32123/commands/bulldoze -Body $body -ContentType "application/json"
```

## POST /commands/save

CS1 の通常 UI と同じ `SavePanel.SaveGame` 経由で保存をリクエストします。`.crp` 書き込みは非同期なので、`/state/saves` で確認します。

## GET /state/saves

ローカル `.crp` セーブを path、timestamp、file size 付きで返します。

## POST /commands/set-simulation-speed

```json
{
  "paused": false,
  "speed": 3
}
```

`speed` は `0..3` に clamp されます。

## POST /commands/set-tax-rate

ゾーン系サービスの税率を設定します。`service`、`subService`、`level` を省略すると
広い範囲へ適用します。`dryRun: true` で対象行だけ確認できます。

```json
{
  "dryRun": false,
  "service": "Commercial",
  "rate": 9
}
```

主な `service` は `Residential`、`Commercial`、`Industrial`、`Office` です。
`rate` は `0..29` です。

## POST /commands/batch

複数コマンドをまとめます。現在の supported command type は `build-road` と `set-zone` です。
