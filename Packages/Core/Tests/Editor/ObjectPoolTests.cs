// Copyright 2026 Spellbound Studio Inc.

using NUnit.Framework;
using Spellbound.Core.ObjectPooling;

namespace Spellbound.Core.Tests {
    public class ObjectPoolTests {
        private class Item {
            public int ResetCount;
        }

        private class ItemPool : ObjectPool<Item> {
            public int Created;
            public int Discarded;

            protected override Item Create() {
                Created++;

                return new Item();
            }

            protected override void Reset(Item item) => item.ResetCount++;

            protected override void Discard(Item item) => Discarded++;
        }

        [Test]
        public void PrewarmFillsToCountWithResetItems() {
            var pool = new ItemPool();

            pool.Prewarm(3);

            Assert.AreEqual(3, pool.Created);
            Assert.AreEqual(3, pool.Available);
            Assert.AreEqual(1, pool.Rent().ResetCount);
            Assert.AreEqual(3, pool.Created);
        }

        [Test]
        public void PrewarmOnlyCreatesTheShortfall() {
            var pool = new ItemPool();
            pool.Return(pool.Rent());

            pool.Prewarm(3);

            Assert.AreEqual(3, pool.Created);
            Assert.AreEqual(3, pool.Available);

            pool.Prewarm(2);

            Assert.AreEqual(3, pool.Created);
            Assert.AreEqual(3, pool.Available);
        }

        [Test]
        public void PrewarmStopsAtMaximumRetained() {
            var pool = new ItemPool { MaximumRetained = 2 };

            pool.Prewarm(5);

            Assert.AreEqual(2, pool.Created);
            Assert.AreEqual(2, pool.Available);
        }

        [Test]
        public void ReturnDiscardsBeyondMaximumRetained() {
            var pool = new ItemPool { MaximumRetained = 1 };
            var first = pool.Rent();
            var second = pool.Rent();

            pool.Return(first);
            pool.Return(second);

            Assert.AreEqual(1, pool.Available);
            Assert.AreEqual(1, pool.Discarded);
            Assert.AreEqual(0, second.ResetCount);
            Assert.AreSame(first, pool.Rent());
        }

        [Test]
        public void ZeroMaximumRetainedKeepsEverything() {
            var pool = new ItemPool();
            var items = new[] { pool.Rent(), pool.Rent(), pool.Rent() };

            foreach (var item in items)
                pool.Return(item);

            Assert.AreEqual(3, pool.Available);
            Assert.AreEqual(0, pool.Discarded);
        }

        [Test]
        public void RentCreatesWhenEmptyAndReusesAfterReturn() {
            var pool = new ItemPool();

            var first = pool.Rent();

            Assert.AreEqual(1, pool.Created);
            Assert.AreEqual(0, pool.Available);

            pool.Return(first);

            Assert.AreEqual(1, pool.Available);
            Assert.AreEqual(1, first.ResetCount);

            var second = pool.Rent();

            Assert.AreSame(first, second);
            Assert.AreEqual(1, pool.Created);
            Assert.AreEqual(0, pool.Available);
        }
    }
}
