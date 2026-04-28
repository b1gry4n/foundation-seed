using FoundationSeed.Diagnostics;
using FoundationSeed.DevConsole;
using FoundationSeed.Input;
using FoundationSeed.Save;
using FoundationSeed.Time;
using UnityEngine;

namespace FoundationSeed.Bootstrap
{
    [DisallowMultipleComponent]
    public sealed class FoundationSeedBootstrapRoot : MonoBehaviour
    {
        private static FoundationSeedBootstrapRoot instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void EnsureRuntime()
        {
            if (instance != null)
            {
                return;
            }

            GameObject runtimeRoot = new GameObject("FoundationSeedBootstrapRoot");
            runtimeRoot.AddComponent<FoundationSeedBootstrapRoot>();
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

            FoundationSeedSessionLogRuntime.EnsureInstance();
            FoundationSeedGameTime.EnsureInstance();
            FoundationSeedTimedRuntimeDriver.EnsureInstance();
            FoundationSeedInputRouter.EnsureInstance();
            FoundationSeedSaveService.EnsureInstance();
            FoundationSeedDeveloperConsole.EnsureInstance();
            FoundationSeedSessionLogRuntime.Trace("Foundation", "BootstrapReady", "Foundation bootstrap initialized core services.", this);
        }
    }
}
