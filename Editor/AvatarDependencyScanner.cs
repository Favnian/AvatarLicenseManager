using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace StarSideUp.AvatarLicenseManager.Editor
{
    public static class AvatarDependencyScanner
    {
        /// <summary>
        /// GameObject（Prefabアセット or Sceneインスタンス）からAssetパスを取得する。
        /// </summary>
        public static string GetPrefabAssetPath(Object obj)
        {
            if (obj == null) return null;

            // Prefabアセットはそのままパスが取れる
            string path = AssetDatabase.GetAssetPath(obj);
            if (!string.IsNullOrEmpty(path)) return path;

            // SceneのGameObject（Hierarchy）の場合：最近接のPrefabルートを探す
            if (obj is GameObject go)
                return PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(go);

            return null;
        }

        /// <summary>
        /// Prefabの推移的依存アセットを解析し、製品ルート候補 (Assets/Vendor/Product) を返す。
        /// SPEC §Product Root Model に従い、3階層目までを製品ルートとして扱う。
        /// </summary>
        public static List<string> FindProductRoots(string prefabAssetPath)
        {
            string[] deps = AssetDatabase.GetDependencies(prefabAssetPath, recursive: true);
            var roots = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);

            foreach (string dep in deps)
            {
                // Packages/ や組み込みリソースは除外
                if (!dep.StartsWith("Assets/", System.StringComparison.OrdinalIgnoreCase))
                    continue;

                string[] parts = dep.Split('/');
                // 必要構成: Assets / <Vendor> / <Product> / <file or subdir>
                // → 最低4パーツ
                if (parts.Length < 4) continue;

                string root = parts[0] + "/" + parts[1] + "/" + parts[2];

                // フォルダとして実在するか確認
                if (AssetDatabase.IsValidFolder(root))
                    roots.Add(root);
            }

            var list = new List<string>(roots);
            list.Sort(System.StringComparer.OrdinalIgnoreCase);
            return list;
        }

        /// <summary>製品ルートパスから販売者名を抽出する。</summary>
        public static string ExtractVendorName(string productRootPath)
        {
            // "Assets/VendorName/ProductName" → "VendorName"
            string[] parts = productRootPath.Split('/');
            return parts.Length >= 2 ? parts[1] : string.Empty;
        }

        /// <summary>製品ルートパスから製品名を抽出する。</summary>
        public static string ExtractProductName(string productRootPath)
        {
            // "Assets/VendorName/ProductName" → "ProductName"
            string[] parts = productRootPath.Split('/');
            return parts.Length >= 3 ? parts[2] : System.IO.Path.GetFileName(productRootPath);
        }
    }
}
