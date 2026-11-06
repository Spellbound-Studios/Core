// Copyright 2026 Spellbound Studio Inc.

using System.Linq;
using System.Threading;
using NUnit.Framework;
using Spellbound.Core.Logging;

namespace Spellbound.Core.Tests {
    public class TallyingLogSinkTests {
        private static void Emit(TallyingLogSink sink, LogLevel level, string message, int line = 0) =>
                sink.Emit(level, "TallyingLogSinkTests", message, nameof(Emit), line, null);

        [Test]
        public void RepeatedMessageCountsUpAsOneEntry() {
            var sink = new TallyingLogSink(16);

            Emit(sink, LogLevel.Error, "boom");
            Emit(sink, LogLevel.Error, "boom");
            Emit(sink, LogLevel.Error, "boom");

            var tallies = sink.Drain();

            Assert.AreEqual(1, tallies.Length);
            Assert.AreEqual("boom", tallies[0].Message);
            Assert.AreEqual(3, tallies[0].Count);
            Assert.AreEqual(LogLevel.Error, tallies[0].Level);
        }

        [Test]
        public void DifferentMessagesAreSeparateEntriesInOrder() {
            var sink = new TallyingLogSink(16);

            Emit(sink, LogLevel.Warning, "first");
            Emit(sink, LogLevel.Error, "second");
            Emit(sink, LogLevel.Warning, "first");

            var tallies = sink.Drain();

            CollectionAssert.AreEqual(new[] { "first", "second" }, tallies.Select(tally => tally.Message));
            CollectionAssert.AreEqual(new[] { 2, 1 }, tallies.Select(tally => tally.Count));
        }

        [Test]
        public void SameTextAtDifferentLevelsOrLinesStaysDistinct() {
            var sink = new TallyingLogSink(16);

            Emit(sink, LogLevel.Warning, "same");
            Emit(sink, LogLevel.Error, "same");
            Emit(sink, LogLevel.Error, "same", 7);

            Assert.AreEqual(3, sink.DistinctCount);
        }

        [Test]
        public void DrainReturnsEverythingAndResets() {
            var sink = new TallyingLogSink(16);

            Emit(sink, LogLevel.Error, "boom");

            Assert.AreEqual(1, sink.Drain().Length);
            Assert.AreEqual(0, sink.DistinctCount);
            Assert.AreEqual(0, sink.Drain().Length);

            Emit(sink, LogLevel.Error, "boom");

            Assert.AreEqual(1, sink.Drain()[0].Count);
        }

        [Test]
        public void OverflowBeyondTheCapIsDroppedAndCounted() {
            var sink = new TallyingLogSink(2);

            Emit(sink, LogLevel.Error, "one");
            Emit(sink, LogLevel.Error, "two");
            Emit(sink, LogLevel.Error, "three");
            Emit(sink, LogLevel.Error, "four");
            Emit(sink, LogLevel.Error, "one");

            Assert.AreEqual(2, sink.DistinctCount);
            Assert.AreEqual(2, sink.DroppedCount);

            var tallies = sink.Drain(out var dropped);

            Assert.AreEqual(2, dropped);
            Assert.AreEqual(2, tallies[0].Count);
            Assert.AreEqual(0, sink.DroppedCount);
        }

        [Test]
        public void CapBelowOneIsRaisedToOne() {
            var sink = new TallyingLogSink(0);

            Assert.AreEqual(1, sink.MaximumDistinctEntries);
        }

        [Test]
        public void ParallelEmitsLoseNothing() {
            const int threadCount = 4;
            const int emitsPerThread = 1000;
            const int distinctMessages = 10;

            var sink = new TallyingLogSink(distinctMessages);
            var threads = new Thread[threadCount];

            for (var t = 0; t < threadCount; t++) {
                threads[t] = new Thread(() => {
                    for (var i = 0; i < emitsPerThread; i++)
                        Emit(sink, LogLevel.Error, "message " + i % distinctMessages);
                });
            }

            foreach (var thread in threads)
                thread.Start();

            foreach (var thread in threads)
                thread.Join();

            var tallies = sink.Drain(out var dropped);

            Assert.AreEqual(0, dropped);
            Assert.AreEqual(distinctMessages, tallies.Length);
            Assert.AreEqual(threadCount * emitsPerThread, tallies.Sum(tally => tally.Count));
        }

        [Test]
        public void RegisteredSinkHonoursTheFilterLevel() {
            var sink = new TallyingLogSink(16);

            using (Log.SuspendSinks())
            using (Log.AddScopedSink(sink, null, LogLevel.Warning)) {
                Log.Emit(LogLevel.Info, "quiet", "TallyingLogSinkTests.cs", nameof(RegisteredSinkHonoursTheFilterLevel), 0);
                Log.Emit(LogLevel.Warning, "loud", "TallyingLogSinkTests.cs", nameof(RegisteredSinkHonoursTheFilterLevel), 0);
            }

            var tallies = sink.Drain();

            Assert.AreEqual(1, tallies.Length);
            Assert.AreEqual("loud", tallies[0].Message);
            Assert.AreEqual("TallyingLogSinkTests", tallies[0].Source);
        }
    }
}
