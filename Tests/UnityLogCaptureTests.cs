// Copyright 2026 Spellbound Studio Inc.

using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Spellbound.Core.Logging;
using UnityEngine;
using UnityEngine.TestTools;

namespace Spellbound.Core.Tests {
    public class UnityLogCaptureTests {
        private RecordingLogSink _recording;
        private Log.SinkSuspension _suspension;

        [SetUp]
        public void SetUp() {
            _suspension = Log.SuspendSinks();
            _recording = new RecordingLogSink(LogLevel.Verbose);
            Log.AddSink(_recording, null, LogLevel.Verbose);
        }

        [TearDown]
        public void TearDown() {
            UnityLogCapture.Stop();
            Log.RemoveSink(_recording);
            _suspension.Dispose();
        }

        private string[] Messages(LogLevel level) =>
                _recording.Entries.Where(entry => entry.Level == level).Select(entry => entry.Message).ToArray();

        [Test]
        public void UnityErrorArrivesOnceWithItsStackTrace() {
            UnityLogCapture.Start(LogLevel.Verbose);
            LogAssert.Expect(LogType.Error, "boom");

            Debug.LogError("boom");

            var errors = Messages(LogLevel.Error);

            Assert.AreEqual(1, errors.Length);
            Assert.IsTrue(errors[0].StartsWith("boom"));
            Assert.AreEqual("Unity", _recording.Entries[0].Source);
            Assert.AreEqual(nameof(LogType.Error), _recording.Entries[0].Member);
        }

        [Test]
        public void ExceptionArrivesAsAnError() {
            UnityLogCapture.Start(LogLevel.Verbose);
            LogAssert.Expect(LogType.Exception, new Regex("InvalidOperationException: broken"));

            Debug.LogException(new System.InvalidOperationException("broken"));

            var errors = Messages(LogLevel.Error);

            Assert.AreEqual(1, errors.Length);
            Assert.IsTrue(errors[0].Contains("broken"));
            Assert.AreEqual(nameof(LogType.Exception), _recording.Entries[0].Member);
        }

        [Test]
        public void SpellboundErrorEchoedToTheUnityConsoleIsNotCapturedTwice() {
            var console = new UnityConsoleSink();
            Log.AddSink(console, null, LogLevel.Verbose);

            try {
                UnityLogCapture.Start(LogLevel.Verbose);
                LogAssert.Expect(LogType.Error, new Regex("echo"));

                Log.Emit(LogLevel.Error, "echo", "UnityLogCaptureTests.cs", nameof(SpellboundErrorEchoedToTheUnityConsoleIsNotCapturedTwice), 0);

                var errors = Messages(LogLevel.Error);

                Assert.AreEqual(1, errors.Length);
                Assert.AreEqual("echo", errors[0]);
                Assert.AreEqual("UnityLogCaptureTests", _recording.Entries[0].Source);
            }
            finally {
                Log.RemoveSink(console);
            }
        }

        [Test]
        public void MessagesBelowTheMinimumLevelAreDropped() {
            UnityLogCapture.Start(LogLevel.Warning);
            LogAssert.Expect(LogType.Warning, "loud");

            Debug.Log("quiet");
            Debug.LogWarning("loud");

            Assert.AreEqual(0, Messages(LogLevel.Info).Length);
            CollectionAssert.AreEqual(new[] { "loud" }, Messages(LogLevel.Warning).Select(message => message.Split('\n')[0]));
        }

        [Test]
        public void StartAgainOnlyChangesTheMinimumLevel() {
            UnityLogCapture.Start(LogLevel.Error);
            UnityLogCapture.Start(LogLevel.Verbose);
            LogAssert.Expect(LogType.Warning, "once");

            Debug.LogWarning("once");

            Assert.AreEqual(LogLevel.Verbose, UnityLogCapture.MinimumLevel);
            Assert.AreEqual(1, Messages(LogLevel.Warning).Length);
        }

        [Test]
        public void StoppedCaptureReceivesNothing() {
            UnityLogCapture.Start(LogLevel.Verbose);
            UnityLogCapture.Stop();
            LogAssert.Expect(LogType.Warning, "silent");

            Debug.LogWarning("silent");

            Assert.IsFalse(UnityLogCapture.IsStarted);
            Assert.AreEqual(0, _recording.Count);
        }

        [Test]
        public void LogTypesMapToLevels() {
            Assert.AreEqual(LogLevel.Info, UnityLogCapture.ToLogLevel(LogType.Log));
            Assert.AreEqual(LogLevel.Warning, UnityLogCapture.ToLogLevel(LogType.Warning));
            Assert.AreEqual(LogLevel.Error, UnityLogCapture.ToLogLevel(LogType.Error));
            Assert.AreEqual(LogLevel.Error, UnityLogCapture.ToLogLevel(LogType.Assert));
            Assert.AreEqual(LogLevel.Error, UnityLogCapture.ToLogLevel(LogType.Exception));
        }
    }
}
