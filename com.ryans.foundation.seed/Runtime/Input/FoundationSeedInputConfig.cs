using UnityEngine;

namespace FoundationSeed.Input
{
    public enum FoundationSeedConsoleToggleInputMode
    {
        AutoDetect = 0,
        InputSystemOnly = 1,
        LegacyInputOnly = 2
    }

    [CreateAssetMenu(menuName = "Foundation Seed/Input Config", fileName = "FoundationSeedInputConfig")]
    public sealed class FoundationSeedInputConfig : ScriptableObject
    {
        public bool enableDefaultConsoleToggle = true;
        public FoundationSeedConsoleToggleInputMode consoleToggleInputMode = FoundationSeedConsoleToggleInputMode.AutoDetect;
        public KeyCode legacyConsoleToggleKey = KeyCode.BackQuote;
    }
}
