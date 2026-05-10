using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace StarSideUp.AvatarLicenseManager.Editor
{
    public static class AvatarDependencyScanner
    {
        // これらの名前が parts[2] にある場合は製品フォルダではなく汎用フォルダと判断し、
        // ベンダールート (Assets/<Vendor>) を製品ルートとして使う (#1 fix)
        private static readonly HashSet<string> CommonUnityFolderNames =
            new HashSet<string>(System.StringComparer.OrdinalIgnoreCase)
        {
            "Prefabs", "Scripts", "Materials", "Textures", "Animations", "Shaders",
            "Audio", "Sounds", "Video", "Sprites", "Fonts", "Editor", "Runtime",
            "Resources", "Plugins", "Controllers", "Animators", "Models", "FBX",
            "Meshes", "Generated", "License", "Licenses", "VN3", "Terms", "Rules",
            "Settings", "Images", "Data", "Config", "Samples", "Documentation",
            "Gimmick", "Shader", "Texture", "Material", "Anim", "Animator",
        };

        /// <summary>
        /// GameObject（Prefabアセット or Sceneインスタンス）からAssetパスを取得する。
        /// </summary>
        public static string GetPrefabAssetPath(Object obj)
        {
            if (obj == null) return null;

            string path = AssetDatabase.GetAssetPath(obj);
            if (!string.IsNullOrEmpty(path)) return path;

            if (obj is GameObject go)
                return PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(go);

            return null;
        }

        /// <summary>
        /// Prefabの推移的依存アセットを解析し、製品ルート候補を返す。
        /// Assets/&lt;Vendor&gt;/&lt;Product&gt; パターンを基本とし、
        /// 汎用フォルダ名が Product 位置にある場合は Assets/&lt;Vendor&gt; をルートとする。
        /// </summary>
        public static List<string> FindProductRoots(string prefabAssetPath)
        {
            string[] deps = AssetDatabase.GetDependencies(prefabAssetPath, recursive: true);
            var roots = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);

            foreach (string dep in deps)
            {
                if (!dep.StartsWith("Assets/", System.StringComparison.OrdinalIgnoreCase))
                    continue;

                string[] parts = dep.Split('/');
                if (parts.Length < 4) continue; // Assets / <p1> / <p2> / <file> が最小

                string root;
                if (CommonUnityFolderNames.Contains(parts[2]))
                {
                    // parts[2] が汎用フォルダ名 → ベンダールートが製品ルート
                    root = parts[0] + "/" + parts[1];
                }
                else
                {
                    root = parts[0] + "/" + parts[1] + "/" + parts[2];
                }

                if (AssetDatabase.IsValidFolder(root))
                    roots.Add(root);
            }

            var list = new List<string>(roots);
            list.Sort(System.StringComparer.OrdinalIgnoreCase);
            return list;
        }

        /// <summary>製品ルートパスからベンダールートパス（Assets/&lt;Vendor&gt;）を返す。</summary>
        public static string ExtractVendorRootPath(string productRootPath)
        {
            string[] parts = productRootPath.Split('/');
            return parts.Length >= 2 ? parts[0] + "/" + parts[1] : productRootPath;
        }

        /// <summary>製品ルートパスから販売者名を抽出する。</summary>
        public static string ExtractVendorName(string productRootPath)
        {
            string[] parts = productRootPath.Split('/');
            return parts.Length >= 2 ? parts[1] : string.Empty;
        }

        /// <summary>製品ルートパスから製品名を抽出する。</summary>
        public static string ExtractProductName(string productRootPath)
        {
            string[] parts = productRootPath.Split('/');
            if (parts.Length >= 3) return parts[2];
            if (parts.Length >= 2) return parts[1]; // ベンダールートが製品ルートの場合
            return System.IO.Path.GetFileName(productRootPath);
        }
    }
}
