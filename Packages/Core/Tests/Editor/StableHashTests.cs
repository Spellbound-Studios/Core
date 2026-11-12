// Copyright 2026 Spellbound Studio Inc.

using NUnit.Framework;
using Spellbound.Core.Hashing;

namespace Spellbound.Core.Tests {
    public class StableHashTests {
        [Test]
        public void Fnv1A32MatchesCanonicalVectorAndReservesZeroForNullOrEmpty() {
            Assert.AreEqual(0xE40C292Cu, StableHash.Fnv1A32("a"));
            Assert.AreEqual(0u, StableHash.Fnv1A32(null));
            Assert.AreEqual(0u, StableHash.Fnv1A32(string.Empty));
        }
    }
}
