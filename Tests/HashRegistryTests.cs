// Copyright 2026 Spellbound Studio Inc.

using NUnit.Framework;
using Spellbound.Core.Registries;
using UnityEngine.TestTools;

namespace Spellbound.Core.Tests {
    public class HashRegistryTests {
        private class Entry : IRegistryEntry {
            public uint Hash { get; set; }
            public string Name { get; set; }
        }

        [Test]
        public void AddAndTryGetResolvesByHash() {
            var registry = new HashRegistry<Entry>();
            var entry = new Entry { Hash = 42u, Name = "answer" };

            registry.Add(entry);

            Assert.AreEqual(1, registry.Count);
            Assert.IsTrue(registry.TryGet(42u, out var resolved));
            Assert.AreSame(entry, resolved);
            Assert.IsTrue(registry.Contains(42u));
        }

        [Test]
        public void ZeroHashIsRejectedAsReservedNull() {
            LogAssert.ignoreFailingMessages = true;
            var registry = new HashRegistry<Entry>();

            registry.Add(new Entry { Hash = 0u });

            Assert.AreEqual(0, registry.Count);
            Assert.IsFalse(registry.TryGet(0u, out _));
            Assert.IsFalse(registry.Contains(0u));
        }

        [Test]
        public void CollisionKeepsFirstEntry() {
            LogAssert.ignoreFailingMessages = true;
            var registry = new HashRegistry<Entry>();
            var first = new Entry { Hash = 7u, Name = "first" };

            registry.Add(first);
            registry.Add(new Entry { Hash = 7u, Name = "second" });

            Assert.AreEqual(1, registry.Count);
            Assert.AreSame(first, registry.Get(7u));
        }

        [Test]
        public void GetMissingHashReturnsNull() {
            LogAssert.ignoreFailingMessages = true;
            var registry = new HashRegistry<Entry>();

            Assert.IsNull(registry.Get(99u));
        }

        [Test]
        public void ClearEmptiesRegistry() {
            var registry = new HashRegistry<Entry>();
            registry.Add(new Entry { Hash = 1u });
            registry.Add(new Entry { Hash = 2u });

            registry.Clear();

            Assert.AreEqual(0, registry.Count);
            Assert.IsEmpty(registry.All);
        }
    }
}
