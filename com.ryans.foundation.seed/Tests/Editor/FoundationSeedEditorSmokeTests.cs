using FoundationSeed.Diagnostics;
using NUnit.Framework;
using UnityEngine;

namespace FoundationSeed.Tests.Editor
{
    public class FoundationSeedEditorSmokeTests
    {
        [Test]
        public void LoggingConfig_CanBeCreated()
        {
            FoundationSeedLoggingConfig config = ScriptableObject.CreateInstance<FoundationSeedLoggingConfig>();
            Assert.IsNotNull(config);
            Assert.IsTrue(config.writeStructuredJsonl || config.writeReadableTranscript);
        }
    }
}
