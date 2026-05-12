---
layout: home

hero:
  name: "Cities: Skylines 1 Agent Skill"
  text: "Codex エージェント向けの API 都市操作"
  tagline: "スクリーンショット認識ではなく、ローカルブリッジ経由で CS1 の都市を調査・修復・建設・ゾーン設定・保存します。"
  image:
    src: /agent-bridge-icon.svg
    alt: Cities: Skylines Agent Bridge icon
  actions:
    - theme: brand
      text: ガイドを読む
      link: /ja/guide/getting-started
    - theme: alt
      text: APIリファレンス
      link: /ja/api

features:
  - title: 状態取得を優先
    details: 問題、施設、道路異常、ネットワーク、セーブ、Prefab を CS1 のデータから直接取得します。
  - title: 小さな操作
    details: 作成、削除、配置、移動、ゾーン、速度、バッチ、保存を分けて実行でき、作業内容を追跡できます。
  - title: Codex Skill 対応
    details: ルートの SKILL.md が、都市の Resume、検査、限定的な修復、保存確認の手順をエージェントに教えます。
---

## このリポジトリの内容

- `src/` 配下の Cities: Skylines 1 MODソース。
- `http://127.0.0.1:32123` で動くローカル HTTP API ブリッジ。
- ビルド、起動、スモークテスト、道路検査、修復ループ、保存用の PowerShell スクリプト。
- `SKILL.md` の Codex Skill 定義と `agents/openai.yaml` の Skill UI メタデータ。

## 基本ワークフロー

1. MODをビルドしてインストールする。
2. Steam と Paradox Launcher 経由で都市を Resume する。
3. 変更前に API で状態を調べる。
4. 小さな操作を1つずつ実行する。
5. シミュレーションを少し進めて再確認し、保存する。

セットアップは [はじめに](/ja/guide/getting-started)、修復ループは [エージェント運用](/ja/guide/usage) を参照してください。
