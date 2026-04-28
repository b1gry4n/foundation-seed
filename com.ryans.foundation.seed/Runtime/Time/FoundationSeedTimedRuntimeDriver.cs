using System.Collections.Generic;
using FoundationSeed.Diagnostics;
using UnityEngine;

namespace FoundationSeed.Time
{
    [DisallowMultipleComponent]
    public sealed class FoundationSeedTimedRuntimeDriver : MonoBehaviour
    {
        private static FoundationSeedTimedRuntimeDriver instance;
        private readonly List<FoundationSeedTimedBehaviour> behaviours = new List<FoundationSeedTimedBehaviour>();
        private readonly List<IFoundationSeedTimedClient> clients = new List<IFoundationSeedTimedClient>();

        public static FoundationSeedTimedRuntimeDriver Instance => instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureRuntime()
        {
            EnsureInstance();
        }

        public static FoundationSeedTimedRuntimeDriver EnsureInstance()
        {
            if (instance != null)
            {
                return instance;
            }

            FoundationSeedGameTime.EnsureInstance();
            GameObject root = new GameObject("FoundationSeedTimedRuntimeDriver");
            return root.AddComponent<FoundationSeedTimedRuntimeDriver>();
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
            FoundationSeedGameTime.EnsureInstance();
            FoundationSeedSessionLogRuntime.Trace("Time", "TimedDriverReady", "Foundation timed runtime driver ready.", this);
        }

        public void Register(FoundationSeedTimedBehaviour behaviour)
        {
            if (behaviour == null || behaviours.Contains(behaviour))
            {
                return;
            }

            behaviours.Add(behaviour);
            behaviours.Sort(CompareBehaviours);
        }

        public void Unregister(FoundationSeedTimedBehaviour behaviour)
        {
            if (behaviour == null)
            {
                return;
            }

            behaviours.Remove(behaviour);
        }

        public void RegisterClient(IFoundationSeedTimedClient client)
        {
            if (client == null || clients.Contains(client))
            {
                return;
            }

            clients.Add(client);
            clients.Sort(CompareClients);
        }

        public void UnregisterClient(IFoundationSeedTimedClient client)
        {
            if (client == null)
            {
                return;
            }

            clients.Remove(client);
        }

        private void Update()
        {
            FoundationSeedPresentationTime.AdvanceFrame(UnityEngine.Time.unscaledDeltaTime);
            float presentationDeltaTime = FoundationSeedPresentationTime.PresentationDeltaTime;
            float smoothedPresentationDeltaTime = FoundationSeedPresentationTime.SmoothedPresentationDeltaTime;
            float gameplayDeltaTime = FoundationSeedPresentationTime.GameplayDeltaTime;

            for (int i = 0; i < behaviours.Count; i += 1)
            {
                FoundationSeedTimedBehaviour behaviour = behaviours[i];
                if (behaviour == null)
                {
                    behaviours.RemoveAt(i);
                    i -= 1;
                    continue;
                }

                behaviour.InvokePresentationUpdate(presentationDeltaTime, smoothedPresentationDeltaTime);
                behaviour.InvokeGameplayUpdate(gameplayDeltaTime);
            }

            for (int i = 0; i < clients.Count; i += 1)
            {
                IFoundationSeedTimedClient client = clients[i];
                if (client == null)
                {
                    clients.RemoveAt(i);
                    i -= 1;
                    continue;
                }

                if (!client.IsTimeClientEnabled)
                {
                    continue;
                }

                client.OnPresentationUpdate(presentationDeltaTime, smoothedPresentationDeltaTime);
                client.OnGameplayUpdate(gameplayDeltaTime);
            }
        }

        private void FixedUpdate()
        {
            FoundationSeedPresentationTime.AdvanceFixedStep(UnityEngine.Time.fixedUnscaledDeltaTime);
            float gameplayFixedDeltaTime = FoundationSeedPresentationTime.GameplayFixedDeltaTime;

            for (int i = 0; i < behaviours.Count; i += 1)
            {
                FoundationSeedTimedBehaviour behaviour = behaviours[i];
                if (behaviour == null)
                {
                    behaviours.RemoveAt(i);
                    i -= 1;
                    continue;
                }

                behaviour.InvokeGameplayFixedUpdate(gameplayFixedDeltaTime);
            }

            for (int i = 0; i < clients.Count; i += 1)
            {
                IFoundationSeedTimedClient client = clients[i];
                if (client == null)
                {
                    clients.RemoveAt(i);
                    i -= 1;
                    continue;
                }

                if (!client.IsTimeClientEnabled)
                {
                    continue;
                }

                client.OnGameplayFixedUpdate(gameplayFixedDeltaTime);
            }
        }

        private void LateUpdate()
        {
            float presentationDeltaTime = FoundationSeedPresentationTime.PresentationDeltaTime;
            float smoothedPresentationDeltaTime = FoundationSeedPresentationTime.SmoothedPresentationDeltaTime;

            for (int i = 0; i < behaviours.Count; i += 1)
            {
                FoundationSeedTimedBehaviour behaviour = behaviours[i];
                if (behaviour == null)
                {
                    behaviours.RemoveAt(i);
                    i -= 1;
                    continue;
                }

                behaviour.InvokePresentationLateUpdate(presentationDeltaTime, smoothedPresentationDeltaTime);
            }

            for (int i = 0; i < clients.Count; i += 1)
            {
                IFoundationSeedTimedClient client = clients[i];
                if (client == null)
                {
                    clients.RemoveAt(i);
                    i -= 1;
                    continue;
                }

                if (!client.IsTimeClientEnabled)
                {
                    continue;
                }

                client.OnPresentationLateUpdate(presentationDeltaTime, smoothedPresentationDeltaTime);
            }
        }

        private static int CompareBehaviours(FoundationSeedTimedBehaviour left, FoundationSeedTimedBehaviour right)
        {
            if (ReferenceEquals(left, right)) return 0;
            if (left == null) return 1;
            if (right == null) return -1;

            int orderComparison = left.TimedExecutionOrder.CompareTo(right.TimedExecutionOrder);
            return orderComparison != 0 ? orderComparison : left.GetInstanceID().CompareTo(right.GetInstanceID());
        }

        private static int CompareClients(IFoundationSeedTimedClient left, IFoundationSeedTimedClient right)
        {
            if (ReferenceEquals(left, right)) return 0;
            if (left == null) return 1;
            if (right == null) return -1;

            int orderComparison = left.TimedExecutionOrder.CompareTo(right.TimedExecutionOrder);
            if (orderComparison != 0)
            {
                return orderComparison;
            }

            return left.GetHashCode().CompareTo(right.GetHashCode());
        }
    }
}
