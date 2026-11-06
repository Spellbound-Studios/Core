// Copyright 2026 Spellbound Studio Inc.

using System.Collections.Generic;
using NUnit.Framework;
using Spellbound.Core.Sampling;

namespace Spellbound.Core.Tests {
    public class WeightedTableTests {
        private class Candidate {
            public string Name;
        }

        private static readonly Candidate A = new() { Name = "A" };
        private static readonly Candidate B = new() { Name = "B" };
        private static readonly Candidate C = new() { Name = "C" };

        private static WeightedTable<Candidate> Table(int nothingWeight, params (Candidate candidate, int weight)[] entries) {
            var list = new List<WeightedEntry<Candidate>>();

            foreach (var (candidate, weight) in entries)
                list.Add(new WeightedEntry<Candidate> { candidate = candidate, weight = weight });

            var table = new WeightedTable<Candidate>();
            table.Define(list, nothingWeight);

            return table;
        }

        [Test]
        public void PickIndexMapsEveryRollToTheSlotOwningItsBand() {
            var table = Table(4, (A, 1), (B, 3));

            Assert.AreEqual(8, table.TotalWeight);
            Assert.AreEqual(0, table.PickIndex(0));

            for (var roll = 1; roll < 4; roll++)
                Assert.AreEqual(1, table.PickIndex(roll));

            for (var roll = 4; roll < 8; roll++)
                Assert.AreEqual(-1, table.PickIndex(roll));
        }

        [Test]
        public void PickIndexReturnsNothingOutsideTheRange() {
            var table = Table(0, (A, 2), (B, 2));

            Assert.AreEqual(1, table.PickIndex(3));
            Assert.AreEqual(-1, table.PickIndex(4));
            Assert.AreEqual(-1, table.PickIndex(-1));
        }

        [Test]
        public void PickIndexSkipsZeroWeightAndNullCandidates() {
            var table = Table(0, (null, 5), (A, 0), (B, 1));

            Assert.AreEqual(1, table.TotalWeight);
            Assert.AreEqual(2, table.PickIndex(0));
        }

        [Test]
        public void TryPickReportsNothingAsFalse() {
            var table = Table(1, (A, 1));

            Assert.IsTrue(table.TryPick(0, out var picked));
            Assert.AreSame(A, picked);
            Assert.IsFalse(table.TryPick(1, out picked));
            Assert.IsNull(picked);
        }

        [Test]
        public void EmptyTableNeverPicks() {
            var table = new WeightedTable<Candidate>();

            Assert.AreEqual(0, table.TotalWeight);
            Assert.IsFalse(table.TryPick(new System.Random(1), out _));
            Assert.IsEmpty(table.Sample(3, new System.Random(1)));
            Assert.IsEmpty(table.Sample(3, new System.Random(1), true));
        }

        [Test]
        public void SameSeedGivesTheSameSequence() {
            var table = Table(2, (A, 1), (B, 3), (C, 5));

            var first = table.Sample(20, new System.Random(99), true);
            var second = table.Sample(20, new System.Random(99), true);

            CollectionAssert.AreEqual(first, second);
        }

        [Test]
        public void SampleWithoutReplacementReturnsDistinctPicksAndStopsWhenExhausted() {
            var table = Table(0, (A, 1), (B, 1), (C, 1));

            var picked = table.Sample(5, new System.Random(7));

            Assert.AreEqual(3, picked.Count);
            CollectionAssert.AllItemsAreUnique(picked);
        }

        [Test]
        public void SampleWithReplacementCanRepeat() {
            var table = Table(0, (A, 1));

            var picked = table.Sample(3, new System.Random(7), true);

            Assert.AreEqual(3, picked.Count);

            foreach (var candidate in picked)
                Assert.AreSame(A, candidate);
        }

        [Test]
        public void NothingConsumesAPickAndIsNeverRemoved() {
            var table = Table(1, (A, 1));

            var withReplacement = table.Sample(200, new System.Random(3), true);
            var withoutReplacement = table.Sample(200, new System.Random(3));

            Assert.Less(withReplacement.Count, 200);
            Assert.Greater(withReplacement.Count, 0);
            Assert.AreEqual(1, withoutReplacement.Count);
            Assert.AreSame(A, withoutReplacement[0]);
        }

        [Test]
        public void OnlyNothingLeftStopsSampling() {
            var table = Table(5, (A, 0));

            Assert.IsEmpty(table.Sample(10, new System.Random(1)));
            Assert.IsEmpty(table.Sample(10, new System.Random(1), true));
        }

        [Test]
        public void DefineRebuildsTheCumulativeWeights() {
            var table = Table(0, (A, 1));

            Assert.AreEqual(1, table.TotalWeight);

            table.Define(new[] { new WeightedEntry<Candidate> { candidate = B, weight = 4 } }, 2);

            Assert.AreEqual(6, table.TotalWeight);
            Assert.AreEqual(0, table.PickIndex(3));
            Assert.AreEqual(-1, table.PickIndex(4));
        }
    }
}
