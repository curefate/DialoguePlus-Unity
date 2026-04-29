using System;
using DialoguePlus.Unity;

namespace DialoguePlus.Unity.Editor
{
    internal static class DialoguePlusKeyUtility
    {
        public static string? TryComputeKey(string assetPath, string normalizedRootFolder, out string? warning)
        {
            warning = null;
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                warning = "assetPath is null/empty";
                return null;
            }

            assetPath = assetPath.Replace('\\', '/');
            if (!assetPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
            {
                warning = $"Asset path '{assetPath}' is not under Assets/.";
                return null;
            }

            normalizedRootFolder = (normalizedRootFolder ?? "Assets/").Replace('\\', '/');
            if (!normalizedRootFolder.EndsWith("/", StringComparison.Ordinal))
                normalizedRootFolder += "/";

            if (!assetPath.StartsWith(normalizedRootFolder, StringComparison.OrdinalIgnoreCase))
            {
                warning = $"'{assetPath}' is outside RootFolder '{normalizedRootFolder}'.";
                return null;
            }

            var key = assetPath.Substring(normalizedRootFolder.Length);
            key = DialoguePlusPathUtils.Normalize(key);
            if (string.IsNullOrWhiteSpace(key))
            {
                warning = $"Computed empty key for '{assetPath}' with RootFolder '{normalizedRootFolder}'.";
                return null;
            }

            return key;
        }

        public static string SourceIdFromKey(string key) => DialoguePlusSourceId.SourceIdFromKey(key);
    }
}
