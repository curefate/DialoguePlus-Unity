using System;
using System.Threading;
using System.Threading.Tasks;
using DialoguePlus.Core;

#if DIALOGUEPLUS_ADDRESSABLES
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
#endif

namespace DialoguePlus.Unity
{
    /// <summary>
    /// Content provider that loads <see cref="DialoguePlusScript"/> assets via Addressables.
    /// Enabled only when com.unity.addressables is installed (DIALOGUEPLUS_ADDRESSABLES define).
    /// </summary>
    public sealed class AddressablesContentProvider : IContentProvider
    {
        public bool CanHandle(string sourceId)
            => DialoguePlusSourceId.IsAddressablesSourceId(sourceId);

        public Task<bool> ExistsAsync(string sourceId, CancellationToken ct = default)
        {
#if DIALOGUEPLUS_ADDRESSABLES
            // There's no cheap synchronous existence check in Addressables without loading.
            // We treat OpenTextAsync failures as non-existence.
            return Task.FromResult(true);
#else
            return Task.FromResult(false);
#endif
        }

        public async Task<SourceContent> OpenTextAsync(string sourceId, CancellationToken ct = default)
        {
#if !DIALOGUEPLUS_ADDRESSABLES
            throw new NotSupportedException(
                "AddressablesContentProvider requires com.unity.addressables. Install Addressables or provide another IContentProvider."
            );
#else
            var key = DialoguePlusSourceId.KeyFromSourceId(sourceId);

            AsyncOperationHandle<DialoguePlusScript> handle;
            try
            {
                handle = Addressables.LoadAssetAsync<DialoguePlusScript>(key);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to start Addressables load for key '{key}': {ex.Message}", ex);
            }

            while (!handle.IsDone)
            {
                ct.ThrowIfCancellationRequested();
                await Task.Yield();
            }

            if (handle.Status != AsyncOperationStatus.Succeeded || handle.Result == null)
            {
                Addressables.Release(handle);
                throw new InvalidOperationException($"Addressables failed to load DialoguePlusScript for key '{key}'.");
            }

            var asset = handle.Result;
            var text = asset.Text ?? string.Empty;

            Addressables.Release(handle);
            return new SourceContent(text);
#endif
        }
    }
}
