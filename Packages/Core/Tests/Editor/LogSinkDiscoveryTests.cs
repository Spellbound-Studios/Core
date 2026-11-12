// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Spellbound.Core.Logging;
using Spellbound.Core.Logging.Editor;
using UnityEngine;

namespace Spellbound.Core.Tests {
    public class LogSinkDiscoveryTests {
        private const string FirstSink = "Fixture.FirstSink, Fixture";
        private const string SecondSink = "Fixture.SecondSink, Fixture";

        private sealed class HiddenSink : ILogSink {
            public string DisplayName => "Hidden";

            public void Initialize(LogConfig config) { }

            public void Emit(LogLevel level, string source, string message, string member, int line,
                UnityEngine.Object context) { }
        }

        public sealed class NestedSink : ILogSink {
            public string DisplayName => "Nested";

            public void Initialize(LogConfig config) { }

            public void Emit(LogLevel level, string source, string message, string member, int line,
                UnityEngine.Object context) { }
        }

        private LogConfig _config;

        [SetUp]
        public void SetUp() {
            _config = ScriptableObject.CreateInstance<LogConfig>();
        }

        [TearDown]
        public void TearDown() {
            UnityEngine.Object.DestroyImmediate(_config);
        }

        [Test]
        public void DiscoveryFindsCoresSinksSortedByDisplayName() {
            var sinks = LogSinkDiscovery.Discover();
            Assert.AreEqual("Unity Console", DisplayNameOf(sinks, typeof(UnityConsoleSink)));
            Assert.AreEqual("Game Console", DisplayNameOf(sinks, typeof(GameConsoleSink)));
            Assert.AreEqual("Write To File", DisplayNameOf(sinks, typeof(FileSink)));
            var names = sinks.Select(sink => sink.DisplayName).ToList();
            CollectionAssert.AreEqual(names.OrderBy(name => name, StringComparer.Ordinal).ToList(), names);
        }

        [Test]
        public void DiscoveryLeavesOutSinksWithoutAPublicParameterlessConstructorAndHiddenOrNestedSinks() {
            var types = LogSinkDiscovery.Discover().Select(sink => sink.QualifiedTypeName).ToList();
            CollectionAssert.DoesNotContain(types, typeof(RecordingLogSink).AssemblyQualifiedName);
            CollectionAssert.DoesNotContain(types, typeof(TallyingLogSink).AssemblyQualifiedName);
            CollectionAssert.DoesNotContain(types, typeof(HiddenSink).AssemblyQualifiedName);
            CollectionAssert.DoesNotContain(types, typeof(NestedSink).AssemblyQualifiedName);
            CollectionAssert.AllItemsAreUnique(types);
        }

        [Test]
        public void AppendingAddsADisabledEntryForEachMissingSinkAndKeepsEveryOther() {
            var unity = typeof(UnityConsoleSink).AssemblyQualifiedName;
            _config.globalLevel = LogLevel.Warning;
            _config.sinks = new[] {
                new SinkEntry {
                    qualifiedTypeName = unity, displayName = "Unity Console", enabled = true,
                    filterLevel = LogLevel.Error
                },
                new SinkEntry {
                    qualifiedTypeName = "Fixture.RemovedSink, Fixture", displayName = "Removed", enabled = true,
                    filterLevel = LogLevel.Warning
                }
            };

            var discovered = new[] {
                new LogSinkDescription(FirstSink, "First"), new LogSinkDescription(unity, "Unity Console"),
                new LogSinkDescription(SecondSink, "Second")
            };

            Assert.IsTrue(LogSinkDiscovery.AppendMissing(_config, discovered));
            var sinks = _config.sinks;
            CollectionAssert.AreEqual(new[] { unity, "Fixture.RemovedSink, Fixture", FirstSink, SecondSink },
                sinks.Select(entry => entry.qualifiedTypeName).ToArray());
            Assert.IsTrue(sinks[0].enabled);
            Assert.AreEqual(LogLevel.Error, sinks[0].filterLevel);
            Assert.IsTrue(sinks[1].enabled);
            Assert.AreEqual("First", sinks[2].displayName);
            Assert.IsFalse(sinks[2].enabled);
            Assert.AreEqual(LogLevel.Warning, sinks[2].filterLevel);
            Assert.AreEqual("Second", sinks[3].displayName);
            Assert.IsFalse(sinks[3].enabled);
        }

        [Test]
        public void AppendingTwiceChangesNothingTheSecondTime() {
            var discovered = new[] { new LogSinkDescription(FirstSink, "First") };
            Assert.IsTrue(LogSinkDiscovery.AppendMissing(_config, discovered));
            var appended = _config.sinks;
            Assert.IsFalse(LogSinkDiscovery.AppendMissing(_config, discovered));
            Assert.AreSame(appended, _config.sinks);
        }

        [Test]
        public void UnderLevelNoneANewEntryFiltersAtError() {
            _config.globalLevel = LogLevel.None;
            LogSinkDiscovery.AppendMissing(_config, new[] { new LogSinkDescription(FirstSink, "First") });
            Assert.AreEqual(LogLevel.Error, _config.sinks[0].filterLevel);
        }

        [Test]
        public void AppendingFromTheLiveDiscoveryCoversEveryDiscoveredSink() {
            Assert.IsTrue(LogSinkDiscovery.AppendMissing(_config));
            CollectionAssert.AreEqual(LogSinkDiscovery.Discover().Select(sink => sink.QualifiedTypeName),
                _config.sinks.Select(entry => entry.qualifiedTypeName));
            Assert.IsFalse(LogSinkDiscovery.AppendMissing(_config));
        }

        [Test]
        public void AConfigIsRequiredAndAMissingListCountsAsEmpty() {
            Assert.Throws<ArgumentNullException>(() =>
                LogSinkDiscovery.AppendMissing(null, new LogSinkDescription[0]));
            _config.sinks = null;
            Assert.IsFalse(LogSinkDiscovery.AppendMissing(_config, new LogSinkDescription[0]));
            var discovered = new[] { new LogSinkDescription(FirstSink, "First") };
            Assert.IsTrue(LogSinkDiscovery.AppendMissing(_config, discovered));
            Assert.AreEqual(1, _config.sinks.Length);
        }

        [Test]
        public void ADescriptionNeedsATypeName() {
            Assert.Throws<ArgumentException>(() => new LogSinkDescription(" ", "Blank"));
            Assert.AreEqual(string.Empty, new LogSinkDescription(FirstSink, null).DisplayName);
        }

        private static string DisplayNameOf(IReadOnlyList<LogSinkDescription> sinks, Type sinkType) {
            var found = sinks.FirstOrDefault(sink => sink.QualifiedTypeName == sinkType.AssemblyQualifiedName);
            Assert.IsNotNull(found, sinkType.FullName + " was not discovered");
            return found.DisplayName;
        }
    }
}
