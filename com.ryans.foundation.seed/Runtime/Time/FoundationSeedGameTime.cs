using System.Collections.Generic;
using FoundationSeed.Diagnostics;
using UnityEngine;

namespace FoundationSeed.Time
{
    [DisallowMultipleComponent]
    public sealed class FoundationSeedGameTime : MonoBehaviour
    {
        private static FoundationSeedGameTime instance;
        private readonly HashSet<string> pauseReasons = new HashSet<string>();

        public static FoundationSeedGameTime Instance => instance;
        public static float GameplayTime { get; private set; }
        public static float GameplayDeltaTime { get; private set; }
        public static bool IsPaused => instance != null && instance.pauseReasons.Count > 0;

        public static void EnsureInstance()
        {
            if (instance != null)
            {
                return;
            }

            GameObject go = new GameObject("FoundationSeedGameTime");
            go.AddComponent<FoundationSeedGameTime>();
        }

        public static void SetPaused(bool paused, string reason)
        {
            EnsureInstance();

            string resolvedReason = string.IsNullOrWhiteSpace(reason) ? "UnnamedPauseReason" : reason.Trim();
            if (paused)
            {
                instance.pauseReasons.Add(resolvedReason);
            }
            else
            {
                instance.pauseReasons.Remove(resolvedReason);
            }
        }

        public static void ResetGameplayClock()
        {
            GameplayTime = 0f;
            GameplayDeltaTime = 0f;
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
            FoundationSeedSessionLogRuntime.Trace("Time", "ServiceReady", "Foundation game time service ready.", this);
        }

        private void Update()
        {
            if (pauseReasons.Count > 0)
            {
                GameplayDeltaTime = 0f;
                return;
            }

            GameplayDeltaTime = UnityEngine.Time.unscaledDeltaTime;
            GameplayTime += GameplayDeltaTime;
        }
    }
}
