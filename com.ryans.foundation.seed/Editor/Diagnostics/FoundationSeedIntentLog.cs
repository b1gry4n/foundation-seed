using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace FoundationSeed.Editor.Diagnostics
{
    public static class FoundationSeedIntentLog
    {
        private const string ConfigAssetPath = "Assets/Plant/Resources/FoundationSeedIntentLoggingConfig.asset";

        private static FoundationSeedIntentLoggingConfig LoadConfig()
        {
            return AssetDatabase.LoadAssetAtPath<FoundationSeedIntentLoggingConfig>(ConfigAssetPath);
        }

        public static bool IsEnabled()
        {
            FoundationSeedIntentLoggingConfig config = LoadConfig();
            return config != null && config.enabledInEditor;
        }

        public static string ResolveLogPath()
        {
            FoundationSeedIntentLoggingConfig config = LoadConfig();
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? string.Empty;
            string folderName = config != null && !string.IsNullOrWhiteSpace(config.projectLogFolderName)
                ? config.projectLogFolderName.Trim()
                : "PlantLogs/CodexLogs";
            string fileName = config != null && !string.IsNullOrWhiteSpace(config.logFileName)
                ? config.logFileName.Trim()
                : "codex_intent_log.jsonl";
            return Path.Combine(projectRoot, folderName, fileName);
        }

        public static int ResolveRetentionCap()
        {
            FoundationSeedIntentLoggingConfig config = LoadConfig();
            return config != null && config.maxRetainedEntries > 0 ? config.maxRetainedEntries : 500;
        }

        public static bool TryAppendIntent(string summary, string intent, string systems, string evidence)
        {
            if (!IsEnabled())
            {
                return false;
            }

            string logPath = ResolveLogPath();
            string directory = Path.GetDirectoryName(logPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string utc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
            string line =
                "{\"utc\":\"" + Escape(utc) +
                "\",\"summary\":\"" + Escape(summary) +
                "\",\"intent\":\"" + Escape(intent) +
                "\",\"systems\":\"" + Escape(systems) +
                "\",\"evidence\":\"" + Escape(evidence) + "\"}";

            File.AppendAllText(logPath, line + Environment.NewLine, Encoding.UTF8);
            TrimToRetention(logPath, ResolveRetentionCap());
            return true;
        }

        private static void TrimToRetention(string logPath, int maxEntries)
        {
            if (maxEntries <= 0 || !File.Exists(logPath))
            {
                return;
            }

            string[] lines = File.ReadAllLines(logPath);
            if (lines.Length <= maxEntries)
            {
                return;
            }

            string[] trimmed = lines.Skip(lines.Length - maxEntries).ToArray();
            File.WriteAllLines(logPath, trimmed, Encoding.UTF8);
        }

        private static string Escape(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            return value.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n");
        }
    }
}
