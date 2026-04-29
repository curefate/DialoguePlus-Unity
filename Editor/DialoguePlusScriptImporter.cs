using System;
using System.IO;
using DialoguePlus.Unity;
using DialoguePlus.Core;
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

            var key = DialoguePlusKeyUtility.TryComputeKey(ctx.assetPath, root, out var keyWarning);
            if (key == null)
            {
                if (!string.IsNullOrWhiteSpace(keyWarning))
                    ctx.LogImportWarning($"[DialoguePlus] {keyWarning}");

                // Keep these empty so runtime callers don't accidentally rely on stale values.
                script.Key = string.Empty;
                script.SourceId = string.Empty;
            }
            else
            {
                script.Key = key;
                script.SourceId = DialoguePlusSourceId.SourceIdFromKey(key);
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
            var importSpecs = DialoguePlusImportScanner.ScanImportSpecs(text);

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
                if (!DialoguePlusSourceId.IsAddressablesSourceId(targetSourceId))
                    continue;

                var targetKey = DialoguePlusSourceId.KeyFromSourceId(targetSourceId);
                var targetAssetPath = (normalizedRoot + targetKey).Replace('\\', '/');

                if (!File.Exists(targetAssetPath) && AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(targetAssetPath) == null)
                {
                    ctx.LogImportWarning(
                        $"[DialoguePlus] Import target not found: '{spec}' -> '{targetSourceId}'. Expected asset at '{targetAssetPath}'."
                    );
                }
            }
        }
    }
}
