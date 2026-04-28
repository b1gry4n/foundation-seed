using System;
using System.Collections.Generic;
using System.IO;
using FoundationSeed.Diagnostics;
using FoundationSeed.Time;
using UnityEngine;

namespace FoundationSeed.Save
{
    public interface IFoundationSeedSaveProvider
    {
        string ProviderId { get; }
        string CaptureStateJson();
        void RestoreStateJson(string stateJson);
    }

    [DisallowMultipleComponent]
    public sealed class FoundationSeedSaveService : MonoBehaviour
    {
        [Serializable]
        private sealed class SaveEntry
        {
            public string providerId;
            public string payloadJson;
        }

        [Serializable]
        private sealed class SaveEnvelope
        {
            public string utc;
            public string activeScene;
            public float gameplayTime;
            public List<SaveEntry> entries = new List<SaveEntry>();
        }

        private static FoundationSeedSaveService instance;
        private readonly Dictionary<string, IFoundationSeedSaveProvider> providers =
            new Dictionary<string, IFoundationSeedSaveProvider>(StringComparer.OrdinalIgnoreCase);

        public static FoundationSeedSaveService Instance => instance;

        public static void EnsureInstance()
        {
            if (instance != null) return;
            GameObject go = new GameObject("FoundationSeedSaveService");
            go.AddComponent<FoundationSeedSaveService>();
        }

        public static bool RegisterProvider(IFoundationSeedSaveProvider provider)
        {
            EnsureInstance();
            if (instance == null || provider == null || string.IsNullOrWhiteSpace(provider.ProviderId)) return false;

            instance.providers[provider.ProviderId.Trim()] = provider;
            return true;
        }

        public static void UnregisterProvider(string providerId)
        {
            if (instance == null || string.IsNullOrWhiteSpace(providerId)) return;
            instance.providers.Remove(providerId.Trim());
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
            FoundationSeedSessionLogRuntime.Trace("Save", "ServiceReady", "Foundation save service ready.", this);
        }

        public bool SaveAll()
        {
            SaveEnvelope envelope = new SaveEnvelope
            {
                utc = DateTime.UtcNow.ToString("o"),
                activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path,
                gameplayTime = FoundationSeedGameTime.GameplayTime
            };

            foreach (KeyValuePair<string, IFoundationSeedSaveProvider> pair in providers)
            {
                envelope.entries.Add(new SaveEntry
                {
                    providerId = pair.Key,
                    payloadJson = pair.Value.CaptureStateJson() ?? string.Empty
                });
            }

            string json = JsonUtility.ToJson(envelope, true);
            File.WriteAllText(GetSavePath(), json);
            FoundationSeedSessionLogRuntime.Trace("Save", "Saved", "Saved " + envelope.entries.Count + " provider payload(s).", this);
            return true;
        }

        public bool LoadAll()
        {
            string savePath = GetSavePath();
            if (!File.Exists(savePath)) return false;

            string json = File.ReadAllText(savePath);
            SaveEnvelope envelope = JsonUtility.FromJson<SaveEnvelope>(json);
            if (envelope == null || envelope.entries == null) return false;

            int restoredCount = 0;
            for (int i = 0; i < envelope.entries.Count; i += 1)
            {
                SaveEntry entry = envelope.entries[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.providerId)) continue;

                if (!providers.TryGetValue(entry.providerId.Trim(), out IFoundationSeedSaveProvider provider) || provider == null) continue;

                provider.RestoreStateJson(entry.payloadJson ?? string.Empty);
                restoredCount += 1;
            }

            FoundationSeedSessionLogRuntime.Trace("Save", "Loaded", "Restored " + restoredCount + " provider payload(s).", this);
            return true;
        }

        public static string GetSavePath() => Path.Combine(Application.persistentDataPath, "foundation_seed_save.json");
    }
}
