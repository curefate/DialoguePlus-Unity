using System.Collections.Concurrent;
using DialoguePlus.Core;

namespace DialoguePlus.Unity
{
    public static class DialoguePlusUnityResolverFactory
    {
        /// <summary>
        /// Creates a resolver suitable for Unity runtime.
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
