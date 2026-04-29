using System;
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
}
