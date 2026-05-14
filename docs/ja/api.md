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

## GET /state/demand

CS1 UI に表示される3本の需要バー、住宅・商業・職場需要を返します。値は `0..100` です。

```powershell
Invoke-RestMethod http://127.0.0.1:32123/state/demand
```

## GET /state/chirps

CS1 の message manager から、最近の Chirper/市民メッセージを返します。
送信者名、sender id、本文、メッセージ種別、取得できる場合は message id やタグも含みます。
住宅需要、税金、交通、サービス、市民満足度などの声を OCR なしで読むための API です。

```powershell
Invoke-RestMethod "http://127.0.0.1:32123/state/chirps?limit=50"
```

## GET /state/zones

ゾーン種別ごとのセル数と概算面積を返します。CS1 の zoning cell は 8m x 8m として扱い、`areaSquareMeters` は住宅・商業・産業・オフィス・未指定の面積比較に使うための概算値です。

```powershell
Invoke-RestMethod http://127.0.0.1:32123/state/zones
```

## GET /state/growables

既に建っている住宅・商業・産業・オフィスの growable 建物を返します。
service、subService、建物サイズ、位置、有効/廃墟状態、問題フラグを含むため、ゾーンを塗る前に既存の街区を避けられます。

```powershell
Invoke-RestMethod "http://127.0.0.1:32123/state/growables?limit=500"
```

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

既知の壊れた/使用禁止 building asset はこの一覧から除外します。現在は `Block Services - ...` 系を除外します。

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

住宅・商業・産業・オフィス系ゾーンの税率を返します。`aggregateTaxRates` は CS1 の予算 UI に表示される6つの税率スライダーと同じ値です。

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
短い不要スタブ、近接した未接続端点、同じノード間に重複した道路に加えて、同じ高さでほぼ重なっている道路、同じ高さで交差しているのにノード共有していない道路、
周囲地形と大きく段差がある埋没・崖化した道路も検出します。
`roadBelowLocalGrade` は、Agent が作った地表道路だけが周囲の街路より大きく
沈んでいるケースを返します。

```powershell
Invoke-RestMethod "http://127.0.0.1:32123/state/road-anomalies?nearMissDistance=18&shortSegmentLength=32&includeDeadEnds=true"
```

## GET /state/external-connections

街のローカル道路コンポーネントが CS1 の外部道路ノードにつながっているかを
返します。高速道路が見えているのに外部から車が来ない場合の確認に使えます。

```powershell
Invoke-RestMethod "http://127.0.0.1:32123/state/external-connections?limit=50"
```

`cityConnectedToOutside`、`disconnectedLocalRoadComponents`、外部ノード数、
道路コンポーネント概要を返します。

## GET /state/building-anomalies

ユーザーが置いた建物や growable 建物の footprint が道路セグメントと
交差していないかを検出します。`roadClearance` を渡すと、道路中心線が
建物 footprint にかなり近いものも検出します。中心線そのものは貫通して
いないが、見た目では道路端が建物にかかっているような配置ミスを拾うための
確認です。岩や橋脚などマップ元来の object は標準では除外します。
それらも調べる場合は `includeOriginal=true` を渡します。

```powershell
Invoke-RestMethod http://127.0.0.1:32123/state/building-anomalies?limit=200
Invoke-RestMethod "http://127.0.0.1:32123/state/building-anomalies?limit=200&roadClearance=0"
Invoke-RestMethod "http://127.0.0.1:32123/state/building-anomalies?limit=200&roadClearance=1&includeOriginal=true"
```

検出する anomaly type:

- `buildingRoadOverlap`: 道路中心線が建物 footprint を貫通している。
- `buildingRoadTooClose`: 道路中心線が建物 footprint から `roadClearance` メートル以内にある。

## GET /state/zone-anomalies

CS1 の zoning block を直接読み、斑になった区画をスクリーンショットなしで検出します。
円形塗りや重ね塗りのあとに、住宅・商業・産業・オフィス・未指定が同じブロック内で混ざった状態を拾うための API です。

```powershell
Invoke-RestMethod "http://127.0.0.1:32123/state/zone-anomalies?limit=200&includeUnzonedHoles=true"
```

検出する anomaly type:

