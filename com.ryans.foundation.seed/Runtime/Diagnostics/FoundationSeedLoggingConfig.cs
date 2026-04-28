using UnityEngine;

namespace FoundationSeed.Diagnostics
{
    [CreateAssetMenu(menuName = "Foundation Seed/Logging Config", fileName = "FoundationSeedLoggingConfig")]
    public sealed class FoundationSeedLoggingConfig : ScriptableObject
    {
        public bool enableInEditor = true;
        public bool enableInDevelopmentBuild = true;
        public bool enableInReleaseBuild = false;
        public bool writeStructuredJsonl = true;
        public bool writeReadableTranscript = true;
        public bool captureUnityWarningsAndErrors = true;
        public bool writeToProjectFolderInEditor = true;
        public string projectSessionFolderName = "PlantLogs/SessionLogs";
        public string runtimeSessionFolderName = "PlantLogs/SessionLogs";
        public int maxRetainedSessions = 10;
    }
}
