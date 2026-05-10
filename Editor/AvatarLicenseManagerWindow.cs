using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace StarSideUp.AvatarLicenseManager.Editor
{
    public sealed class AvatarLicenseManagerWindow : EditorWindow
    {
        private AvatarLicenseDatabase _database;
        private GameObject _targetPrefab;
        private string _targetAssetPath = string.Empty;
        private AvatarAnalysisEntry _activeEntry;
        private Vector2 _scroll;
        private bool _isDragOverDropZone;

        // 製品カードの展開状態。キーは ProductRootPath
        private readonly Dictionary<string, bool> _expandedProducts = new Dictionary<string, bool>();

        // 読み込み済み avatar-license.json キャッシュ。キーは AssetPath
        private readonly Dictionary<string, AvatarLicenseJson> _licenseJsonCache =
            new Dictionary<string, AvatarLicenseJson>();

        // 総合評価キャッシュ（RunAnalysis でクリアされる）
        private Dictionary<string, string> _aggregation;
        private int _aggregationCovered;
        private int _aggregationUncovered;

        private const string DatabaseAssetPath =
            "Assets/StarSideUp/AvatarLicenseManager/Generated/AvatarLicenseDatabase.asset";

        [MenuItem("Tools/StarSideUp/Avatar License Manager")]
        public static void Open() => GetWindow<AvatarLicenseManagerWindow>("Avatar License Manager");

        private void OnEnable()
        {
            LoadOrCreateDatabase();
            RestoreLastEntry();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Avatar License Manager", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "アバター Prefab を下のエリアにドロップするか、選択してから「分析する」を押してください。\n" +
                "Prefab の依存アセットを解析し、製品ごとのライセンスファイル所在を一覧表示します。",
                MessageType.Info);

            DrawSectionHeader("対象アバター / Target Avatar");
            DrawTargetSection();

            if (_activeEntry == null) return;

            DrawSectionHeader("分析結果 / Analysis Results");
            DrawResultsSummary();

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            DrawAggregation();
            DrawProductList();
            EditorGUILayout.EndScrollView();
        }

        // ── Target section ───────────────────────────────────────────────────────

        private void DrawTargetSection()
        {
            Rect dropZoneRect = GUILayoutUtility.GetRect(0, 72, GUILayout.ExpandWidth(true));
            DrawDropZone(dropZoneRect);
            HandleDragAndDrop(dropZoneRect);

            EditorGUILayout.Space(4);

            // ProjectビューのPrefabまたはHierarchyのGameObjectを受け付ける
            EditorGUI.BeginChangeCheck();
            _targetPrefab = (GameObject)EditorGUILayout.ObjectField(
                "Avatar Prefab", _targetPrefab, typeof(GameObject), allowSceneObjects: true);
            if (EditorGUI.EndChangeCheck())
                _targetAssetPath = AvatarDependencyScanner.GetPrefabAssetPath(_targetPrefab) ?? string.Empty;

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("選択中を使う / Use Selection", GUILayout.Width(200)))
                    ApplySelection();

                using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(_targetAssetPath)))
                {
                    if (GUILayout.Button("分析する / Analyze"))
                        RunAnalysis();
                }
            }

            if (!string.IsNullOrEmpty(_targetAssetPath))
                EditorGUILayout.LabelField("対象: " + _targetAssetPath, EditorStyles.miniLabel);
        }

        private void DrawDropZone(Rect rect)
        {
            if (_isDragOverDropZone)
                EditorGUI.DrawRect(rect, new Color(0.3f, 0.5f, 1f, 0.18f));

            GUI.Box(rect, GUIContent.none, EditorStyles.helpBox);

            var labelStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 12,
                wordWrap = true,
                normal =
                {
                    textColor = _isDragOverDropZone
                        ? new Color(0.5f, 0.8f, 1f)
                        : new Color(0.5f, 0.5f, 0.5f)
                }
            };
            GUI.Label(rect,
                "アバター Prefab をここにドロップ\nDrop avatar Prefab here  (Project view or Hierarchy)",
                labelStyle);
        }

        private void HandleDragAndDrop(Rect dropZoneRect)
        {
            Event evt = Event.current;
            switch (evt.type)
            {
                case EventType.DragUpdated:
                    if (dropZoneRect.Contains(evt.mousePosition))
                    {
                        _isDragOverDropZone = HasGameObjectInDrag();
                        DragAndDrop.visualMode = _isDragOverDropZone
                            ? DragAndDropVisualMode.Copy
                            : DragAndDropVisualMode.Rejected;
                        evt.Use();
                        Repaint();
                    }
                    else
                    {
                        _isDragOverDropZone = false;
                    }
                    break;

                case EventType.DragPerform:
                    if (dropZoneRect.Contains(evt.mousePosition) && HasGameObjectInDrag())
                    {
                        DragAndDrop.AcceptDrag();
                        ApplyDraggedObject();
                        evt.Use();
                    }
                    _isDragOverDropZone = false;
                    Repaint();
                    break;

                case EventType.DragExited:
                    _isDragOverDropZone = false;
                    Repaint();
                    break;
            }
        }

        private static bool HasGameObjectInDrag()
        {
            foreach (Object obj in DragAndDrop.objectReferences)
            {
                if (obj is GameObject) return true;
            }
            return false;
        }

        private void ApplyDraggedObject()
        {
            foreach (Object obj in DragAndDrop.objectReferences)
            {
                if (obj is GameObject go)
                {
                    string path = AvatarDependencyScanner.GetPrefabAssetPath(go);
                    if (!string.IsNullOrEmpty(path))
                    {
                        _targetPrefab = go;
                        _targetAssetPath = path;
                        return;
                    }
                }
            }
        }

        private void ApplySelection()
        {
            Object selected = Selection.activeObject;
            if (selected == null) return;
            string path = AvatarDependencyScanner.GetPrefabAssetPath(selected);
            if (string.IsNullOrEmpty(path)) return;
            _targetPrefab = selected as GameObject;
            _targetAssetPath = path;
        }

        // ── Analysis ─────────────────────────────────────────────────────────────

        private void RunAnalysis()
        {
            if (string.IsNullOrEmpty(_targetAssetPath)) return;
            if (_database == null) LoadOrCreateDatabase();

            List<string> productRoots;
            try
            {
                EditorUtility.DisplayProgressBar("Avatar License Manager", "依存関係を解析中...", 0f);
                productRoots = AvatarDependencyScanner.FindProductRoots(_targetAssetPath);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[AvatarLicenseManager] Analysis failed: {ex.Message}");
                EditorUtility.DisplayDialog("分析エラー", ex.Message, "閉じる");
                return;
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            _licenseJsonCache.Clear();
            _aggregation = null;
            Undo.RecordObject(_database, "Analyze Avatar Licenses");

            AvatarAnalysisEntry entry = FindOrCreateEntry(_targetAssetPath);
            entry.LastAnalyzedAt = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm");

            // 既存エントリーのメモを引き継ぎながら製品リストを更新する
            var updated = new List<ProductLicenseEntry>(productRoots.Count);
            foreach (string root in productRoots)
            {
                ProductLicenseEntry prev = FindProduct(entry, root);

                // 製品ルートでライセンスが見つからない場合、ベンダールートをフォールバック探索する (#2)
                var licFiles = LicenseFileLocator.FindInFolder(root);
                if (licFiles.Count == 0)
                {
                    string vendorRoot = AvatarDependencyScanner.ExtractVendorRootPath(root);
                    if (!string.Equals(vendorRoot, root, System.StringComparison.OrdinalIgnoreCase)
                        && AssetDatabase.IsValidFolder(vendorRoot))
                    {
                        licFiles = LicenseFileLocator.FindInFolder(vendorRoot);
                    }
                }

                updated.Add(new ProductLicenseEntry
                {
                    ProductRootPath  = root,
                    ProductName      = prev?.ProductName  ?? AvatarDependencyScanner.ExtractProductName(root),
                    VendorName       = prev?.VendorName   ?? AvatarDependencyScanner.ExtractVendorName(root),
                    LicenseFilePaths = licFiles,
                    Note             = prev?.Note         ?? string.Empty
                });
            }
            entry.Products = updated;

            EditorUtility.SetDirty(_database);
            AssetDatabase.SaveAssets();

            _activeEntry = entry;
            Debug.Log(
                $"[AvatarLicenseManager] Analyzed '{_targetAssetPath}': {productRoots.Count} product(s) found.");
        }

        private AvatarAnalysisEntry FindOrCreateEntry(string avatarPath)
        {
            foreach (AvatarAnalysisEntry e in _database.Entries)
            {
                if (string.Equals(e.AvatarPrefabPath, avatarPath, System.StringComparison.OrdinalIgnoreCase))
                    return e;
            }
            var entry = new AvatarAnalysisEntry
            {
                AvatarPrefabPath = avatarPath,
                DisplayName      = Path.GetFileNameWithoutExtension(avatarPath)
            };
            _database.Entries.Add(entry);
            return entry;
        }

        private static ProductLicenseEntry FindProduct(AvatarAnalysisEntry entry, string root)
        {
            if (entry.Products == null) return null;
            foreach (ProductLicenseEntry p in entry.Products)
            {
                if (string.Equals(p.ProductRootPath, root, System.StringComparison.OrdinalIgnoreCase))
                    return p;
            }
            return null;
        }

        // ── Results ──────────────────────────────────────────────────────────────

        private void DrawResultsSummary()
        {
            if (_activeEntry == null) return;

            int withLicense = 0, withoutLicense = 0;
            foreach (ProductLicenseEntry p in _activeEntry.Products)
            {
                if (p.LicenseFilePaths != null && p.LicenseFilePaths.Count > 0) withLicense++;
                else withoutLicense++;
            }

            string avatarName = !string.IsNullOrEmpty(_activeEntry.DisplayName)
                ? _activeEntry.DisplayName
                : Path.GetFileNameWithoutExtension(_activeEntry.AvatarPrefabPath);

            EditorGUILayout.HelpBox(
                $"Avatar: {avatarName}　製品数: {_activeEntry.Products.Count}　" +
                $"ライセンス検出: {withLicense}　未検出: {withoutLicense}　" +
                $"分析日時: {_activeEntry.LastAnalyzedAt}",
                withoutLicense > 0 ? MessageType.Warning : MessageType.Info);
        }

        private void DrawProductList()
        {
            if (_activeEntry?.Products == null) return;
            foreach (ProductLicenseEntry product in _activeEntry.Products)
            {
                if (product != null) DrawProductCard(product);
                EditorGUILayout.Space(4);
            }
        }

        private void DrawProductCard(ProductLicenseEntry product)
        {
            string key = product.ProductRootPath ?? string.Empty;
            if (!_expandedProducts.ContainsKey(key))
                _expandedProducts[key] = true;

            bool hasLicense = product.LicenseFilePaths != null && product.LicenseFilePaths.Count > 0;

            using (new EditorGUILayout.VerticalScope(CardStyle()))
            {
                // ヘッダー行（折りたたみ可）
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUIContent icon = EditorGUIUtility.IconContent(
                        hasLicense ? "FilterSelectedOnly" : "console.warnicon.sml");
                    string header = $"{product.VendorName}  /  {product.ProductName}";
                    _expandedProducts[key] = EditorGUILayout.Foldout(
                        _expandedProducts[key],
                        new GUIContent("  " + header, icon.image),
                        true, EditorStyles.foldoutHeader);
                    GUILayout.FlexibleSpace();
                }

                // 折りたたみ時はスペースのみ
                if (!_expandedProducts[key])
                {
                    EditorGUILayout.Space(2);
                    return;
                }

                // ボディ（展開時のみ）
                EditorGUI.indentLevel++;

                // 販売者・製品名（編集可）
                EditorGUI.BeginChangeCheck();
                string newVendor  = EditorGUILayout.TextField("販売者 / Vendor",  product.VendorName);
                string newProduct = EditorGUILayout.TextField("製品名 / Product", product.ProductName);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(_database, "Edit Product Names");
                    product.VendorName  = newVendor;
                    product.ProductName = newProduct;
                    EditorUtility.SetDirty(_database);
                }

                // フォルダパス + Ping
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("フォルダ", product.ProductRootPath, EditorStyles.miniLabel);
                    if (GUILayout.Button("Ping", GUILayout.Width(48)))
                        PingAsset(product.ProductRootPath);
                }

                EditorGUILayout.Space(4);

                // ライセンスファイル一覧
                EditorGUILayout.LabelField("ライセンスファイル / License Files", EditorStyles.boldLabel);
                if (!hasLicense)
                {
                    EditorGUILayout.HelpBox(
                        "ライセンスファイルが見つかりませんでした。フォルダを Ping して手動で確認してください。",
                        MessageType.Warning);
                }
                else
                {
                    foreach (string path in product.LicenseFilePaths)
                        DrawLicenseFileRow(path);
                }

                // ライセンス内容（JSON があれば表示。ある .json が読めない場合は警告）(#6)
                bool hasJsonCandidate = product.LicenseFilePaths != null &&
                    product.LicenseFilePaths.Exists(p =>
                        p.EndsWith(".json", System.StringComparison.OrdinalIgnoreCase));
                AvatarLicenseJson licenseJson = TryGetLicenseJson(product);
                if (licenseJson != null)
                    DrawPermissionsTable(licenseJson);
                else if (hasJsonCandidate)
                    EditorGUILayout.HelpBox(
                        "JSON ファイルが見つかりましたが読み込めませんでした。" +
                        " schemaVersion フィールドが含まれているか確認してください。",
                        MessageType.Warning);

                EditorGUILayout.Space(4);

                // メモ欄
                EditorGUILayout.LabelField("メモ / Note（利用条件の要約など）");
                EditorGUI.BeginChangeCheck();
                string newNote = EditorGUILayout.TextArea(product.Note ?? string.Empty, GUILayout.MinHeight(40));
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(_database, "Edit Product Note");
                    product.Note = newNote;
                    EditorUtility.SetDirty(_database);
                }

                EditorGUI.indentLevel--;
            }
        }

        // ── Utilities ────────────────────────────────────────────────────────────

        private static void DrawLicenseFileRow(string assetPath)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(assetPath, EditorStyles.miniLabel);
                if (GUILayout.Button("Ping", GUILayout.Width(48)))
                    PingAsset(assetPath);
                if (GUILayout.Button("開く", GUILayout.Width(40)))
                    OpenFile(assetPath);
            }
        }

        private static void PingAsset(string assetPath)
        {
            Object obj = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
            if (obj != null)
                EditorGUIUtility.PingObject(obj);
            else
                Debug.LogWarning($"[AvatarLicenseManager] Not found in AssetDatabase: {assetPath}");
        }

        private static void OpenFile(string assetPath)
        {
            // Assets/ 以外のパスは変換できない (#10)
            if (string.IsNullOrEmpty(assetPath) ||
                !assetPath.StartsWith("Assets", System.StringComparison.OrdinalIgnoreCase))
            {
                Debug.LogWarning($"[AvatarLicenseManager] Cannot open non-Asset path: {assetPath}");
                return;
            }
            string absolute = Application.dataPath + assetPath.Substring("Assets".Length);
            if (File.Exists(absolute))
                Application.OpenURL("file:///" + absolute.Replace('\\', '/'));
            else
                Debug.LogWarning($"[AvatarLicenseManager] File not found on disk: {absolute}");
        }

        private void LoadOrCreateDatabase()
        {
            _database = AssetDatabase.LoadAssetAtPath<AvatarLicenseDatabase>(DatabaseAssetPath);
            if (_database != null) return;

            EnsureFolder("Assets/StarSideUp/AvatarLicenseManager", "Generated");
            _database = CreateInstance<AvatarLicenseDatabase>();
            AssetDatabase.CreateAsset(_database, DatabaseAssetPath);
            AssetDatabase.SaveAssets();
        }

        private void RestoreLastEntry()
        {
            if (_database == null || _database.Entries.Count == 0) return;

            // 最後に分析したエントリーを復元する
            AvatarAnalysisEntry last = _database.Entries[_database.Entries.Count - 1];
            _activeEntry     = last;
            _targetAssetPath = last.AvatarPrefabPath ?? string.Empty;
            if (!string.IsNullOrEmpty(_targetAssetPath))
                _targetPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(_targetAssetPath);
        }

        private static void EnsureFolder(string parent, string name)
        {
            if (!AssetDatabase.IsValidFolder(parent + "/" + name))
                AssetDatabase.CreateFolder(parent, name);
        }

        private static void DrawSectionHeader(string label)
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
        }

        private static GUIStyle CardStyle()
        {
            return new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(10, 10, 8, 8),
                margin  = new RectOffset(0, 0, 2, 4)
            };
        }

        // ── License JSON 読み込みとパーミッション表示 ──────────────────────────────

        private static readonly (string Field, string Label)[] PermissionRows =
        {
            ("personalUse",           "個人利用"),
            ("corporateUse",          "法人利用"),
            ("commercialUse",         "商業利用"),
            ("modification",          "改変"),
            ("redistribution",        "再配布"),
            ("modifiedRedistribution","改変後再配布"),
            ("vrchatUpload",          "VRChatアップロード"),
            ("publicAvatar",          "パブリックアバター"),
            ("pedestalUse",           "台座利用"),
            ("videoStreaming",         "動画・配信"),
            ("socialMediaPosting",    "SNS投稿"),
            ("adultExpression",       "アダルト表現"),
            ("violentExpression",     "暴力的表現"),
            ("politicalReligiousUse", "政治・宗教利用"),
            ("nftUse",                "NFT利用"),
            ("aiTraining",            "AI学習"),
        };

        private static readonly (string Field, string Label)[] RequirementRows =
        {
            ("credit",           "クレジット表記"),
            ("usageReport",      "利用報告"),
            ("contactBeforeUse", "事前連絡"),
        };

        private AvatarLicenseJson TryGetLicenseJson(ProductLicenseEntry product)
        {
            if (product.LicenseFilePaths == null) return null;
            foreach (string path in product.LicenseFilePaths)
            {
                if (!path.EndsWith(".json", System.StringComparison.OrdinalIgnoreCase)) continue;
                if (!_licenseJsonCache.TryGetValue(path, out AvatarLicenseJson cached))
                {
                    cached = AvatarLicenseJson.Load(path);
                    _licenseJsonCache[path] = cached;
                }
                if (cached != null) return cached;
            }
            return null;
        }

        private static void DrawPermissionsTable(AvatarLicenseJson json)
        {
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("ライセンス内容 / Permissions", EditorStyles.boldLabel);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                // permissions を2列グリッドで表示
                for (int i = 0; i < PermissionRows.Length; i += 2)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        DrawPermCell(
                            PermissionRows[i].Label,
                            GetPermValue(json.permissions, PermissionRows[i].Field));

                        if (i + 1 < PermissionRows.Length)
                        {
                            DrawPermCell(
                                PermissionRows[i + 1].Label,
                                GetPermValue(json.permissions, PermissionRows[i + 1].Field));
                        }
                        GUILayout.FlexibleSpace();
                    }
                }

                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField("義務・要件 / Requirements", EditorStyles.boldLabel);
                using (new EditorGUILayout.HorizontalScope())
                {
                    foreach (var (field, label) in RequirementRows)
                        DrawPermCell(label, GetReqValue(json.requirements, field));
                    GUILayout.FlexibleSpace();
                }

                if (!string.IsNullOrEmpty(json.lastReviewedAt))
                {
                    EditorGUILayout.Space(2);
                    EditorGUILayout.LabelField("最終確認: " + json.lastReviewedAt, EditorStyles.miniLabel);
                }

                if (!string.IsNullOrEmpty(json.notes))
                {
                    EditorGUILayout.Space(4);
                    EditorGUILayout.LabelField("JSONメモ / Source Notes");
                    EditorGUILayout.LabelField(json.notes, EditorStyles.wordWrappedMiniLabel);
                }
            }
        }

        private static void DrawPermCell(string label, string value)
        {
            using (new EditorGUILayout.HorizontalScope(GUILayout.Width(200)))
            {
                GUILayout.Label(label, GUILayout.Width(100));
                var style = new GUIStyle(EditorStyles.miniLabel)
                {
                    fontStyle = FontStyle.Bold,
                    normal    = { textColor = ValueColor(value) }
                };
                GUILayout.Label(ValueBadge(value), style, GUILayout.Width(90));
            }
        }

        private static string ValueBadge(string value)
        {
            switch (value?.ToLowerInvariant())
            {
                case "allow":        return "● 許可";
                case "deny":         return "✕ 禁止";
                case "conditional":  return "▲ 条件付き";
                case "ask":          return "? 要確認";
                case "required":     return "● 必要";
                case "notrequired":  return "– 不要";
                case "unknown":      return "? 不明";
                case "notmentioned": return "– 記載なし";
                // null/空 = JSONフィールド欠如または未取得。notMentionedと区別する (#19)
                case null:
                case "":             return "· 未取得";
                default:             return $"? {value}";
            }
        }

        private static Color ValueColor(string value)
        {
            bool dark = EditorGUIUtility.isProSkin;
            switch (value?.ToLowerInvariant())
            {
                case "allow":
                    return dark ? new Color(0.4f, 0.9f, 0.5f) : new Color(0.1f, 0.5f, 0.15f);
                case "deny":
                    return dark ? new Color(1f, 0.45f, 0.45f) : new Color(0.6f, 0.05f, 0.05f);
                case "conditional":
                    return dark ? new Color(1f, 0.78f, 0.3f) : new Color(0.5f, 0.3f, 0f);
                case "ask":
                    return dark ? new Color(0.55f, 0.80f, 1f) : new Color(0.1f, 0.3f, 0.7f);
                case "unknown":
                    return dark ? new Color(0.7f, 0.7f, 0.7f) : new Color(0.45f, 0.45f, 0.45f);
                case null:
                case "":
                    return new Color(0.38f, 0.38f, 0.38f); // 未取得 (null/empty) は薄いグレー (#19)
                default:
                    return new Color(0.5f, 0.5f, 0.5f); // notMentioned
            }
        }

        private static string GetPermValue(PermissionsData p, string field)
        {
            if (p == null) return null;
            switch (field)
            {
                case "personalUse":           return p.personalUse;
                case "corporateUse":          return p.corporateUse;
                case "commercialUse":         return p.commercialUse;
                case "modification":          return p.modification;
                case "redistribution":        return p.redistribution;
                case "modifiedRedistribution":return p.modifiedRedistribution;
                case "vrchatUpload":          return p.vrchatUpload;
                case "publicAvatar":          return p.publicAvatar;
                case "pedestalUse":           return p.pedestalUse;
                case "videoStreaming":        return p.videoStreaming;
                case "socialMediaPosting":    return p.socialMediaPosting;
                case "adultExpression":       return p.adultExpression;
                case "violentExpression":     return p.violentExpression;
                case "politicalReligiousUse": return p.politicalReligiousUse;
                case "nftUse":               return p.nftUse;
                case "aiTraining":           return p.aiTraining;
                default:                     return null;
            }
        }

        private static string GetReqValue(RequirementsData r, string field)
        {
            if (r == null) return null;
            switch (field)
            {
                case "credit":           return r.credit;
                case "usageReport":      return r.usageReport;
                case "contactBeforeUse": return r.contactBeforeUse;
                default:                 return null;
            }
        }

        // ── アバター総合評価 ──────────────────────────────────────────────────────

        private void DrawAggregation()
        {
            if (_activeEntry?.Products == null || _activeEntry.Products.Count == 0) return;

            if (_aggregation == null)
                _aggregation = BuildAggregation(out _aggregationCovered, out _aggregationUncovered);

            DrawSectionHeader("アバター総合評価 / Avatar License Summary");

            // カバレッジ表示 (#11: 未取得があることを明確に)
            string coverMsg = _aggregationUncovered > 0
                ? $"⚠ JSON 取得済み: {_aggregationCovered}製品 / 未取得: {_aggregationUncovered}製品\n" +
                  "未取得製品のライセンスは不明なため、各項目は「不明（unknown）」として処理されています。"
                : $"全{_aggregationCovered}製品の avatar-license.json から集計しています。";

            bool hasStrict = false;
            foreach (string v in _aggregation.Values)
            {
                if (string.Equals(v, "deny", System.StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(v, "ask",  System.StringComparison.OrdinalIgnoreCase))
                { hasStrict = true; break; }
            }

            EditorGUILayout.HelpBox(coverMsg,
                _aggregationCovered == 0 ? MessageType.Error :
                hasStrict             ? MessageType.Warning : MessageType.Info);

            if (_aggregationCovered == 0)
            {
                EditorGUILayout.HelpBox(
                    "集計できる製品がありません。各製品フォルダに avatar-license.json を配置してください。",
                    MessageType.Warning);
                return;
            }

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                // permissions グリッド
                EditorGUILayout.LabelField("パーミッション", EditorStyles.boldLabel);
                for (int i = 0; i < PermissionRows.Length; i += 2)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        DrawPermCell(PermissionRows[i].Label,
                            _aggregation.TryGetValue(PermissionRows[i].Field, out string v0) ? v0 : null);
                        if (i + 1 < PermissionRows.Length)
                            DrawPermCell(PermissionRows[i + 1].Label,
                                _aggregation.TryGetValue(PermissionRows[i + 1].Field, out string v1) ? v1 : null);
                        GUILayout.FlexibleSpace();
                    }
                }

                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField("義務・要件", EditorStyles.boldLabel);
                using (new EditorGUILayout.HorizontalScope())
                {
                    foreach (var (field, label) in RequirementRows)
                        DrawPermCell(label,
                            _aggregation.TryGetValue(field, out string rv) ? rv : null);
                    GUILayout.FlexibleSpace();
                }

                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField(
                    "集計方針（厳しい順）: deny > ask > conditional > unknown > notMentioned > allow",
                    new GUIStyle(EditorStyles.miniLabel) { wordWrap = true });
            }

            EditorGUILayout.Space(4);
        }

        private Dictionary<string, string> BuildAggregation(out int covered, out int uncovered)
        {
            covered = 0; uncovered = 0;

            var result = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase);
            foreach (var (field, _) in PermissionRows)  result[field] = null;
            foreach (var (field, _) in RequirementRows) result[field] = null;

            if (_activeEntry?.Products == null) return result;

            foreach (ProductLicenseEntry product in _activeEntry.Products)
            {
                AvatarLicenseJson json = TryGetLicenseJson(product);
                if (json == null) { uncovered++; continue; }
                covered++;

                foreach (var (field, _) in PermissionRows)
                    result[field] = Stricter(result[field], GetPermValue(json.permissions, field));
                // Requirements は別の集計順序を使う (#7)
                foreach (var (field, _) in RequirementRows)
                    result[field] = StricterReq(result[field], GetReqValue(json.requirements, field));
            }

            // 未取得製品がある場合、全フィールドに unknown を適用して許可寄りに見えないようにする (#3)
            if (uncovered > 0)
            {
                var keys = new List<string>(result.Keys);
                foreach (string key in keys)
                {
                    bool isReq = System.Array.Exists(RequirementRows, r => r.Field == key);
                    result[key] = isReq
                        ? StricterReq(result[key], "unknown")
                        : Stricter(result[key], "unknown");
                }
            }

            // JSON が1件も取得できていない場合は空文字に
            var allKeys = new List<string>(result.Keys);
            foreach (string key in allKeys)
                if (result[key] == null)
                    result[key] = covered > 0 ? "unknown" : string.Empty;

            return result;
        }

        /// <summary>Permissions: 厳しい方の値を返す。deny > ask > conditional > unknown > notMentioned > allow</summary>
        private static string Stricter(string current, string candidate)
        {
            return PermSeverity(candidate) > PermSeverity(current) ? candidate : current;
        }

        /// <summary>Requirements: 厳しい方の値を返す。ask > required > conditional > unknown > notMentioned > notRequired (#7)</summary>
        private static string StricterReq(string current, string candidate)
        {
            return ReqSeverity(candidate) > ReqSeverity(current) ? candidate : current;
        }

        private static int PermSeverity(string value)
        {
            switch (value?.ToLowerInvariant())
            {
                case "deny":         return 5;
                case "ask":          return 4;
                case "conditional":  return 3;
                case "unknown":      return 2;
                case "notmentioned": return 1;
                case "allow":        return 0;
                default:             return -1; // null → 最小
            }
        }

        private static int ReqSeverity(string value)
        {
            switch (value?.ToLowerInvariant())
            {
                case "ask":          return 4;
                case "required":     return 3;
                case "conditional":  return 2;
                case "unknown":      return 1;
                case "notmentioned": return 0;
                case "notrequired":  return -1;
                default:             return -1; // null → 最小
            }
        }
    }
}
