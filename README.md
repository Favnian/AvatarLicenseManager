# Avatar License Manager

Avatar License Manager は VRChat アバタープロジェクト向けの Unity Editor 拡張ツールです。

アバターの Prefab をツールウィンドウにドロップするだけで、そのアバターが参照している
アセット（衣装・ヘア・アクセサリーなど）を自動解析し、製品ごとのライセンスファイルの
所在を一覧表示します。

## 現在できること（v0.1.0 実装済み）

- **Prefab ドロップで依存解析** — `AssetDatabase.GetDependencies` を使い、アバターが
  参照するすべてのアセットを収集し `Assets/<Vendor>/<Product>` 単位で製品グループ化します。
- **ライセンスファイル自動検出** — 各製品フォルダ内の `avatar-license.json` や
  `利用規約.txt` などを自動検出してパスを表示します。ベンダールートへのフォールバックも行います。
- **Ping / 開く** — Project View へのハイライトと OS 既定アプリでの開封に対応します。
- **パーミッション表示** — `avatar-license.json` スキーマを読み込み、16項目のパーミッションと3項目の要件を色付き一覧で表示します。
- **アバター総合評価** — 全製品のライセンスから最も厳しい判定を集計して表示します。
- **メモ** — 製品ごとに利用条件の要約などを記入・保存できます。
- **セッション間で保持** — 分析結果は ScriptableObject として保存されます。

## まだできないこと（今後の実装予定）

- UI 上でのライセンス値手動入力・`avatar-license.json` の書き出し
- Simple / Detailed 表示モードの切り替え
- 分析レポートの Markdown / JSON エクスポート
- PDF テキスト抽出による自動マッピング
- VCC / VPM パッケージとしての配布

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
