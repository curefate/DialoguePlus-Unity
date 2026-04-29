using System;

namespace DialoguePlus.Unity
{
    internal static class DialoguePlusSourceId
    {
        public const string AddressablesScheme = "addr://";

        public static string SourceIdFromKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("key cannot be null or empty.", nameof(key));

            key = DialoguePlusPathUtils.Normalize(key);
            return AddressablesScheme + key;
        }

        public static bool IsAddressablesSourceId(string sourceId)
            => !string.IsNullOrWhiteSpace(sourceId) && sourceId.StartsWith(AddressablesScheme, StringComparison.OrdinalIgnoreCase);

        public static string KeyFromSourceId(string sourceId)
        {
            if (!IsAddressablesSourceId(sourceId))
                throw new ArgumentException($"Expected sourceId starting with '{AddressablesScheme}', got '{sourceId}'.", nameof(sourceId));

            var key = sourceId.Substring(AddressablesScheme.Length);
            return DialoguePlusPathUtils.Normalize(key);
        }
    }
}
