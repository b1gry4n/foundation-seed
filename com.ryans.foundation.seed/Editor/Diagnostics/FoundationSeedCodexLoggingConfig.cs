using UnityEngine;

namespace FoundationSeed.Editor.Diagnostics
{
    [CreateAssetMenu(menuName = "Foundation Seed/Codex Logging Config", fileName = "FoundationSeedCodexLoggingConfig")]
    public sealed class FoundationSeedCodexLoggingConfig : ScriptableObject
    {
        public bool enabledInEditor = true;
        public bool logImportedAssets = true;
        public bool logDeletedAssets = true;
        public bool logMovedAssets = true;
        public string projectLogFolderName = "PlantLogs/CodexLogs";
        public string logFileName = "codex_change_log.jsonl";
        public int maxRetainedEntries = 500;
    }
}
