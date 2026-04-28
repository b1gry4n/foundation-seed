using FoundationSeed.Diagnostics;
using FoundationSeed.Input;
using FoundationSeed.Time;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections;

namespace FoundationSeed.Tests.Runtime
{
    public class FoundationSeedRuntimeSmokeTests
    {
        [UnityTest]
        public IEnumerator Services_Can_Be_Ensured()
        {
            FoundationSeedGameTime.EnsureInstance();
            FoundationSeedTimedRuntimeDriver.EnsureInstance();
            FoundationSeedSessionLogRuntime.EnsureInstance();
            FoundationSeedInputRouter.EnsureInstance();

            yield return null;

            Assert.IsNotNull(FoundationSeedGameTime.Instance);
            Assert.IsNotNull(FoundationSeedTimedRuntimeDriver.Instance);
            Assert.IsNotNull(FoundationSeedInputRouter.Instance);
        }

        [Test]
        public void LoggingConfig_DefaultRetention_IsBounded()
        {
            FoundationSeedLoggingConfig config = ScriptableObject.CreateInstance<FoundationSeedLoggingConfig>();
            Assert.GreaterOrEqual(config.maxRetainedSessions, 1);
        }
    }
}
