using System;
using FoundationSeed.Diagnostics;
using UnityEngine;

namespace FoundationSeed.Input
{
    public readonly struct FoundationSeedInputAction
    {
        public readonly string ActionId;
        public readonly float Value;
        public readonly bool IsPressed;

        public FoundationSeedInputAction(string actionId, float value = 1f, bool isPressed = true)
        {
            ActionId = string.IsNullOrWhiteSpace(actionId) ? string.Empty : actionId.Trim();
            Value = value;
            IsPressed = isPressed;
        }
    }

    [DisallowMultipleComponent]
    public sealed class FoundationSeedInputRouter : MonoBehaviour
    {
        private static FoundationSeedInputRouter instance;

        public static FoundationSeedInputRouter Instance => instance;
        public event Action<FoundationSeedInputAction> ActionTriggered;

        public static void EnsureInstance()
        {
            if (instance != null) return;
            GameObject go = new GameObject("FoundationSeedInputRouter");
            go.AddComponent<FoundationSeedInputRouter>();
        }

        public static void RaiseAction(string actionId) => RaiseAction(new FoundationSeedInputAction(actionId));

        public static void RaiseAction(in FoundationSeedInputAction action)
        {
            EnsureInstance();
            instance?.ActionTriggered?.Invoke(action);
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
            FoundationSeedSessionLogRuntime.Trace("Input", "RouterReady", "Foundation input router ready.", this);
        }
    }
}
