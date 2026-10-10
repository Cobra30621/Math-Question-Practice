using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace Game.Editor
{
    // 把平板需要的 Player Settings 寫成一個選單，換電腦或重建專案時不用再手動找
    internal static class TabletPlayerSettings
    {
        private const string ProductName = "餵小怪獸";
        private const string AndroidIdentifier = "com.practice.feedmonster";

        [MenuItem("Tools/Game/套用平板設定")]
        private static void Apply()
        {
            PlayerSettings.productName = ProductName;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, AndroidIdentifier);

            // 版面只為橫向設計；允許左右兩種橫向，平板怎麼拿都是正的
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;

            AssetDatabase.SaveAssets();
            Debug.Log($"[Game] 已套用平板設定：{ProductName}（{AndroidIdentifier}），只允許橫向");
        }
    }
}
