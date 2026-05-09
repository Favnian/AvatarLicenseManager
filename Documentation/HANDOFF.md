# Avatar License Manager — 開発引継ぎ資料

最終更新: 2026-05-09

---

## 実装済み機能

### ターゲット選択
- アバター Prefab のドロップゾーン（DragAndDrop API、ハイライト付き）
- ObjectField による Prefab / Scene オブジェクト選択
- 「選択中を使う」ボタン（Project View / Hierarchy どちらも対応）
- HierarchyオブジェクトはPrefabUtility経由でアセットパスを取得

### 依存解析・製品グループ化（`AvatarDependencyScanner.cs`）
- `AssetDatabase.GetDependencies(recursive: true)` で Prefab の推移的依存を全収集
- `Assets/<Vendor>/<Product>/<...>` パターンの製品ルートを自動検出
- フォルダが実在するもののみ対象（`IsValidFolder` で確認）
- 販売者名・製品名をパスから自動抽出（手動修正可能）

### ライセンスファイル検索（`LicenseFileLocator.cs`）
3段階の優先度でスキャン:

1. **機械可読 JSON**（製品ルート直下の完全一致）: `avatar-license.json` / `license.json` / `vn3-license.json`
2. **ライセンスフォルダ内の全文書ファイル**: `License`, `Licenses`, `VN3`, `Terms`, `規約`, `利用規約`, `ライセンス` という名前のサブフォルダ内
3. **ファイル名キーワードスキャン**（再帰）: `license`, `licence`, `ライセンス`, `利用規約`, `terms`, `eula`, `vn3` を含むファイル

対象拡張子: `.txt` `.pdf` `.md` `.html` `.htm` `.json`

### EditorWindow UI（`AvatarLicenseManagerWindow.cs`）
- **製品カード一覧**: 折りたたみ可能、ライセンスファイルパス表示、Ping / 開くボタン
- **製品名・販売者名**: 自動抽出 + 手動編集可
- **パーミッション表示**: JSON が検出できた製品は 16 パーミッション + 3 要件を 2 列グリッドで色付き表示
- **アバター総合評価**: 全製品の JSON を集計し、各フィールドで最も厳しい判定を表示（`deny > ask > conditional > unknown > notMentioned > allow`）
- **メモ欄**: 製品ごとに自由記入
- **セッション間保持**: 最後の分析結果を次回起動時に復元

### データモデル（`AvatarLicenseDatabase.cs` / `AvatarLicenseJson.cs`）
- `AvatarLicenseDatabase`（ScriptableObject）: 複数アバターの分析結果を保持
- `AvatarAnalysisEntry`: アバターパス・最終分析日時・製品リスト
- `ProductLicenseEntry`: 製品ルート・名前・ライセンスファイルパス・メモ
- `AvatarLicenseJson`: `avatar-license.json` スキーマのデシリアライズモデル（`JsonUtility` 使用）

### ドキュメント
- `Documentation/SPEC.md` — 機能要件・設計仕様・将来スコープ
- `Documentation/README.html` — ユーザー向け HTML ガイド
- `Documentation/TransformLicensePrompt.md` — PDF→JSON 変換用 Claude プロンプト
- `README.md` — パッケージルート README
- `package.json` — VCC/VPM 配布用メタデータ

---

## 検証済み

| 項目 | 状態 |
|---|---|
| コンパイルエラーなし | ✅ Unity MCP で確認済み |
| `Tools > StarSideUp > Avatar License Manager` メニュー表示 | ✅ |
| Prefab ドロップ → 依存解析 → 製品検出 | ✅（10製品検出を確認） |
| `StarSideUp-License.json` の検出（`.json` 拡張子追加後） | ✅ |
| Ping / 開くボタン | ✅ |
| パーミッション表示（色付きグリッド） | ✅ |
| アバター総合評価（集計） | 実装済み・UI検証待ち |
| セッション間の永続化 | ✅ |

---

## 未実装（今後の課題）

### 優先度：高
- [ ] **ライセンス手動入力 UI** — JSON ファイルなしで UI 上から各パーミッション値を入力・保存する機能
- [ ] **`avatar-license.json` 書き出し** — UI で編集した内容を製品フォルダへ保存
- [ ] **製品ルートの手動修正** — 自動検出が不正確な場合にフォルダを変更できる UI

### 優先度：中
- [ ] **Simple / Detailed 表示モード切り替え** — 一般ユーザー向けに主要項目だけを表示するモード（SPEC §Display Modes）
- [ ] **Markdown / JSON レポートエクスポート** — 分析結果の書き出し
- [ ] **ベンダールートのフォールバック検索** — `Assets/<Vendor>/` レベルのライセンスを製品が未検出の場合の代替として利用（SPEC §License File Search Priority 5〜8）
- [ ] **総合評価のカテゴリ別まとめ** — 許可 / 禁止 / 条件付き / 要確認 / 不明 / 記載なし でグループ分けした一覧表示

### 優先度：低・将来スコープ
- [ ] **PDF テキスト抽出** — 選択可能テキストの PDF から文字列を読み取り、VN3 ラベルを自動マッピング
- [ ] **ライセンス鮮度チェック** — `lastReviewedAt` に基づいた再確認促進
- [ ] **VCC / VPM パッケージ整備** — `package.json` の完成・VPM リポジトリへの公開

### 設計上の未決定事項
- [ ] `avatar-license.json` をサードパーティフォルダ内に書くか、中央 DB として管理するかの決定（SPEC §Open Questions）
- [ ] VPM パッケージレイアウト: `Assets/StarSideUp/AvatarLicenseManager` 配下のまま vs UPM スタイルに変換

---

## ファイル構成（最終）

```
Assets/StarSideUp/AvatarLicenseManager/
├── Editor/
│   ├── AvatarLicenseManager.Editor.asmdef
│   ├── AvatarLicenseDatabase.cs        ← ScriptableObject + データモデル
│   ├── AvatarDependencyScanner.cs      ← Prefab依存解析・製品ルート抽出
│   ├── LicenseFileLocator.cs           ← ライセンスファイル検索
│   ├── AvatarLicenseJson.cs            ← avatar-license.json デシリアライズ
│   └── AvatarLicenseManagerWindow.cs  ← EditorWindow UI 全体
├── Documentation/
│   ├── SPEC.md                         ← 機能要件・設計仕様
│   ├── HANDOFF.md                      ← このファイル
│   ├── README.html                     ← ユーザー向けHTMLガイド
│   ├── TransformLicensePrompt.md       ← PDF→JSON Claude プロンプト
│   └── StarSideUp-License.json        ← サンプルライセンスJSON
├── Generated/                          ← .gitignore 対象（ユーザー固有データ）
│   └── AvatarLicenseDatabase.asset
├── 20250128105755vn3license_ja.pdf     ← VN3ライセンスPDFサンプル
├── package.json
└── README.md
```

---

## 次に触るべきファイル

1. `Editor/AvatarLicenseManagerWindow.cs` — 手動入力UI・`avatar-license.json`書き出し
2. `Editor/LicenseFileLocator.cs` — ベンダールートフォールバック追加
3. `Documentation/SPEC.md §Open Questions` — 設計決定を記録してから実装へ
