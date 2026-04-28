using UnityEngine;

namespace FoundationSeed.Development
{
    public static class FoundationSeedDevelopmentGate
    {
        public static bool IsDevelopmentRuntime => Debug.isDebugBuild || Application.isEditor;
    }
}
