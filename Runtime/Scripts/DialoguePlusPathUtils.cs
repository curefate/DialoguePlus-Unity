using System;
using System.Collections.Generic;

namespace DialoguePlus.Unity
{
    internal static class DialoguePlusPathUtils
    {
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

            // preserve leading slash? our keys should not start with '/', so we strip it.
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
                    {
                        // Clamped: don't allow walking beyond root.
                        continue;
                    }
                    continue;
                }
                stack.Add(p);
            }

            return string.Join("/", stack);
        }
    }
}
