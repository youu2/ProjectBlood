#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace ProjectBlood.EditorTools
{
    /// <summary>
    /// 将 Assets/Art/Sound/SFX 下所有 AudioClip 分配到 "sfx" AssetBundle。
    /// ResKit 的 GetAllAssetBundleNames 只识别资源级分配，文件夹级不会生效。
    /// </summary>
    public static class AssignSfxBundle
    {
        private const string SfxRoot = "Assets/Art/Sound/SFX";
        private const string BundleName = "sfx";

        [MenuItem("Tools/ProjectBlood/Assign SFX AssetBundle")]
        public static void Assign()
        {
            var guids = AssetDatabase.FindAssets("t:AudioClip", new[] { SfxRoot });
            int changed = 0;

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path);
                if (importer == null || importer.assetBundleName == BundleName)
                    continue;

                importer.assetBundleName = BundleName;
                importer.SaveAndReimport();
                changed++;
            }

            AssetDatabase.Refresh();
            Debug.Log($"[AssignSfxBundle] 已将 {changed} 个 AudioClip 分配到 bundle '{BundleName}'。");
        }
    }
}
#endif
