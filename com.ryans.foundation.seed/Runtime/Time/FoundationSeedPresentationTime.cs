using UnityEngine;

namespace FoundationSeed.Time
{
    public static class FoundationSeedPresentationTime
    {
        private const float DefaultSmoothing = 0.18f;

        public static float GameplayTime { get; private set; }
        public static float GameplayDeltaTime { get; private set; }
        public static float GameplayFixedDeltaTime { get; private set; }
        public static float PresentationTime { get; private set; }
        public static float PresentationDeltaTime { get; private set; }
        public static float PresentationFixedDeltaTime { get; private set; }
        public static float SmoothedPresentationDeltaTime { get; private set; }

        internal static void AdvanceFrame(float presentationDeltaTime)
        {
            float resolvedPresentationDelta = Mathf.Max(0f, presentationDeltaTime);
            float resolvedGameplayDelta = FoundationSeedGameTime.Instance != null
                ? Mathf.Max(0f, FoundationSeedGameTime.GameplayDeltaTime)
                : resolvedPresentationDelta;

            PresentationDeltaTime = resolvedPresentationDelta;
            PresentationTime += resolvedPresentationDelta;
            GameplayDeltaTime = resolvedGameplayDelta;
            GameplayTime += resolvedGameplayDelta;
            SmoothedPresentationDeltaTime = SmoothedPresentationDeltaTime <= 0f
                ? resolvedPresentationDelta
                : Mathf.Lerp(SmoothedPresentationDeltaTime, resolvedPresentationDelta, DefaultSmoothing);
        }

        internal static void AdvanceFixedStep(float presentationFixedDeltaTime)
        {
            float resolvedPresentationFixedDelta = Mathf.Max(0f, presentationFixedDeltaTime);
            PresentationFixedDeltaTime = resolvedPresentationFixedDelta;
            GameplayFixedDeltaTime = FoundationSeedGameTime.IsPaused ? 0f : resolvedPresentationFixedDelta;
        }
    }
}
