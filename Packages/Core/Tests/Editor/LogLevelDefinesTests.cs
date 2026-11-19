// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Diagnostics;
using System.Reflection;
using NUnit.Framework;
using Spellbound.Core.Logging;
using Spellbound.Core.Logging.Editor;

namespace Spellbound.Core.Tests {
    public class LogLevelDefinesTests {
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
            Log.RemoveSink(_recording);
            _suspension.Dispose();
        }

        [TestCase(LogLevel.Verbose,
            new[] {
                LogLevelDefines.VerboseDefine, LogLevelDefines.DebugDefine, LogLevelDefines.InfoDefine,
                LogLevelDefines.WarningDefine
            })]
        [TestCase(LogLevel.Debug,
            new[] { LogLevelDefines.DebugDefine, LogLevelDefines.InfoDefine, LogLevelDefines.WarningDefine })]
        [TestCase(LogLevel.Info, new[] { LogLevelDefines.InfoDefine, LogLevelDefines.WarningDefine })]
        [TestCase(LogLevel.Warning, new[] { LogLevelDefines.WarningDefine })]
        [TestCase(LogLevel.Error, new string[0])]
        [TestCase(LogLevel.None, new string[0])]
        public void EachLevelCompilesItselfAndEveryLevelAbove(LogLevel level, string[] expected) {
            CollectionAssert.AreEqual(expected, LogLevelDefines.For(level));
            Assert.AreEqual(0, _recording.Count);
        }

        [Test]
        public void AnUnknownLevelAddsNothingAndLogsOneError() {
            Assert.AreEqual(0, LogLevelDefines.For((LogLevel)42).Count);
            Assert.AreEqual(1, _recording.CountOf(LogLevel.Error));
            Assert.IsTrue(_recording.Contains(LogLevel.Error, "42"));
        }

        [Test]
        public void EveryDefineIsTheOneTheLoggerWaitsFor() {
            Assert.AreEqual("SPELLBOUND_LOG_", LogLevelDefines.Prefix);
            Assert.AreEqual(LogLevelDefines.VerboseDefine, ConditionOf(nameof(Log.Verbose)));
            Assert.AreEqual(LogLevelDefines.DebugDefine, ConditionOf(nameof(Log.Debug)));
            Assert.AreEqual(LogLevelDefines.InfoDefine, ConditionOf(nameof(Log.Info)));
            Assert.AreEqual(LogLevelDefines.WarningDefine, ConditionOf(nameof(Log.Warn)));
            Assert.IsNull(ConditionOf(nameof(Log.Error)));

            foreach (LogLevel level in Enum.GetValues(typeof(LogLevel))) {
                foreach (var define in LogLevelDefines.For(level)) {
                    StringAssert.StartsWith(LogLevelDefines.Prefix, define);
                }
            }
        }

        [Test]
        public void MergeKeepsOtherDefinesInOrderAndReplacesEveryLogDefine() {
            var current = new[] {
                "UNITY_POST_PROCESSING_STACK_V2", LogLevelDefines.VerboseDefine, "GAME_DEMO",
                LogLevelDefines.Prefix + "RETIRED"
            };

            CollectionAssert.AreEqual(
                new[] {
                    "UNITY_POST_PROCESSING_STACK_V2", "GAME_DEMO", LogLevelDefines.InfoDefine,
                    LogLevelDefines.WarningDefine
                },
                LogLevelDefines.Merge(current, LogLevel.Info));
        }

        [Test]
        public void MergingAgainLeavesOneSetOfLogDefines() {
            var once = LogLevelDefines.Merge(new[] { "GAME_DEMO" }, LogLevel.Verbose);
            CollectionAssert.AreEqual(new[] { "GAME_DEMO", LogLevelDefines.WarningDefine },
                LogLevelDefines.Merge(once, LogLevel.Warning));
            CollectionAssert.AreEqual(new[] { "GAME_DEMO" }, LogLevelDefines.Merge(once, LogLevel.Error));
        }

        [Test]
        public void MergeOfNoDefinesIsTheLevelsOwn() {
            CollectionAssert.AreEqual(LogLevelDefines.For(LogLevel.Debug), LogLevelDefines.Merge(null, LogLevel.Debug));
            CollectionAssert.IsEmpty(LogLevelDefines.Merge(new string[0], LogLevel.Error));
        }

        [Test]
        public void MergeMatchesThePrefixExactly() {
            var current = new[] { "spellbound_log_info", "SPELLBOUND_LOGGER" };
            CollectionAssert.AreEqual(current, LogLevelDefines.Merge(current, LogLevel.None));
        }

        [TestCase(LogLevel.Verbose,
            new[] { LogLevel.Verbose, LogLevel.Debug, LogLevel.Info, LogLevel.Warning, LogLevel.Error })]
        [TestCase(LogLevel.Debug, new[] { LogLevel.Debug, LogLevel.Info, LogLevel.Warning, LogLevel.Error })]
        [TestCase(LogLevel.Info, new[] { LogLevel.Info, LogLevel.Warning, LogLevel.Error })]
        [TestCase(LogLevel.Warning, new[] { LogLevel.Warning, LogLevel.Error })]
        [TestCase(LogLevel.Error, new[] { LogLevel.Error })]
        [TestCase(LogLevel.None, new[] { LogLevel.Error })]
        public void TheCompiledLevelsStartAtTheGlobalLevelAndNeverIncludeNone(LogLevel globalLevel,
            LogLevel[] expected) {
            CollectionAssert.AreEqual(expected, LogLevelDefines.CompiledLevels(globalLevel));
        }

        private static string ConditionOf(string methodName) {
            var plain = typeof(Log).GetMethod(methodName, BindingFlags.Public | BindingFlags.Static, null,
                new[] { typeof(string), typeof(string), typeof(string), typeof(int) }, null);
            var withContext = typeof(Log).GetMethod(methodName, BindingFlags.Public | BindingFlags.Static, null,
                new[] { typeof(string), typeof(UnityEngine.Object), typeof(string), typeof(string), typeof(int) }, null);
            Assert.IsNotNull(plain, methodName);
            Assert.IsNotNull(withContext, methodName + " with a context");
            var condition = plain.GetCustomAttribute<ConditionalAttribute>()?.ConditionString;
            Assert.AreEqual(condition, withContext.GetCustomAttribute<ConditionalAttribute>()?.ConditionString,
                methodName);
            return condition;
        }
    }
}
