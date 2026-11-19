// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Spellbound.Core.Logging;
using Spellbound.Core.Observations;
using Spellbound.Core.Operations;
using Spellbound.Core.Tests.OperationFixture;
using UnityEngine;

namespace Spellbound.Core.Tests {
    public class OperationReplayTests {
        private static readonly Assembly Fixture = typeof(FixtureOperations).Assembly;
        private static readonly Assembly Core = typeof(OperationHost).Assembly;

        private static readonly string[] SessionLines = {
            "fixture stamp",
            "fixture stamp",
            "fixture purge --confirm",
            "fixture add 1 x",
            "fixture wipe --dry-run"
        };

        private RecordingLogSink _recording;
        private Log.SinkSuspension _suspension;
        private OperationCaller _recorder;
        private OperationCaller _developer;

        [SetUp]
        public void SetUp() {
            _suspension = Log.SuspendSinks();
            _recording = new RecordingLogSink(LogLevel.Verbose);
            Log.AddSink(_recording, null, LogLevel.Verbose);
            _recorder = new OperationCaller("recorder", OperationAudiences.Developer);
            _developer = OperationCaller.Development();
            FixtureOperations.Reset();
        }

        [TearDown]
        public void TearDown() {
            Log.RemoveSink(_recording);
            _suspension.Dispose();
            FixtureOperations.Reset();
        }

        private static OperationHost NewHost() {
            return new OperationHost(new OperationCatalog(() => new[] { Fixture, Core }),
                () => ScriptableObject.CreateInstance<OperationSettings>());
        }

        private List<OperationHistoryEntry> RecordSession(params string[] lines) {
            var recording = NewHost();

            foreach (var line in lines) {
                recording.Run(line, _recorder, ObservationContext.Discard);
            }

            var entries = new List<OperationHistoryEntry>(recording.History);
            FixtureOperations.Reset();
            return entries;
        }

        private static string Counts(OperationHost host) {
            return host.Run("fixture counts", OperationCaller.Development(), ObservationContext.Discard).Result.Text;
        }

        [Test]
        public void ReplayOnASecondHostReproducesTheRecordedCounts() {
            var entries = RecordSession(SessionLines);
            var replaying = NewHost();
            Assert.AreEqual("succeeded: stamp 0 purge 0 wipe 0", Counts(replaying));
            var run = replaying.Replay(entries, _developer, ObservationContext.Discard);
            Assert.IsTrue(run.IsDone);
            Assert.AreEqual(OperationStatus.Succeeded, run.Result.Status);
            Assert.AreEqual("succeeded: stamp 2 purge 1 wipe 0", Counts(replaying));
        }

        [Test]
        public void ReplaySkipsAnInvalidAndAPreviewedLineAndCountsThem() {
            var entries = RecordSession(SessionLines);
            Assert.AreEqual(OperationStatus.Invalid, entries[3].Status);
            Assert.AreEqual(OperationStatus.Previewed, entries[4].Status);
            var run = NewHost().Replay(entries, _developer, ObservationContext.Discard);
            CollectionAssert.Contains(run.Lines, "replayed 3, skipped 2");
            Assert.AreEqual(3, run.Results.Count);
        }

        [Test]
        public void ReplayedLinesEnterTheSecondHostsHistoryUnderTheReplayCaller() {
            var entries = RecordSession(SessionLines);
            var replaying = NewHost();
            replaying.Replay(entries, _developer, ObservationContext.Discard);
            Assert.AreEqual(3, replaying.History.Count);
            CollectionAssert.AreEqual(new[] { "fixture stamp", "fixture stamp", "fixture purge --confirm" },
                new[] { replaying.History[0].Line, replaying.History[1].Line, replaying.History[2].Line });

            foreach (var entry in replaying.History) {
                Assert.AreEqual("development", entry.Caller);
                Assert.AreEqual(OperationStatus.Succeeded, entry.Status);
            }
        }

        [Test]
        public void AnEmptyHistoryReplaysToSucceeded() {
            var replaying = NewHost();
            var run = replaying.Replay(new List<OperationHistoryEntry>(), _developer, ObservationContext.Discard);
            Assert.IsTrue(run.IsDone);
            Assert.AreEqual(OperationStatus.Succeeded, run.Result.Status);
            CollectionAssert.Contains(run.Lines, "replayed 0, skipped 0");
            Assert.AreEqual(0, replaying.History.Count);
        }

        [Test]
        public void AHistoryOfOnlyAnInvalidLineReplaysToSucceeded() {
            var entries = RecordSession("fixture add 1 x");
            var replaying = NewHost();
            var run = replaying.Replay(entries, _developer, ObservationContext.Discard);
            Assert.IsTrue(run.IsDone);
            Assert.AreEqual(OperationStatus.Succeeded, run.Result.Status);
            CollectionAssert.Contains(run.Lines, "replayed 0, skipped 1");
            Assert.AreEqual(0, replaying.History.Count);
        }

        [Test]
        public void ReplayOfAHostsOwnHistoryReadsACopy() {
            var host = NewHost();

            foreach (var line in SessionLines) {
                host.Run(line, _recorder, ObservationContext.Discard);
            }

            var run = host.Replay(host.History, _developer, ObservationContext.Discard);
            CollectionAssert.Contains(run.Lines, "replayed 3, skipped 2");
            Assert.AreEqual(SessionLines.Length + 3, host.History.Count);
            Assert.AreEqual("succeeded: stamp 4 purge 2 wipe 0", Counts(host));
        }

        [Test]
        public void ReplayRejectsAMissingArgument() {
            var host = NewHost();
            var entries = new List<OperationHistoryEntry>();
            Assert.Throws<ArgumentNullException>(() => host.Replay(null, _developer, ObservationContext.Discard));
            Assert.Throws<ArgumentNullException>(() => host.Replay(entries, null, ObservationContext.Discard));
            Assert.Throws<ArgumentNullException>(() => host.Replay(entries, _developer, null));
        }
    }
}
