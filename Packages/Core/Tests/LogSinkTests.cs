// Copyright 2026 Spellbound Studio Inc.

using System.Linq;
using NUnit.Framework;
using Spellbound.Core.Logging;

namespace Spellbound.Core.Tests {
    public class LogSinkTests {
        private static RecordingLogSink NewSink() => new RecordingLogSink(LogLevel.Verbose);

        private static void Emit(LogLevel level, string message) =>
                Log.Emit(level, message, "LogSinkTests.cs", nameof(Emit), 0);

        private static string[] MessagesOf(RecordingLogSink sink) =>
                sink.Entries.Select(entry => entry.Message).ToArray();

        [Test]
        public void AddedSinkReceivesEmittedMessages() {
            var sink = NewSink();
            Log.AddSink(sink, null, LogLevel.Verbose);

            try {
                Emit(LogLevel.Error, "boom");

                CollectionAssert.AreEqual(new[] { "boom" }, MessagesOf(sink));
            }
            finally {
                Log.RemoveSink(sink);
            }
        }

        [Test]
        public void RemovedSinkReceivesNothingFurther() {
            var sink = NewSink();
            Log.AddSink(sink, null, LogLevel.Verbose);
            Emit(LogLevel.Error, "before");

            Assert.IsTrue(Log.RemoveSink(sink));

            Emit(LogLevel.Error, "after");

            CollectionAssert.AreEqual(new[] { "before" }, MessagesOf(sink));
        }

        [Test]
        public void RemoveSinkReturnsFalseWhenNotRegistered() {
            Assert.IsFalse(Log.RemoveSink(NewSink()));
            Assert.IsFalse(Log.RemoveSink(null));
        }

        [Test]
        public void RemoveSinkRemovesOnlyTheGivenInstance() {
            var kept = NewSink();
            var removed = NewSink();
            Log.AddSink(kept, null, LogLevel.Verbose);
            Log.AddSink(removed, null, LogLevel.Verbose);

            try {
                Log.RemoveSink(removed);
                Emit(LogLevel.Error, "boom");

                CollectionAssert.AreEqual(new[] { "boom" }, MessagesOf(kept));
                Assert.AreEqual(0, removed.Count);
            }
            finally {
                Log.RemoveSink(kept);
            }
        }

        [Test]
        public void RemoveSinkIsIdempotent() {
            var sink = NewSink();
            Log.AddSink(sink, null, LogLevel.Verbose);

            Assert.IsTrue(Log.RemoveSink(sink));
            Assert.IsFalse(Log.RemoveSink(sink));
        }

        [Test]
        public void ScopedSinkUnregistersOnDispose() {
            var sink = NewSink();

            using (Log.AddScopedSink(sink, null, LogLevel.Verbose))
                Emit(LogLevel.Error, "inside");

            Emit(LogLevel.Error, "outside");

            CollectionAssert.AreEqual(new[] { "inside" }, MessagesOf(sink));
        }

        [Test]
        public void RegistrationFilterLevelGatesEmission() {
            var sink = NewSink();

            using (Log.AddScopedSink(sink, null, LogLevel.Warning)) {
                Emit(LogLevel.Info, "chatter");
                Emit(LogLevel.Warning, "careful");
                Emit(LogLevel.Error, "boom");
            }

            CollectionAssert.AreEqual(
                new[] { LogLevel.Warning, LogLevel.Error },
                sink.Entries.Select(entry => entry.Level).ToArray()
            );
        }

        [Test]
        public void SinkMinimumLevelGatesRecording() {
            var sink = new RecordingLogSink(LogLevel.Error);

            using (Log.AddScopedSink(sink, null, LogLevel.Verbose)) {
                Emit(LogLevel.Warning, "careful");
                Emit(LogLevel.Error, "boom");
            }

            CollectionAssert.AreEqual(new[] { "boom" }, MessagesOf(sink));
        }

        [Test]
        public void ErrorReachesScopedSink() {
            var sink = NewSink();

            using (Log.AddScopedSink(sink, null, LogLevel.Error))
                Log.Error("reported");

            var entries = sink.Entries;

            Assert.AreEqual(1, entries.Length);
            Assert.AreEqual(LogLevel.Error, entries[0].Level);
            Assert.AreEqual("reported", entries[0].Message);
            Assert.AreEqual("LogSinkTests", entries[0].Source);
        }

        [Test]
        public void CountOfAndContainsMatchRecordedEntries() {
            var sink = NewSink();

            using (Log.AddScopedSink(sink, null, LogLevel.Verbose)) {
                Emit(LogLevel.Warning, "disk almost full");
                Emit(LogLevel.Error, "chunk load failed");
                Emit(LogLevel.Error, "chunk save failed");
            }

            Assert.AreEqual(2, sink.CountOf(LogLevel.Error));
            Assert.AreEqual(1, sink.CountOf(LogLevel.Warning));
            Assert.IsTrue(sink.Contains(LogLevel.Error, "save failed"));
            Assert.IsFalse(sink.Contains(LogLevel.Warning, "save failed"));
        }

        [Test]
        public void ClearSinksRemovesEveryRegistration() {
            var sink = NewSink();
            Log.AddSink(sink, null, LogLevel.Verbose);

            Log.ClearSinks();
            Emit(LogLevel.Error, "boom");

            Assert.AreEqual(0, sink.Count);
            Assert.IsFalse(Log.RemoveSink(sink));
        }

        [Test]
        public void SuspendSinksMutesThenRestores() {
            var sink = NewSink();
            Log.AddSink(sink, null, LogLevel.Verbose);

            try {
                using (Log.SuspendSinks())
                    Emit(LogLevel.Error, "muted");

                Emit(LogLevel.Error, "audible");

                CollectionAssert.AreEqual(new[] { "audible" }, MessagesOf(sink));
            }
            finally {
                Log.RemoveSink(sink);
            }
        }

        [Test]
        public void SinksAddedDuringSuspensionSurviveRestore() {
            var original = NewSink();
            var recorder = NewSink();
            Log.AddSink(original, null, LogLevel.Verbose);

            try {
                using (Log.SuspendSinks()) {
                    Log.AddSink(recorder, null, LogLevel.Verbose);
                    Emit(LogLevel.Error, "during");
                }

                Emit(LogLevel.Error, "after");

                CollectionAssert.AreEqual(new[] { "during", "after" }, MessagesOf(recorder));
                CollectionAssert.AreEqual(new[] { "after" }, MessagesOf(original));
            }
            finally {
                Log.RemoveSink(original);
                Log.RemoveSink(recorder);
            }
        }

        [Test]
        public void DisposingSuspensionTwiceDoesNotDuplicateSinks() {
            var sink = NewSink();
            Log.AddSink(sink, null, LogLevel.Verbose);

            try {
                var suspension = Log.SuspendSinks();
                suspension.Dispose();
                suspension.Dispose();

                Emit(LogLevel.Error, "once");

                CollectionAssert.AreEqual(new[] { "once" }, MessagesOf(sink));
            }
            finally {
                Log.RemoveSink(sink);
            }
        }

        [Test]
        public void ClearDropsRecordedEntries() {
            var sink = NewSink();

            using (Log.AddScopedSink(sink, null, LogLevel.Verbose)) {
                Emit(LogLevel.Error, "boom");
                sink.Clear();
                Emit(LogLevel.Error, "again");
            }

            CollectionAssert.AreEqual(new[] { "again" }, MessagesOf(sink));
        }
    }
}