- `mixedZoneBlock`: 1つの zoning block 内に、住宅と商業など複数のゾーン種別が混ざっている。
- `patchyUnzonedHoles`: ほぼ1種類のゾーンだが未指定セルが多く、塗り残しの穴に見えやすい。

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
  "preserveOccupied": true,
  "zone": "ResidentialLow",
  "center": { "x": 40, "z": 0 },
  "radius": 32
}
```

`preserveOccupied` は既定で `true` です。既存の住宅・商業・産業・オフィス・公共サービス・公園・モニュメント建物がある zoning block はスキップし、広めのゾーン操作で発展済み区画を上書きしないようにします。

## POST /commands/repair-zones-to-growables

既存の growable 建物がある zoning block を、最寄りの住宅・商業・産業・オフィス建物に合わせて修復します。
住宅と商業が同じ距離にあるような曖昧な混在ブロックはスキップします。

```json
{
  "dryRun": true
}
```

## POST /commands/repair-zone-clusters

街区全体がまだらに見える 80m 単位の zoning cluster を修復します。
未ゾーン穴を埋められます。既定では既存 growable 建物がある block では最寄り建物のゾーンを優先し、街区の支配色だけで発展済み建物を別用途に変えないようにします。

```json
{
  "dryRun": true,
  "includePatchy": true,
  "fillUnzoned": true,
  "preferGrowableZone": true,
  "gridSize": 80
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

## POST /commands/set-building-active

既存建物を id 指定でオン/オフします。

```json
{
  "id": 123,
  "active": false
}
```

## POST /commands/disable-blocked-assets

CS1 の package asset state 上で既知の壊れたアセットを無効化し、ゲームが使用しないようにします。現在は `Block Services - ...` 系を対象にします。

```powershell
Invoke-RestMethod -Method Post -Uri http://127.0.0.1:32123/commands/disable-blocked-assets
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

## POST /commands/capture-view

ロード中の都市を人間がざっと確認するための PNG 撮影をリクエストします。
必要に応じてカメラを俯瞰位置へ移動し、CS1 の info view を切り替えてから
Unity にスクリーンショット保存を依頼します。PNG 書き込みは次の描画フレーム後に
行われるため、レスポンスには `pending: true` と確認用の `path` が入ります。

対応する `preset`:

- `overview`: 通常表示の都市俯瞰。
- `transport`, `route-map`: 公共交通の路線図ビュー。
- `underground`, `metro`, `subway`: 地下トンネルビュー。地下鉄や地下ネットワーク確認向け。

```powershell
$body = @{ preset = "overview"; superSize = 1 } | ConvertTo-Json
Invoke-RestMethod -Method Post -Uri http://127.0.0.1:32123/commands/capture-view -Body $body -ContentType "application/json"

$body = @{ preset = "transport"; name = "routes.png"; superSize = 2 } | ConvertTo-Json
Invoke-RestMethod -Method Post -Uri http://127.0.0.1:32123/commands/capture-view -Body $body -ContentType "application/json"

$body = @{ preset = "underground"; name = "metro-underground.png" } | ConvertTo-Json
Invoke-RestMethod -Method Post -Uri http://127.0.0.1:32123/commands/capture-view -Body $body -ContentType "application/json"
```

`center`、`zoom`、`height`、`angleX`、`angleY` でカメラを上書きできます。
現在のカメラを保ったまま info view だけ切り替えて撮る場合は
`setCamera: false` を渡します。

補助スクリプト:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\capture-city-view.ps1 -Preset overview
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\capture-city-view.ps1 -Preset transport -SuperSize 2
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\capture-city-view.ps1 -Preset underground -Name metro-underground.png
```

## GET /state/captures

`/commands/capture-view` が作成した PNG ファイルを返します。

```powershell
Invoke-RestMethod http://127.0.0.1:32123/state/captures
```

## POST /commands/restore-ui

撮影、info view、カメラ検証のあとに通常HUDが非表示のまま残った場合、
CS1 UI を通常表示へ戻します。info view を解除し、default tool へ戻し、
main UI view と toolbar を再有効化して UI を更新します。

```powershell
Invoke-RestMethod -Method Post -Uri http://127.0.0.1:32123/commands/restore-ui
```

## GET /state/transport-line-anomalies

公共交通路線の切断や運行不能を路線単位で検知します。`lineNotConnected` の停留所ノード、未完成路線、停留所不足、車両が必要なのに出ていない路線を返します。車両数だけでは運行確認にならないため、路線を運行中と判断する前に使います。

```powershell
Invoke-RestMethod "http://127.0.0.1:32123/state/transport-line-anomalies?limit=200"
```

## GET /state/transport-station-anomalies

旅客駅の建物が存在するのに、同じ交通種別の路線停留所が近くにない状態を検知します。駅を建てたが実際には路線が通っていないケースを拾うために使います。`maxStopDistance` の既定値は `96` メートルです。

```powershell
Invoke-RestMethod "http://127.0.0.1:32123/state/transport-station-anomalies?limit=200&maxStopDistance=96"
```

## GET /state/transport-vehicles

全路線または `lineId` 指定の交通車両を返します。各車両には路線 id、path id、座標、速度、source/target building、wait/block counter が含まれます。

```powershell
Invoke-RestMethod "http://127.0.0.1:32123/state/transport-vehicles?lineId=29&limit=50"
```

## POST /commands/set-simulation-speed

```json
{
  "paused": false,
  "speed": 3
}
```

`speed` は `1..3` に clamp されます。停止したい場合は `paused: true`
を使います。これにより、ゲームUIの速度ボタンは有効な状態のまま保たれます。

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
