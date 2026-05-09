using System.Collections.Generic;
using UnityEngine;

namespace StarSideUp.AvatarLicenseManager.Editor
{
    public sealed class AvatarLicenseDatabase : ScriptableObject
    {
        public List<AvatarAnalysisEntry> Entries = new List<AvatarAnalysisEntry>();
    }

    [System.Serializable]
    public sealed class AvatarAnalysisEntry
    {
        [Tooltip("分析対象アバターPrefabのAssetパス")]
        public string AvatarPrefabPath;

        [Tooltip("表示名（省略時はPrefab名）")]
        public string DisplayName;

        [Tooltip("最終分析日時")]
        public string LastAnalyzedAt;

        [Tooltip("依存解析で検出された製品エントリー一覧")]
        public List<ProductLicenseEntry> Products = new List<ProductLicenseEntry>();
    }

    [System.Serializable]
    public sealed class ProductLicenseEntry
    {
        [Tooltip("製品ルートフォルダ（Assets/<Vendor>/<Product>）")]
        public string ProductRootPath;

        [Tooltip("製品名（自動抽出・編集可）")]
        public string ProductName;

        [Tooltip("販売者名（自動抽出・編集可）")]
        public string VendorName;

        [Tooltip("スキャンで検出したライセンスファイルのパス一覧")]
        public List<string> LicenseFilePaths = new List<string>();

        [Tooltip("補足メモ（利用条件の要約など）")]
        public string Note;
    }
}
