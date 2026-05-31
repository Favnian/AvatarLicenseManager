using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace StarSideUp.AvatarLicenseManager.Editor
{
    /// <summary>avatar-license.json / StarSideUp-License.json などのデータモデル。</summary>
    [System.Serializable]
    public sealed class AvatarLicenseJson
    {
        public int schemaVersion;
        public string productName;
        public string vendorName;
        public string scope;
        public List<string> sourceDocuments = new List<string>();
        public PermissionsData permissions = new PermissionsData();
        public RequirementsData requirements = new RequirementsData();
        public string notes;
        public string lastReviewedAt;

        /// <summary>AssetパスのJSONファイルを読み込む。スキーマ不一致や読み込みエラーの場合は null。</summary>
        public static AvatarLicenseJson Load(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath)) return null;
            string absolute = Application.dataPath + assetPath.Substring("Assets".Length);
            if (!File.Exists(absolute)) return null;
            try
            {
                string json = File.ReadAllText(absolute, System.Text.Encoding.UTF8);
                var result = JsonUtility.FromJson<AvatarLicenseJson>(json);
                if (result == null || result.schemaVersion <= 0)
                {
                    // schemaVersion なし = 本ツール用フォーマットではない (#8)
                    Debug.LogWarning(
                        $"[AvatarLicenseManager] {System.IO.Path.GetFileName(absolute)}: " +
                        "schemaVersion フィールドがないため読み飛ばしました。" +
                        " avatar-license.json スキーマに準拠しているか確認してください。");
                    return null;
                }
                return result;
            }
            catch
            {
                return null;
            }
        }
    }

    [System.Serializable]
    public sealed class PermissionsData
    {
        public string personalUse;
        public string corporateUse;
        public string commercialUse;
        public string modification;
        public string redistribution;
        public string modifiedRedistribution;
        public string vrchatUpload;
        public string publicAvatar;
        public string pedestalUse;
        public string videoStreaming;
        public string socialMediaPosting;
        public string adultExpression;
        public string violentExpression;
        public string politicalReligiousUse;
        public string nftUse;
        public string aiTraining;
    }

    [System.Serializable]
    public sealed class RequirementsData
    {
        public string credit;
        public string usageReport;
        public string contactBeforeUse;
    }
}
