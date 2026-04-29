using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using DialoguePlus.Core;
using DialoguePlus.Unity;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace DialoguePlus.Unity.Editor
{
    [ScriptedImporter(1, "dp")]
    public sealed class DialoguePlusScriptImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            var settings = DialoguePlusSettingsProvider.GetOrCreateSettings();
            var root = settings.GetNormalizedRootFolder();

            var text = File.ReadAllText(ctx.assetPath);

            var script = ScriptableObject.CreateInstance<DialoguePlusScript>();
            script.Text = text;

            var key = TryComputeKey(ctx.assetPath, root, out var keyWarning);
            if (key == null)
            {
                if (!string.IsNullOrWhiteSpace(keyWarning))
                    ctx.LogImportWarning($"[DialoguePlus] {keyWarning}");

                // Keep this empty so runtime callers don't accidentally rely on stale values.
                script.SourceId = string.Empty;
            }
            else
            {
                script.SourceId = DialoguePlusAddressablesIds.SourceIdFromKey(key);
            }

#if UNITY_EDITOR
            script.AssetPath = ctx.assetPath;
#endif

            // Optional: import existence warnings.
            // We don't persist resolvedImports; runtime already uses Core for import traversal.
            if (!string.IsNullOrEmpty(script.SourceId))
            {
                WarnMissingImports(ctx, script.SourceId, text, root);
            }

            ctx.AddObjectToAsset("DialoguePlusScript", script);
            ctx.SetMainObject(script);
        }

        private static void WarnMissingImports(AssetImportContext ctx, string entrySourceId, string text, string normalizedRoot)
        {
            var importResolver = new AddressablesImportResolver();
            var importSpecs = ScanImportSpecs(text);

            foreach (var spec in importSpecs)
            {
                string targetSourceId;
                try
                {
                    targetSourceId = importResolver.Resolve(entrySourceId, spec);
                }
                catch (Exception ex)
                {
                    ctx.LogImportWarning($"[DialoguePlus] Failed to resolve import '{spec}' from '{entrySourceId}': {ex.Message}");
                    continue;
                }

                // Only validate addr:// targets.
                if (!DialoguePlusAddressablesIds.IsAddressablesSourceId(targetSourceId))
                    continue;

                var targetKey = DialoguePlusAddressablesIds.KeyFromSourceId(targetSourceId);
                var targetAssetPath = (normalizedRoot + targetKey).Replace('\\', '/');

                if (!File.Exists(targetAssetPath) && AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(targetAssetPath) == null)
                {
                    ctx.LogImportWarning(
                        $"[DialoguePlus] Import target not found: '{spec}' -> '{targetSourceId}'. Expected asset at '{targetAssetPath}'."
                    );
                }
            }
        }

        private static string? TryComputeKey(string assetPath, string normalizedRootFolder, out string? warning)
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
            key = DialoguePlusAddressablesIds.Normalize(key);
            if (string.IsNullOrWhiteSpace(key))
            {
                warning = $"Computed empty key for '{assetPath}' with RootFolder '{normalizedRootFolder}'.";
                return null;
            }

            return key;
        }

        // Matches: import "..."  OR  import '...'  OR  import ...
        private static readonly Regex ImportLine = new Regex(
            "^\\s*import\\s+(?<spec>.+?)\\s*$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant
        );

        private static List<string> ScanImportSpecs(string text)
        {
            var result = new List<string>();
            if (string.IsNullOrEmpty(text)) return result;

            var lines = text.Split('\n');
            foreach (var raw in lines)
            {
                var line = raw.TrimEnd('\r');
                if (string.IsNullOrWhiteSpace(line)) continue;

                var trimmedStart = line.TrimStart();
                if (trimmedStart.StartsWith("#")) continue;

                var m = ImportLine.Match(line);
                if (!m.Success) continue;

                var spec = m.Groups["spec"].Value.Trim();

                if ((spec.StartsWith("\"") && spec.EndsWith("\"")) || (spec.StartsWith("'") && spec.EndsWith("'")))
                {
                    if (spec.Length >= 2)
                        spec = spec.Substring(1, spec.Length - 2);
                }

                if (!string.IsNullOrWhiteSpace(spec))
                    result.Add(spec);
            }

            return result;
        }
    }
}
