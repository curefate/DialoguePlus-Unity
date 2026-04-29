using System.IO;
using UnityEditor;
using UnityEngine;

namespace DialoguePlus.Unity.Editor
{
    internal static class DialoguePlusSettingsProvider
    {
        public const string DefaultSettingsPath = "Assets/DialoguePlusSettings.asset";

        public static DialoguePlusSettings GetOrCreateSettings()
        {
            var settings = AssetDatabase.LoadAssetAtPath<DialoguePlusSettings>(DefaultSettingsPath);
            if (settings != null) return settings;

            settings = ScriptableObject.CreateInstance<DialoguePlusSettings>();

            var dir = Path.GetDirectoryName(DefaultSettingsPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
                AssetDatabase.Refresh();
            }

            AssetDatabase.CreateAsset(settings, DefaultSettingsPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return settings;
        }
    }
}
