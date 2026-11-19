// Copyright (c) Spellbound Studios. All rights reserved.

#if SPELLBOUND_CORE_TESTS_BURST
using System;
using NUnit.Framework;
using Spellbound.Core.Observations;
using Unity.Burst;
using Unity.Jobs;

namespace Spellbound.Core.Tests {
    public class NativeObservationQueueBurstTests {
        private const int LatticeCellCount = 17 * 17 * 17;

        private static readonly Counter Visited = CoreKit.Observations.Counter("nativequeuebursttests.cell.visited",
            "A lattice cell a Burst job visited and counted through a native observation queue.");

        [BurstCompile(CompileSynchronously = true)]
        private struct CountEveryCellJob : IJobParallelFor {
            public NativeObservationWriter Writer;
            public uint KeyHash;

            public void Execute(int index) {
                Writer.Count(KeyHash, 1);
            }
        }

        [Test]
        public void ABurstJobCountsEveryCellAndDrainAddsThemExactly() {
            var context = CoreKit.Observations.CreateContext(
                "nativequeueburst_" + Guid.NewGuid().ToString("N").Substring(0, 8));
            var queue = context.CreateQueue(LatticeCellCount);

            try {
                var job = new CountEveryCellJob { Writer = queue.Writer, KeyHash = Visited.Key.Hash };
                job.Schedule(LatticeCellCount, 64).Complete();

                Assert.AreEqual(LatticeCellCount, queue.Drain());
                Assert.IsTrue(context.TryRead(Visited.Key, out var reading));
                Assert.AreEqual((double)LatticeCellCount, reading.Value);
                Assert.IsFalse(context.TryRead(CoreObservations.QueueDropped.Key, out _));
            }
            finally {
                queue.Dispose();
                context.Dispose();
            }
        }
    }
}
#endif
