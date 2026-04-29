using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace DialoguePlus.Unity.Editor
{
    /// <summary>
    /// Project settings for DialoguePlus Unity integration.
    /// </summary>
    public sealed class DialoguePlusSettings : ScriptableObject
    {
        [Tooltip("Asset path prefix used to compute Addressables key for .dp files. Must start with 'Assets/'. Default: 'Assets/'.")]
        public string RootFolder = "Assets/";

        public string GetNormalizedRootFolder()
        {
            var root = RootFolder ?? "Assets/";
            root = root.Replace('\\', '/');
            if (!root.StartsWith("Assets", StringComparison.OrdinalIgnoreCase))
            {
                // Keep it predictable: force Assets.
                root = "Assets/";
            }
            if (!root.EndsWith("/", StringComparison.Ordinal))
                root += "/";
            if (!root.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                root = "Assets/";
            return root;
        }
    }

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
