using UnityEngine;

namespace FoundationSeed.Editor.Diagnostics
{
    [CreateAssetMenu(menuName = "Foundation Seed/Intent Logging Config", fileName = "FoundationSeedIntentLoggingConfig")]
    public sealed class FoundationSeedIntentLoggingConfig : ScriptableObject
    {
        public bool enabledInEditor = false;
        public string projectLogFolderName = "PlantLogs/CodexLogs";
        public string logFileName = "codex_intent_log.jsonl";
        public int maxRetainedEntries = 500;
    }
}
