// Copyright 2026 Spellbound Studio Inc.

namespace Spellbound.Core.Hashing {
    /// <summary>
    /// Canonical hashing for stable, deterministic ids. FNV-1a (offset 2166136261, prime 16777619) over a
    /// string produces the same value on every machine and build. Use this anywhere a string needs a compact,
    /// stable numeric id (registry keys, save / network ids) instead of re-implementing the algorithm inline.
    /// </summary>
    public static class StableHash {
        private const uint Fnv1AOffset = 2166136261u;
        private const uint Fnv1APrime = 16777619u;

        /// <summary>
        /// Full 32-bit FNV-1a of <paramref name="value"/>. Empty or null returns 0, which callers treat as the
        /// reserved "null / none" id.
        /// </summary>
        public static uint Fnv1A32(string value) {
            if (string.IsNullOrEmpty(value))
                return 0u;

            var hash = Fnv1AOffset;

            foreach (var t in value) {
                hash ^= t;
                hash *= Fnv1APrime;
            }

            return hash;
        }
    }
}
