using UnityEngine;

namespace FoundationSeed.Time
{
    public abstract class FoundationSeedTimedBehaviour : MonoBehaviour
    {
        internal virtual int TimedExecutionOrder => 0;

        protected virtual void OnTimedEnable() {}
        protected virtual void OnTimedDisable() {}
        protected virtual void OnPresentationUpdate(float presentationDeltaTime, float smoothedPresentationDeltaTime) {}
        protected virtual void OnGameplayUpdate(float gameplayDeltaTime) {}
        protected virtual void OnGameplayFixedUpdate(float gameplayFixedDeltaTime) {}
        protected virtual void OnPresentationLateUpdate(float presentationDeltaTime, float smoothedPresentationDeltaTime) {}

        private void OnEnable()
        {
            FoundationSeedTimedRuntimeDriver.EnsureInstance().Register(this);
            OnTimedEnable();
        }

        private void OnDisable()
        {
            FoundationSeedTimedRuntimeDriver.Instance?.Unregister(this);
            OnTimedDisable();
        }

        internal void InvokePresentationUpdate(float presentationDeltaTime, float smoothedPresentationDeltaTime) => OnPresentationUpdate(presentationDeltaTime, smoothedPresentationDeltaTime);
        internal void InvokeGameplayUpdate(float gameplayDeltaTime) => OnGameplayUpdate(gameplayDeltaTime);
        internal void InvokeGameplayFixedUpdate(float gameplayFixedDeltaTime) => OnGameplayFixedUpdate(gameplayFixedDeltaTime);
        internal void InvokePresentationLateUpdate(float presentationDeltaTime, float smoothedPresentationDeltaTime) => OnPresentationLateUpdate(presentationDeltaTime, smoothedPresentationDeltaTime);
    }
}
