using System;
using DialoguePlus.Core;

namespace DialoguePlus.Unity
{
    /// <summary>
    /// Import resolver for addr:// sourceIds.
    /// Supports absolute imports (addr://...) and relative imports (./, ../, or plain file name).
    /// </summary>
    public sealed class AddressablesImportResolver : IImportResolver
    {
        public const string Scheme = "addr://";

        public string Resolve(string fromSourceId, string importSpec)
        {
            if (string.IsNullOrWhiteSpace(fromSourceId))
                throw new ArgumentException("fromSourceId cannot be null or empty.", nameof(fromSourceId));
            if (string.IsNullOrWhiteSpace(importSpec))
                throw new ArgumentException("importSpec cannot be null or empty.", nameof(importSpec));

            importSpec = importSpec.Trim();

            // Absolute addr sourceId
            if (importSpec.StartsWith(Scheme, StringComparison.OrdinalIgnoreCase))
                return importSpec;

            // Treat other absolute URIs as already-resolved.
            if (Uri.TryCreate(importSpec, UriKind.Absolute, out _))
                return importSpec;

            var fromKey = DialoguePlusSourceId.KeyFromSourceId(fromSourceId);
            var baseDir = DialoguePlusPathUtils.GetDirectory(fromKey);
            var combinedKey = DialoguePlusPathUtils.CombineAndNormalize(baseDir, importSpec);
            return DialoguePlusSourceId.SourceIdFromKey(combinedKey);
        }
    }
}
