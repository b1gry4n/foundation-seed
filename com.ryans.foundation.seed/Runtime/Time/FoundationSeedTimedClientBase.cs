using System;

namespace FoundationSeed.Time
{
    public abstract class FoundationSeedTimedClientBase : IFoundationSeedTimedClient, IDisposable
    {
        private bool isRegistered;

        public virtual int TimedExecutionOrder => 0;
        public virtual bool IsTimeClientEnabled => true;

        protected FoundationSeedTimedClientBase(bool autoRegister = true)
        {
            if (autoRegister)
            {
                RegisterTimedClient();
            }
        }

        public void RegisterTimedClient()
        {
            if (isRegistered)
            {
                return;
            }

            FoundationSeedTimedRuntimeDriver.EnsureInstance().RegisterClient(this);
            isRegistered = true;
            OnTimedClientRegistered();
        }

        public void UnregisterTimedClient()
        {
            if (!isRegistered)
            {
                return;
            }

            FoundationSeedTimedRuntimeDriver.Instance?.UnregisterClient(this);
            isRegistered = false;
            OnTimedClientUnregistered();
        }

        void IDisposable.Dispose()
        {
            UnregisterTimedClient();
        }

        protected virtual void OnTimedClientRegistered()
        {
        }

        protected virtual void OnTimedClientUnregistered()
        {
        }

        public virtual void OnPresentationUpdate(float presentationDeltaTime, float smoothedPresentationDeltaTime)
        {
        }

        public virtual void OnGameplayUpdate(float gameplayDeltaTime)
        {
        }

        public virtual void OnGameplayFixedUpdate(float gameplayFixedDeltaTime)
        {
        }

        public virtual void OnPresentationLateUpdate(float presentationDeltaTime, float smoothedPresentationDeltaTime)
        {
        }
    }
}
