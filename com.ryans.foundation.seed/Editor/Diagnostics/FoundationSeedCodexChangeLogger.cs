using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace FoundationSeed.Editor.Diagnostics
{
    internal sealed class FoundationSeedCodexChangeLogger : AssetPostprocessor
    {
        private const string ConfigAssetPath = "Assets/Plant/Resources/FoundationSeedCodexLoggingConfig.asset";

        private static FoundationSeedCodexLoggingConfig LoadConfig()
        {
            return AssetDatabase.LoadAssetAtPath<FoundationSeedCodexLoggingConfig>(ConfigAssetPath);
        }

        public static string ResolveLogPath()
        {
            FoundationSeedCodexLoggingConfig config = LoadConfig();
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? string.Empty;
            string folderName = config != null && !string.IsNullOrWhiteSpace(config.projectLogFolderName)
                ? config.projectLogFolderName.Trim()
                : "PlantLogs/CodexLogs";
            string fileName = config != null && !string.IsNullOrWhiteSpace(config.logFileName)
                ? config.logFileName.Trim()
                : "codex_change_log.jsonl";

            return Path.Combine(projectRoot, folderName, fileName);
        }

        public static int ResolveRetentionCap()
        {
            FoundationSeedCodexLoggingConfig config = LoadConfig();
            if (config == null || config.maxRetainedEntries <= 0)
            {
                return 500;
            }

            return config.maxRetainedEntries;
        }

        private static bool IsEnabled()
        {
            FoundationSeedCodexLoggingConfig config = LoadConfig();
            return config == null || config.enabledInEditor;
        }

        private static bool ShouldLogImported()
        {
            FoundationSeedCodexLoggingConfig config = LoadConfig();
            return config == null || config.logImportedAssets;
        }

        private static bool ShouldLogDeleted()
        {
            FoundationSeedCodexLoggingConfig config = LoadConfig();
            return config == null || config.logDeletedAssets;
        }

        private static bool ShouldLogMoved()
        {
            FoundationSeedCodexLoggingConfig config = LoadConfig();
            return config == null || config.logMovedAssets;
        }

        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            if (!IsEnabled())
            {
                return;
            }

            if ((importedAssets == null || importedAssets.Length == 0)
                && (deletedAssets == null || deletedAssets.Length == 0)
                && (movedAssets == null || movedAssets.Length == 0))
            {
                return;
            }

            try
            {
                string logPath = ResolveLogPath();
                string logDirectory = Path.GetDirectoryName(logPath);
                if (!string.IsNullOrWhiteSpace(logDirectory))
                {
                    Directory.CreateDirectory(logDirectory);
                }

                using (StreamWriter writer = new StreamWriter(logPath, append: true, Encoding.UTF8))
                {
                    string utc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);

                    if (ShouldLogImported() && importedAssets != null)
                    {
                        for (int i = 0; i < importedAssets.Length; i += 1)
                        {
                            WriteEntry(writer, utc, "imported", importedAssets[i], string.Empty);
                        }
                    }

                    if (ShouldLogDeleted() && deletedAssets != null)
                    {
                        for (int i = 0; i < deletedAssets.Length; i += 1)
                        {
                            WriteEntry(writer, utc, "deleted", deletedAssets[i], string.Empty);
                        }
                    }

                    if (ShouldLogMoved() && movedAssets != null)
                    {
                        for (int i = 0; i < movedAssets.Length; i += 1)
                        {
                            string from = movedFromAssetPaths != null && i < movedFromAssetPaths.Length
                                ? movedFromAssetPaths[i]
                                : string.Empty;
                            WriteEntry(writer, utc, "moved", movedAssets[i], from);
                        }
                    }
                }

                TrimToRetention(logPath, ResolveRetentionCap());
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[FoundationSeedCodexChangeLogger] Failed to write codex change log. " + exception.Message);
            }
        }

        private static void WriteEntry(StreamWriter writer, string utc, string kind, string path, string fromPath)
        {
            writer.WriteLine(
                "{\"utc\":\"" + Escape(utc) +
                "\",\"kind\":\"" + Escape(kind) +
                "\",\"path\":\"" + Escape(path) +
                "\",\"from\":\"" + Escape(fromPath) + "\"}");
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
