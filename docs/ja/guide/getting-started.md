# はじめに

このガイドは、Steam版 Cities: Skylines 1 が入っている Windows 環境を前提にしています。

## 必要なもの

- Cities: Skylines 1。
- PowerShell。
- Windows の .NET Framework コンパイラ。
- CS1 の managed assemblies。

デフォルトのビルドスクリプトは、ゲームが次の場所にある前提です。

```text
D:\SteamLibrary\steamapps\common\Cities_Skylines
```

インストール先が違う場合は、ビルド前に `scripts/build.ps1` を編集してください。

## ビルドとインストール

リポジトリルートで実行します。

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\build.ps1
```

スクリプトは `SkylinesAgentBridge.dll` をコンパイルし、次の場所にコピーします。

```text
%LOCALAPPDATA%\Colossal Order\Cities_Skylines\Addons\Mods\SkylinesAgentBridge
```

都市をロードする前に、CS1 のコンテンツマネージャーで MOD を有効化してください。

## 都市を Resume する

通常のループは最新ローカルセーブから開始します。

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\start-resume.ps1
```

このスクリプトは `-SkipBuild` を渡さない限り MOD をビルドし、Steam 経由で CS1 を起動し、ランチャーの Resume をクリックして、ローカル API を待ちます。

## 新規マップから始める

検証用のクリーンな都市にはこちらを使います。

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\start-new-map.ps1
```

便利なフラグ:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\start-new-map.ps1 -SkipBuild
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\start-new-map.ps1 -SkipNewMap
```

## ブリッジ確認

都市がロードされたら確認します。

```powershell
Invoke-RestMethod http://127.0.0.1:32123/health
Invoke-RestMethod http://127.0.0.1:32123/state/summary
```

より広い読み取りと dry-run コマンド確認には `scripts/smoke-test.ps1` を使います。
