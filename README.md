# Avatar License Manager

Avatar License Manager は VRChat アバタープロジェクト向けの Unity Editor 拡張ツールです。

アバターの Prefab をツールウィンドウにドロップするだけで、そのアバターが参照している
アセット（衣装・ヘア・アクセサリーなど）を自動解析し、製品ごとのライセンスファイルの
所在を一覧表示します。

## 主な機能

- **Prefab ドロップで依存解析** — `AssetDatabase.GetDependencies` を使い、アバターが
  参照するすべてのアセットを収集し `Assets/<Vendor>/<Product>` 単位で製品グループ化します。
- **ライセンスファイル自動検出** — 各製品フォルダ内の `avatar-license.json` や
  `利用規約.txt` などを自動検出してパスを表示します。
- **Ping / 開く** — Project View へのハイライトと OS 既定アプリでの開封に対応します。
- **メモ** — 製品ごとに利用条件の要約などを記入・保存できます。
- **セッション間で保持** — 分析結果は ScriptableObject として保存されます。

## 使い方

1. `Tools > StarSideUp > Avatar License Manager` を開く
2. アバター Prefab をドロップゾーンにドラッグ＆ドロップ（または ObjectField で選択）
3. 「分析する」を押す
4. 検出された製品カードでライセンスファイルのパスを確認する

## ドキュメント

- `Documentation/SPEC.md` — 機能要件・設計仕様
- `Documentation/README.html` — ユーザー向け詳細ガイド（ブラウザで開いてください）
- `Documentation/HANDOFF.md` — 開発引継ぎ資料

## 配布予定

このフォルダは単体の GitHub リポジトリとして管理し、将来的に VCC / VPM パッケージとして
配布することを想定しています。パッケージ名: `com.starsideup.avatar-license-manager`
