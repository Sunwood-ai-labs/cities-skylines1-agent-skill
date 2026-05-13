# v0.3.0 リリースノート

![v0.3.0 release header](/release-header-v0.3.0.svg)

`v0.3.0` は、基本的なコマンドAPIだったブリッジを、都市運用のための実用的なループへ広げるリリースです。`v0.1.0` と比べて、エージェントは都市のライブ状態をより細かく読み取り、道路・ゾーン・経済・アセットの問題をAPIデータで診断し、小さな修復コマンドを実行し、ゲーム内APIコンソールで操作履歴を確認できます。

## ハイライト

- 需要、Chirperメッセージ、ゾーン集計、growable建物、経済、外部道路接続、ゾーン異常を読む状態APIを追加しました。
- 税率設定、ブロック対象アセットの無効化、建物の有効化、ゾーン修復、クラスタ単位のゾーン修復、安全なブルドーズのためのコマンドAPIを追加しました。
- 道路異常検出を拡張し、重複、重なり、ノードなし交差、地形段差、沈んだ道路、短い行き止まり、外部接続を確認できるようにしました。
- 広範囲のゾーン塗りで既存建物を保護する既定動作を追加し、修復コマンドでは近くのgrowable建物に合わせてゾーンを整えられるようにしました。
- 一時的な通知オーバーレイを、最近のAPI呼び出しを保持するゲーム内APIコンソールへ更新しました。クリア、最小化、ドラッグ移動に対応しています。
- 都市パラメータ記録、インフラ付き都市開発、サービス建物の道路重なり修復に使う運用スクリプトを追加しました。
- Git Flowベースのレビュー手順を文書化し、CI用docsリンク検証を強化しました。

## 都市状態と経済

エージェントが都市を変更する前に読める情報が増えました。

- `/state/demand`: 住宅、商業、雇用需要バーを返します。
- `/state/chirps`: OCRなしで最近のCS1メッセージを読み取ります。
- `/state/zones`: ゾーンセル数とゾーン種別ごとの概算面積を返します。
- `/state/growables`: 既存の住宅、商業、産業、オフィスgrowable建物を位置や状態つきで返します。
- `/state/economy`: UI税率スライダーの集約値と詳細な税率行を返します。
- `/state/external-connections`: 都市側の道路コンポーネントが外部道路ノードにつながっているか確認します。
- `/state/zone-anomalies`: 混在ゾーンブロックや穴あきの未ゾーンセルを検出します。

`/commands/set-tax-rate` は住宅、商業、産業、オフィスの税率を設定できます。`dryRun` と、`service`、`subService`、`level` による絞り込みに対応しています。

## 修復と安全性

v0.3.0 は、隠れた一括修復ではなく「調べてから小さく直す」運用を強化しています。道路異常検出は、画面上は重なって見えるが実際には接続していない道路、重複セグメント、行き止まりの短い道路、地形段差、周辺の道路面より沈んだエージェント作成道路を扱えます。レビュー対応として、道路重なり検出は空間グリッドで候補を絞るようになり、大きな都市で `/state/road-anomalies` が不要な全ペア走査を避けます。

ゾーン操作も安全寄りになりました。`/commands/set-zone` は既定で `preserveOccupied: true` になり、`RepairZonesToGrowables` は既存growable建物の近くにある非空ゾーンセルを揃えます。`RepairZoneClusters` は、80m単位のまだらなゾーンクラスタを修復しつつ、既存growable建物のゾーン文脈を優先できます。

ブルドーズと建物移動では、同一ゲームスレッド上のrelease fallbackを `GameThreadHelpers` に共通化しました。セグメント削除のfallbackでは、CS1側の非公開メソッドが対応している場合に `keepNodes` を保持します。

## 運用スクリプト

ローカル運用を繰り返しやすくするため、次のスクリプトを追加・更新しました。

- `scripts/log-city-parameters.ps1`: summary、demand、economy、problemをJSONL/CSVに記録し、税率変更をイベントログに残します。
- `scripts/develop-city-with-infrastructure.ps1`: 道路、ユーティリティ、サービス、追加ゾーン、シミュレーション待機、保存まで含むスターター都市を構築します。
- `scripts/repair-service-overlap.ps1`: サービス建物と関連インフラを道路中心線から離して再配置します。
- `scripts/check-doc-links.ps1`: 角括弧つきMarkdownリンクを検証し、生成済み依存・buildフォルダを無視します。

## Docs とリリースプロセス

英日README/API docsを更新し、日本語CONTRIBUTING、開発フローガイド、再現可能なdocs buildのための `docs/package-lock.json` を追加しました。

このリリースで使ったブランチモデルも文書化されています。feature作業は `develop`、リリース安定化は `main`、リリース修正は `develop` へバックマージします。

## 検証

リリースブランチでは、マージ前に次を実行しました。

```powershell
.\scripts\build.ps1
.\scripts\check-doc-links.ps1
```

このリリースノート作成では、新しいSVGヘッダーの検証とdocs siteのbuildも実行してから公開します。

## リンク

- [解説記事](/ja/guide/articles/v0-3-0-agent-city-operations)
- [APIリファレンス](/ja/api)
- [エージェント運用](/ja/guide/usage)
- [開発フロー](/ja/guide/development-flow)
- [English release notes](/releases/v0.3.0/)
