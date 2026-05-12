# トラブルシュート

## ビルドスクリプトが CS1 を見つけられない

`scripts/build.ps1` の `$game` パスを、自分の Cities: Skylines インストール先に合わせてください。スクリプトはコンパイル前に `Cities_Data\Managed\ICities.dll` を確認します。

## API が応答しない

次の順に確認します。

1. CS1 のコンテンツマネージャーで MOD が有効になっている。
2. 都市がロードされている。
3. port `32123` が他のプロセスに使われていない。
4. `scripts/start-resume.ps1` または `scripts/start-new-map.ps1` が `/health` レスポンスまで到達している。

## 道路がつながって見えるのに交通が流れない

CS1 のネットワーク交差は、実ノードが作られていない限り交差点ではありません。まず確認します。

```powershell
Invoke-RestMethod "http://127.0.0.1:32123/state/road-anomalies?nearMissDistance=18&shortSegmentLength=32&includeDeadEnds=false"
```

その後、悪いセグメントを `/commands/bulldoze` で消し、意図した道路ノードを再利用できる近い端点で作り直します。

## サービス施設が道路と重なっている

次の API を使います。

```powershell
Invoke-RestMethod http://127.0.0.1:32123/state/building-anomalies?limit=200
```

`/commands/move-building` または `/commands/place-building` で移動・再配置し、もう一度 anomaly endpoint を確認します。

## 保存がまだ見えない

`/commands/save` は CS1 の通常保存パネル経由で保存するため、ファイル書き込みは非同期です。保存リクエスト後に `/state/saves` を poll するか、次のスクリプトを使います。

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\save-city.ps1 -Name AgentAutoSave-clean
```
