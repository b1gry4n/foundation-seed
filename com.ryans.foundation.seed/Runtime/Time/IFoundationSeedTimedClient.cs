namespace FoundationSeed.Time
{
    public interface IFoundationSeedTimedClient
    {
        int TimedExecutionOrder { get; }
        bool IsTimeClientEnabled { get; }
        void OnPresentationUpdate(float presentationDeltaTime, float smoothedPresentationDeltaTime);
        void OnGameplayUpdate(float gameplayDeltaTime);
        void OnGameplayFixedUpdate(float gameplayFixedDeltaTime);
        void OnPresentationLateUpdate(float presentationDeltaTime, float smoothedPresentationDeltaTime);
    }
}
