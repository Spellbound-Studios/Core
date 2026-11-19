// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using Spellbound.Core.Observations;
using Spellbound.Core.Simulation;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace Spellbound.Core.Tests {
    public class NativeObservationQueueTests {
        private const int ThreadCount = 8;
        private const int EntriesPerThread = 10000;

        private static readonly Counter Written = CoreKit.Observations.Counter("nativequeuetests.queue.written",
            "An entry a job wrote into a native observation queue.");

        private static readonly Occurrence Arrived = CoreKit.Observations.Occurrence(
            "nativequeuetests.queue.arrived",
            "A body a job saw arrive, written into a native observation queue.");

        private readonly List<RecordedOccurrence> _records = new List<RecordedOccurrence>(16);
        private readonly List<ObservationContext> _contexts = new List<ObservationContext>(2);
        private readonly List<NativeObservationQueue> _queues = new List<NativeObservationQueue>(2);

        [TearDown]
        public void DisposeEverything() {
            foreach (var queue in _queues) {
                queue.Dispose();
            }

            foreach (var context in _contexts) {
                context.Dispose();
            }

            _queues.Clear();
            _contexts.Clear();
        }

        [Test]
        public void TheEntryIsThirtyTwoBytes() {
            Assert.AreEqual(32, UnsafeUtility.SizeOf<NativeObservationEntry>());
        }

        [Test]
        public void ACountWrittenAndDrainedAddsTheCounter() {
            var context = CreateContext();
            var queue = CreateQueue(context, 64);

            queue.Writer.Count(Written.Key.Hash, 5);
            queue.Writer.Count(Written.Key.Hash, 2);

            Assert.AreEqual(2, queue.Drain());
            Assert.AreEqual(7d, ReadValue(context, Written.Key));
        }

        [Test]
        public void AnOccurrenceWrittenLandsInTheRingWithItsTickAndSubject() {
            var context = CreateContext();
            var queue = CreateQueue(context, 64);

            queue.Writer.Record(Arrived.Key.Hash, new Tick(812), 0x0A0B0C0D01020304UL);

            Assert.AreEqual(1, queue.Drain());
            Assert.AreEqual(1, context.ReadRecords(_records));
            Assert.AreEqual(Arrived.Key.Hash, _records[0].KeyHash);
            Assert.AreEqual(new Tick(812), _records[0].Tick);
            Assert.AreEqual(0x0A0B0C0D01020304UL, _records[0].Subject);
            Assert.AreEqual(1d, ReadValue(context, Arrived.Key));
        }

        [Test]
        public void EightThreadsWritingUnderCapacityLoseNothing() {
            var context = CreateContext();
            var queue = CreateQueue(context, 100000);
            var writer = queue.Writer;
            var key = Written.Key.Hash;

            Parallel.For(0, ThreadCount, new ParallelOptions { MaxDegreeOfParallelism = ThreadCount }, thread => {
                for (var i = 0; i < EntriesPerThread; i++) {
                    writer.Count(key, 1);
                }
            });

            Assert.AreEqual(ThreadCount * EntriesPerThread, queue.Drain());
            Assert.AreEqual(ThreadCount * EntriesPerThread, ReadValue(context, Written.Key));
            Assert.IsFalse(context.TryRead(CoreObservations.QueueDropped.Key, out _));
        }

        [Test]
        public void AFullQueueDropsAndCountsTheDropped() {
            var context = CreateContext();
            var queue = CreateQueue(context, 16);

            for (var i = 0; i < 20; i++) {
                queue.Writer.Count(Written.Key.Hash, 1);
            }

            Assert.AreEqual(16, queue.Drain());
            Assert.AreEqual(16d, ReadValue(context, Written.Key));
            Assert.AreEqual(4d, ReadValue(context, CoreObservations.QueueDropped.Key));
        }

        [Test]
        public void TheDroppedCountResetsOnDrain() {
            var context = CreateContext();
            var queue = CreateQueue(context, 4);

            for (var i = 0; i < 6; i++) {
                queue.Writer.Count(Written.Key.Hash, 1);
            }

            queue.Drain();

            for (var i = 0; i < 4; i++) {
                queue.Writer.Count(Written.Key.Hash, 1);
            }

            Assert.AreEqual(4, queue.Drain());
            Assert.AreEqual(8d, ReadValue(context, Written.Key));
            Assert.AreEqual(2d, ReadValue(context, CoreObservations.QueueDropped.Key));
        }

        [Test]
        public void TheRingWrapsAcrossDrainsInOrder() {
            var context = CreateContext();
            var queue = CreateQueue(context, 4);

            for (var round = 0; round < 3; round++) {
                for (var i = 0; i < 3; i++) {
                    queue.Writer.Record(Arrived.Key.Hash, new Tick((ulong)(round * 3 + i + 1)), (ulong)round);
                }

                Assert.AreEqual(3, queue.Drain());
            }

            Assert.AreEqual(9, context.ReadRecords(_records));

            for (var i = 0; i < 9; i++) {
                Assert.AreEqual(new Tick((ulong)(i + 1)), _records[i].Tick);
            }
        }

        [Test]
        public void AnUndeclaredKeyHashIsLoggedOnceAndSkipped() {
            var context = CreateContext();
            var queue = CreateQueue(context, 16);
            const uint undeclared = 0x5EEDF00Du;

            LogAssert.Expect(LogType.Error, new Regex(undeclared.ToString()));
            queue.Writer.Count(undeclared, 1);
            queue.Writer.Count(undeclared, 1);
            queue.Writer.Count(Written.Key.Hash, 1);

            Assert.AreEqual(3, queue.Drain());
            queue.Writer.Count(undeclared, 1);
            Assert.AreEqual(1, queue.Drain());
            LogAssert.NoUnexpectedReceived();
            Assert.AreEqual(1d, ReadValue(context, Written.Key));
        }

        [Test]
        public void CreateQueueRefusesACapacityUnderOne() {
            var context = CreateContext();

            Assert.Throws<ArgumentOutOfRangeException>(() => context.CreateQueue(0));
        }

        [Test]
        public void QueueWrite_NoAllocation() {
            var context = CreateContext();
            var queue = CreateQueue(context, 4096);
            var writer = queue.Writer;
            var countKey = Written.Key.Hash;
            var recordKey = Arrived.Key.Hash;

            AssertNoAllocation(() => {
                for (var i = 0; i < 1000; i++) {
                    writer.Count(countKey, 1);
                    writer.Record(recordKey, new Tick((ulong)i), 9UL);
                }

                queue.Drain();
            });
        }

        [Test]
        public void QueueDrain_NoAllocation() {
            var context = CreateContext();
            var queue = CreateQueue(context, 64);
            var writer = queue.Writer;
            var countKey = Written.Key.Hash;
            var recordKey = Arrived.Key.Hash;

            AssertNoAllocation(() => {
                for (var i = 0; i < 80; i++) {
                    writer.Count(countKey, 1);
                    writer.Record(recordKey, new Tick((ulong)i), 9UL);
                }

                queue.Drain();
            });

            Assert.Greater(ReadValue(context, CoreObservations.QueueDropped.Key), 0d);
        }

        [Test]
        public void DisposeTwiceIsSafe() {
            var context = CreateContext();
            var queue = context.CreateQueue(8);

            queue.Dispose();
            queue.Dispose();

            Assert.AreEqual(0, queue.Drain());
            Assert.DoesNotThrow(() => queue.Writer.Count(Written.Key.Hash, 1));
        }

        private ObservationContext CreateContext() {
            var context = CoreKit.Observations.CreateContext(
                "nativequeue_" + Guid.NewGuid().ToString("N").Substring(0, 8));
            _contexts.Add(context);
            return context;
        }

        private NativeObservationQueue CreateQueue(ObservationContext context, int capacity) {
            var queue = context.CreateQueue(capacity);
            _queues.Add(queue);
            return queue;
        }

        private static double ReadValue(ObservationContext context, ObservationKey key) {
            Assert.IsTrue(context.TryRead(key, out var reading), key + " has no reading.");
            return reading.Value;
        }

        private static void AssertNoAllocation(TestDelegate action) {
            for (var warm = 0; warm < 3; warm++) {
                action();
            }

            var constraint = ((IResolveConstraint)Is.Not.AllocatingGCMemory()).Resolve();
            constraint.ApplyTo(action);
            Assert.That(action, constraint);
        }
    }
}
