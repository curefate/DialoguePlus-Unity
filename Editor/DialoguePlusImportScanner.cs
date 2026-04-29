using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace DialoguePlus.Unity.Editor
{
    internal static class DialoguePlusImportScanner
    {
        // Matches: import "..."  OR  import '...'  OR  import ...
        private static readonly Regex ImportLine = new Regex(
            "^\\s*import\\s+(?<spec>.+?)\\s*$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant
        );

        public static List<string> ScanImportSpecs(string text)
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
