# エージェント運用

このブリッジは、調査してから小さく操作する都市運用を前提にしています。エージェントは大きな隠れた修復処理を避け、あとで説明できる小さな API 呼び出しを積み上げます。

## 推奨ループ

1. ブリッジの health と都市サマリーを確認する。
2. 問題、道路異常、建物異常、施設、ネットワークを読む。
3. 1つの限定的な変更を選ぶ。
4. 対応 API が dry run を持つ場合は、まず dry run で確認する。
5. シミュレーションを少し進める。
6. 状態を再取得して差分を見る。
7. 都市を保存し、`/state/saves` で保存を確認する。

## 調査コマンド

```powershell
Invoke-RestMethod http://127.0.0.1:32123/health
Invoke-RestMethod http://127.0.0.1:32123/state/summary
Invoke-RestMethod "http://127.0.0.1:32123/state/problems?limit=200"
Invoke-RestMethod "http://127.0.0.1:32123/state/road-anomalies?limit=500&nearMissDistance=18&shortSegmentLength=32&includeDeadEnds=false"
Invoke-RestMethod "http://127.0.0.1:32123/state/building-anomalies?limit=200"
Invoke-RestMethod "http://127.0.0.1:32123/state/facilities?limit=500"
Invoke-RestMethod "http://127.0.0.1:32123/state/networks?limit=1000&service=Road"
```

`/state/facilities` の `includeMapObjects=true` は、配管や暖房の内部ヘルパー建物が必要な場合だけ使います。

## 修復パターン

- 既知の悪い建物、ノード、セグメントは `/commands/bulldoze` で削除します。
- 道路、配管、暖房管、電線は `/commands/build-network` で作ります。
- サービス施設は `/commands/place-building` または `/commands/move-building` で配置します。
- 道路が zoning block を作ったあとに `/commands/set-zone` を使います。
- 結果を見る前に `/commands/set-simulation-speed` で都市を少し進めます。
- 意味のある変更後は `/commands/save` または `scripts/save-city.ps1` で保存します。

## プロンプト例

```text
Use $cities-skylines1-agent-skill to resume my CS1 city, inspect current problems, repair road/infrastructure issues with separate API calls, save the city, and report what changed.
```
