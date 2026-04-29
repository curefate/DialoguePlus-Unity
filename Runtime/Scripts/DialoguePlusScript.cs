using UnityEngine;

namespace DialoguePlus.Unity
{
    /// <summary>
    /// Imported representation of a .dp script.
    /// This asset is intended to be Addressables-loadable and provides the raw script text.
    /// </summary>
    public sealed class DialoguePlusScript : ScriptableObject
    {
        [TextArea(3, 30)]
        public string Text = string.Empty;

        /// <summary>
        /// Addressables key for this script (human friendly). Optional at runtime.
        /// </summary>
        public string Key = string.Empty;

        /// <summary>
        /// Core-facing sourceId for this script (e.g. addr://dialogue/ch1/main.dp). Optional at runtime.
        /// </summary>
        public string SourceId = string.Empty;

#if UNITY_EDITOR
        /// <summary>
        /// Unity asset path of the source .dp file (Editor-only, for debugging).
        /// </summary>
        public string AssetPath = string.Empty;
#endif
    }
}
