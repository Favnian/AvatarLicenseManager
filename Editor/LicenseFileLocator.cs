using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace StarSideUp.AvatarLicenseManager.Editor
{
    public static class LicenseFileLocator
    {
        // Machine-readable license files take priority (SPEC §License File Search Priority)
        private static readonly string[] MachineReadableNames =
        {
            "avatar-license.json",
            "license.json",
            "vn3-license.json"
        };

        // Folder names that conventionally contain license documents
        private static readonly string[] LicenseFolderNames =
        {
            "License", "Licenses", "VN3", "Terms", "規約", "利用規約", "ライセンス"
        };

        // Keywords matched against filename (without extension) to detect license documents
        private static readonly string[] DocumentKeywords =
        {
            "license", "licence", "ライセンス", "利用規約", "terms", "eula", "vn3"
        };

        private static readonly HashSet<string> DocumentExtensions =
            new HashSet<string>(System.StringComparer.OrdinalIgnoreCase)
            {
                ".txt", ".pdf", ".md", ".html", ".htm", ".json"
            };

        public static List<string> FindInFolder(string folderAssetPath)
        {
            var results = new List<string>();
            if (string.IsNullOrEmpty(folderAssetPath)) return results;

            string absolutePath = AssetToAbsolute(folderAssetPath);
            if (!Directory.Exists(absolutePath)) return results;

            // Priority 1: machine-readable JSON at folder root
            foreach (string name in MachineReadableNames)
            {
                string candidate = Path.Combine(absolutePath, name);
                if (File.Exists(candidate))
                    results.Add(AbsoluteToAsset(candidate));
            }

            // Priority 2: license document files found by recursive scan
            CollectDocuments(absolutePath, results);

            return Deduplicate(results);
        }

        private static void CollectDocuments(string directory, List<string> results)
        {
            foreach (string file in Directory.GetFiles(directory))
            {
                if (!DocumentExtensions.Contains(Path.GetExtension(file))) continue;

                string stem = Path.GetFileNameWithoutExtension(file).ToLowerInvariant();
                foreach (string keyword in DocumentKeywords)
                {
                    if (stem.Contains(keyword.ToLowerInvariant()))
                    {
                        results.Add(AbsoluteToAsset(file));
                        break;
                    }
                }
            }

            foreach (string subDir in Directory.GetDirectories(directory))
            {
                string dirName = Path.GetFileName(subDir);
                if (IsLicenseFolder(dirName))
                {
                    // Collect all document files inside named license folders
                    foreach (string file in Directory.GetFiles(subDir, "*", SearchOption.AllDirectories))
                    {
                        if (DocumentExtensions.Contains(Path.GetExtension(file)))
                            results.Add(AbsoluteToAsset(file));
                    }
                }
                else
                {
                    CollectDocuments(subDir, results);
                }
            }
        }

        private static bool IsLicenseFolder(string name)
        {
            foreach (string candidate in LicenseFolderNames)
            {
                if (string.Equals(name, candidate, System.StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        private static List<string> Deduplicate(List<string> paths)
        {
            var seen = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
            var result = new List<string>(paths.Count);
            foreach (string path in paths)
            {
                if (seen.Add(path))
                    result.Add(path);
            }
            result.Sort(System.StringComparer.OrdinalIgnoreCase);
            return result;
        }

        private static string AssetToAbsolute(string assetPath)
        {
            // Application.dataPath = "…/Assets"; assetPath starts with "Assets"
            return Application.dataPath + assetPath.Substring("Assets".Length).Replace('/', Path.DirectorySeparatorChar);
        }

        private static string AbsoluteToAsset(string absolutePath)
        {
            string dataPath = Application.dataPath.Replace('\\', '/');
            string normalized = absolutePath.Replace('\\', '/');
            if (normalized.StartsWith(dataPath, System.StringComparison.OrdinalIgnoreCase))
                return "Assets" + normalized.Substring(dataPath.Length);
            return absolutePath;
        }
    }
}
