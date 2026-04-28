using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FoundationSeed.Diagnostics
{
    [DisallowMultipleComponent]
    public sealed class FoundationSeedSessionLogRuntime : MonoBehaviour
    {
        private const string ConfigResourcePath = "FoundationSeedLoggingConfig";

        private static FoundationSeedSessionLogRuntime instance;

        private FoundationSeedLoggingConfig config;
        private StreamWriter jsonWriter;
        private StreamWriter transcriptWriter;
        private bool effectiveEnabled;

        public static bool IsEnabled => instance != null && instance.effectiveEnabled;
        public static string LatestSessionDirectory { get; private set; } = string.Empty;
        public static string LatestJsonlPath { get; private set; } = string.Empty;
        public static string LatestTranscriptPath { get; private set; } = string.Empty;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureRuntime() => EnsureInstance();

        public static FoundationSeedSessionLogRuntime EnsureInstance()
        {
            if (instance != null) return instance;
            GameObject root = new GameObject("FoundationSeedSessionLogRuntime");
            return root.AddComponent<FoundationSeedSessionLogRuntime>();
        }

        public static void Trace(string category, string eventName, string message, UnityEngine.Object context = null)
        {
            EnsureInstance();
            instance?.WriteEvent("trace", category, eventName, message, context);
        }

        public static void Feed(string category, string message, UnityEngine.Object context = null)
        {
            EnsureInstance();
            instance?.WriteEvent("feed", category, "Recent", message, context);
        }

        public static void SetEnabled(bool enabled, string reason = "")
        {
            EnsureInstance();
            instance?.ApplyEffectiveEnabled(enabled, reason);
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            config = Resources.Load<FoundationSeedLoggingConfig>(ConfigResourcePath);
            SceneManager.sceneLoaded += HandleSceneLoaded;
            Application.logMessageReceived += HandleUnityLog;
            ApplyEffectiveEnabled(ResolveDefaultEnabled(), "startup");
        }

        private void OnDestroy()
        {
            if (instance != this) return;
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            Application.logMessageReceived -= HandleUnityLog;
            EndSession();
            instance = null;
        }

        private bool ResolveDefaultEnabled()
        {
            if (config == null)
            {
#if UNITY_EDITOR
                return true;
#else
                return Debug.isDebugBuild;
#endif
            }

#if UNITY_EDITOR
            return config.enableInEditor;
#else
            return Debug.isDebugBuild ? config.enableInDevelopmentBuild : config.enableInReleaseBuild;
#endif
        }

        private void ApplyEffectiveEnabled(bool enabled, string reason)
        {
            effectiveEnabled = enabled;
            if (!enabled)
            {
                EndSession();
                return;
            }

            BeginSessionIfNeeded();
            WriteEvent("session", "Session", "Enabled", string.IsNullOrWhiteSpace(reason) ? "Session logging enabled." : reason, this);
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode) => WriteEvent("trace", "Scene", "Loaded", scene.path, this);

        private void HandleUnityLog(string condition, string stackTrace, LogType type)
        {
            bool capture = config == null || config.captureUnityWarningsAndErrors;
            if (!capture || !effectiveEnabled || type == LogType.Log) return;

            string message = string.IsNullOrWhiteSpace(stackTrace) ? condition : condition + "\n" + stackTrace;
            WriteEvent("unity", "Unity", type.ToString(), message, null);
        }

        private void BeginSessionIfNeeded()
        {
            if (!effectiveEnabled || jsonWriter != null || transcriptWriter != null) return;

            string sessionRoot = ResolveSessionRoot();
            Directory.CreateDirectory(sessionRoot);
            TrimOldSessions(sessionRoot);

            string stem = "session_" + DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff", CultureInfo.InvariantCulture);
            LatestSessionDirectory = sessionRoot;
            LatestJsonlPath = Path.Combine(sessionRoot, stem + ".jsonl");
            LatestTranscriptPath = Path.Combine(sessionRoot, stem + ".log");

            if (config == null || config.writeStructuredJsonl)
                jsonWriter = new StreamWriter(LatestJsonlPath, false, Encoding.UTF8) { AutoFlush = true };

            if (config == null || config.writeReadableTranscript)
                transcriptWriter = new StreamWriter(LatestTranscriptPath, false, Encoding.UTF8) { AutoFlush = true };

            TrimOldSessions(sessionRoot);
        }

        private string ResolveSessionRoot()
        {
#if UNITY_EDITOR
            if (config == null || config.writeToProjectFolderInEditor)
            {
                string projectRoot = Directory.GetParent(Application.dataPath).FullName;
                string folderName = config != null && !string.IsNullOrWhiteSpace(config.projectSessionFolderName)
                    ? config.projectSessionFolderName.Trim()
                    : "PlantLogs/SessionLogs";
                return Path.Combine(projectRoot, folderName);
            }
#endif
            string runtimeFolder = config != null && !string.IsNullOrWhiteSpace(config.runtimeSessionFolderName)
                ? config.runtimeSessionFolderName.Trim()
                : "PlantLogs/SessionLogs";
            return Path.Combine(Application.persistentDataPath, runtimeFolder);
        }

        private void EndSession()
        {
            DisposeWriter(jsonWriter);
            DisposeWriter(transcriptWriter);
            jsonWriter = null;
            transcriptWriter = null;
        }

        private void WriteEvent(string channel, string category, string eventName, string message, UnityEngine.Object context)
        {
            if (!effectiveEnabled) return;

            BeginSessionIfNeeded();
            if (jsonWriter == null && transcriptWriter == null) return;

            string utc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
            string contextName = context != null ? context.name : string.Empty;
            string sceneName = context != null && context is Component component && component.gameObject.scene.IsValid()
                ? component.gameObject.scene.name
                : string.Empty;

            if (jsonWriter != null)
            {
                jsonWriter.WriteLine(
                    "{\"utc\":\"" + Escape(utc) +
                    "\",\"channel\":\"" + Escape(channel) +
                    "\",\"category\":\"" + Escape(category) +
                    "\",\"event\":\"" + Escape(eventName) +
                    "\",\"scene\":\"" + Escape(sceneName) +
                    "\",\"context\":\"" + Escape(contextName) +
                    "\",\"message\":\"" + Escape(message) + "\"}");
            }

            if (transcriptWriter != null)
            {
                transcriptWriter.WriteLine(
                    "[" + utc + "]" +
                    " [" + Sanitize(channel) + "]" +
                    " [" + Sanitize(category) + "/" + Sanitize(eventName) + "]" +
                    (string.IsNullOrWhiteSpace(sceneName) ? string.Empty : " [scene:" + sceneName + "]") +
                    (string.IsNullOrWhiteSpace(contextName) ? string.Empty : " [ctx:" + contextName + "]") +
                    " " + (message ?? string.Empty));
            }
        }

        private void TrimOldSessions(string sessionRoot)
        {
            if (string.IsNullOrWhiteSpace(sessionRoot) || !Directory.Exists(sessionRoot)) return;

            int retentionCount = config != null && config.maxRetainedSessions > 0 ? config.maxRetainedSessions : 10;
            FileInfo[] sessionFiles = new DirectoryInfo(sessionRoot)
                .GetFiles("session_*.*", SearchOption.TopDirectoryOnly)
                .Where(file =>
                    string.Equals(file.Extension, ".jsonl", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(file.Extension, ".log", StringComparison.OrdinalIgnoreCase))
                .ToArray();

            var sessionsByStem = sessionFiles
                .GroupBy(file => Path.GetFileNameWithoutExtension(file.Name), StringComparer.OrdinalIgnoreCase)
                .Select(group => new
                {
                    Files = group.ToArray(),
                    LastWriteUtc = group.Max(file => file.LastWriteTimeUtc),
                    CreationUtc = group.Max(file => file.CreationTimeUtc)
                })
                .OrderByDescending(entry => entry.CreationUtc)
                .ThenByDescending(entry => entry.LastWriteUtc)
                .ToArray();

            for (int i = retentionCount; i < sessionsByStem.Length; i += 1)
            {
                for (int j = 0; j < sessionsByStem[i].Files.Length; j += 1)
                {
                    try { sessionsByStem[i].Files[j].Delete(); }
                    catch (Exception exception)
                    {
                        Debug.LogWarning("[FoundationSeedSessionLogRuntime] Failed to trim file " + sessionsByStem[i].Files[j].FullName + ". " + exception.Message, this);
                    }
                }
            }
        }

        private static void DisposeWriter(StreamWriter writer)
        {
            if (writer == null) return;
            writer.Flush();
            writer.Dispose();
        }

        private static string Escape(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n");
        }

        private static string Sanitize(string value) => string.IsNullOrWhiteSpace(value) ? "none" : value.Trim();
    }
}
