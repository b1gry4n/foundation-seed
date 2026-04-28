using System;
using System.Reflection;
using FoundationSeed.Development;
using FoundationSeed.Diagnostics;
using FoundationSeed.Input;
using FoundationSeed.Time;
using UnityEngine;

namespace FoundationSeed.DevConsole
{
    [DisallowMultipleComponent]
    public sealed class FoundationSeedConsoleToggleBridge : FoundationSeedTimedBehaviour
    {
        private const string InputConfigResourcePath = "FoundationSeedInputConfig";

        private static FoundationSeedConsoleToggleBridge instance;
        private static bool attemptedInputSystemBind;
        private static PropertyInfo keyboardCurrentProperty;
        private static PropertyInfo keyboardBackquoteProperty;
        private static PropertyInfo keyControlPressedProperty;
        private static FoundationSeedInputConfig cachedInputConfig;
        private static bool attemptedInputConfigLoad;

        public static bool EnableDefaultBackquoteToggle = true;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureRuntime()
        {
            if (instance != null)
            {
                return;
            }

            GameObject root = new GameObject("FoundationSeedConsoleToggleBridge");
            root.AddComponent<FoundationSeedConsoleToggleBridge>();
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
            FoundationSeedDeveloperConsole.EnsureInstance();
            FoundationSeedSessionLogRuntime.Trace("DevConsole", "ToggleBridgeReady", "Foundation console toggle bridge ready.", this);
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        protected override void OnPresentationUpdate(float presentationDeltaTime, float smoothedPresentationDeltaTime)
        {
            if (!EnableDefaultBackquoteToggle || !FoundationSeedDevelopmentGate.IsDevelopmentRuntime)
            {
                return;
            }

            FoundationSeedInputConfig inputConfig = GetInputConfig();
            if (inputConfig != null && !inputConfig.enableDefaultConsoleToggle)
            {
                return;
            }

            if (WasTogglePressed(inputConfig))
            {
                FoundationSeedDeveloperConsole.ToggleConsole();
            }
        }

        private static bool WasTogglePressed(FoundationSeedInputConfig inputConfig)
        {
            FoundationSeedConsoleToggleInputMode mode = inputConfig != null
                ? inputConfig.consoleToggleInputMode
                : FoundationSeedConsoleToggleInputMode.AutoDetect;

            switch (mode)
            {
                case FoundationSeedConsoleToggleInputMode.InputSystemOnly:
                    return TryReadInputSystemBackquotePressed(out bool inputSystemPressed) && inputSystemPressed;

                case FoundationSeedConsoleToggleInputMode.LegacyInputOnly:
                    return UnityEngine.Input.GetKeyDown(inputConfig != null ? inputConfig.legacyConsoleToggleKey : KeyCode.BackQuote);

                default:
                    if (TryReadInputSystemBackquotePressed(out bool autoPressed))
                    {
                        return autoPressed;
                    }

                    return UnityEngine.Input.GetKeyDown(inputConfig != null ? inputConfig.legacyConsoleToggleKey : KeyCode.BackQuote);
            }
        }

        private static FoundationSeedInputConfig GetInputConfig()
        {
            if (attemptedInputConfigLoad)
            {
                return cachedInputConfig;
            }

            attemptedInputConfigLoad = true;
            cachedInputConfig = Resources.Load<FoundationSeedInputConfig>(InputConfigResourcePath);
            return cachedInputConfig;
        }

        private static bool TryReadInputSystemBackquotePressed(out bool pressed)
        {
            pressed = false;
            if (!TryBindInputSystemReflection())
            {
                return false;
            }

            object keyboard = keyboardCurrentProperty.GetValue(null);
            if (keyboard == null)
            {
                return true;
            }

            object backquoteKey = keyboardBackquoteProperty.GetValue(keyboard);
            if (backquoteKey == null)
            {
                return true;
            }

            object pressedValue = keyControlPressedProperty.GetValue(backquoteKey);
            if (pressedValue is bool boolValue)
            {
                pressed = boolValue;
            }

            return true;
        }

        private static bool TryBindInputSystemReflection()
        {
            if (attemptedInputSystemBind)
            {
                return keyboardCurrentProperty != null
                    && keyboardBackquoteProperty != null
                    && keyControlPressedProperty != null;
            }

            attemptedInputSystemBind = true;

            const string keyboardTypeName = "UnityEngine.InputSystem.Keyboard, Unity.InputSystem";
            const string keyControlTypeName = "UnityEngine.InputSystem.Controls.KeyControl, Unity.InputSystem";

            Type keyboardType = Type.GetType(keyboardTypeName, throwOnError: false);
            Type keyControlType = Type.GetType(keyControlTypeName, throwOnError: false);
            if (keyboardType == null || keyControlType == null)
            {
                return false;
            }

            keyboardCurrentProperty = keyboardType.GetProperty("current", BindingFlags.Public | BindingFlags.Static);
            keyboardBackquoteProperty = keyboardType.GetProperty("backquoteKey", BindingFlags.Public | BindingFlags.Instance);
            keyControlPressedProperty = keyControlType.GetProperty("wasPressedThisFrame", BindingFlags.Public | BindingFlags.Instance);

            return keyboardCurrentProperty != null
                && keyboardBackquoteProperty != null
                && keyControlPressedProperty != null;
        }
    }
}
