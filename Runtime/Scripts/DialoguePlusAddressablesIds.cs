using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using DialoguePlus.Core;

namespace DialoguePlus.Unity
{
	/// <summary>
	/// Helpers for addr:// sourceIds and path normalization.
	/// Kept internal to avoid leaking host conventions into public API surface.
	/// </summary>
	internal static class DialoguePlusAddressablesIds
	{
		public const string AddressablesScheme = "addr://";

		public static string SourceIdFromKey(string key)
		{
			if (string.IsNullOrWhiteSpace(key))
				throw new ArgumentException("key cannot be null or empty.", nameof(key));

			key = Normalize(key);
			return AddressablesScheme + key;
		}

		public static bool IsAddressablesSourceId(string sourceId)
			=> !string.IsNullOrWhiteSpace(sourceId)
			&& sourceId.StartsWith(AddressablesScheme, StringComparison.OrdinalIgnoreCase);

		public static string KeyFromSourceId(string sourceId)
		{
			if (!IsAddressablesSourceId(sourceId))
				throw new ArgumentException($"Expected sourceId starting with '{AddressablesScheme}', got '{sourceId}'.", nameof(sourceId));

			var key = sourceId.Substring(AddressablesScheme.Length);
			return Normalize(key);
		}

		public static string NormalizeSlashes(string s) => s.Replace('\\', '/');

		public static string GetDirectory(string path)
		{
			path = NormalizeSlashes(path);
			var i = path.LastIndexOf('/');
			if (i < 0) return string.Empty;
			return path.Substring(0, i);
		}

		public static string CombineAndNormalize(string baseDir, string relative)
		{
			baseDir = NormalizeSlashes(baseDir ?? string.Empty);
			relative = NormalizeSlashes(relative ?? string.Empty);

			if (string.IsNullOrEmpty(baseDir))
				return Normalize(relative);

			if (!baseDir.EndsWith("/", StringComparison.Ordinal))
				baseDir += "/";

			return Normalize(baseDir + relative);
		}

		public static string Normalize(string path)
		{
			path = NormalizeSlashes(path);

			while (path.StartsWith("/", StringComparison.Ordinal))
				path = path.Substring(1);

			var parts = path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
			var stack = new List<string>(parts.Length);

			foreach (var p in parts)
			{
				if (p == ".") continue;
				if (p == "..")
				{
					if (stack.Count > 0)
						stack.RemoveAt(stack.Count - 1);
					else
						continue; // clamp
					continue;
				}
				stack.Add(p);
			}

			return string.Join("/", stack);
		}
	}

	/// <summary>
	/// Creates DialoguePlus Core resolvers suitable for Unity runtime.
	/// Kept internal to minimize public surface and file count.
	/// </summary>
	internal static class DialoguePlusUnityResolverFactory
	{
		/// <summary>
		/// Provider order: cache (override) -> addressables (addr://) -> file system.
		/// </summary>
		public static IScriptResolver CreateRuntimeResolver(
			ConcurrentDictionary<string, SourceContent>? cache = null,
			IImportResolver? importResolver = null)
		{
			var content = new ContentResolver()
				.Register(new CacheContentProvider(cache))
				.Register(new AddressablesContentProvider())
				.Register(new FileContentProvider());

			return new ScriptResolver(content, importResolver ?? new AddressablesImportResolver());
		}
	}
}
