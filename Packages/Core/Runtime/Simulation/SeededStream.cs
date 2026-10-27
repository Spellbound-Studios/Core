// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Globalization;

namespace Spellbound.Core.Simulation {
    public struct SeededStream {
        public const int Algorithm = 1;

        private const ulong GoldenGamma = 0x9E3779B97F4A7C15UL;

        public readonly ulong Seed;

        private ulong _state0;
        private ulong _state1;
        private ulong _state2;
        private ulong _state3;

        public SeededStream(ulong seed) {
            Seed = seed;
            var position = seed;
            position += GoldenGamma;
            _state0 = SplitMix64Mix(position);
            position += GoldenGamma;
            _state1 = SplitMix64Mix(position);
            position += GoldenGamma;
            _state2 = SplitMix64Mix(position);
            position += GoldenGamma;
            _state3 = SplitMix64Mix(position);

            if (_state0 == 0 && _state1 == 0 && _state2 == 0 && _state3 == 0) {
                _state0 = 1;
            }
        }

        private SeededStream(ulong seed, ulong state0, ulong state1, ulong state2, ulong state3) {
            Seed = seed;
            _state0 = state0;
            _state1 = state1;
            _state2 = state2;
            _state3 = state3;
        }

        public static SeededStream FromState(ulong state0, ulong state1, ulong state2, ulong state3) {
            if (state0 == 0 && state1 == 0 && state2 == 0 && state3 == 0) {
                throw new ArgumentException("A stream state needs at least one nonzero word.");
            }

            return new SeededStream(0, state0, state1, state2, state3);
        }

        public ulong NextUnsigned() {
            var result = RotateLeft(_state1 * 5, 7) * 9;
            var shifted = _state1 << 17;
            _state2 ^= _state0;
            _state3 ^= _state1;
            _state1 ^= _state2;
            _state0 ^= _state3;
            _state2 ^= shifted;
            _state3 = RotateLeft(_state3, 45);
            return result;
        }

        public double NextDouble() {
            return (NextUnsigned() >> 11) * (1.0 / (1UL << 53));
        }

        public float NextFloat() {
            return (NextUnsigned() >> 40) * (1.0f / (1 << 24));
        }

        public int NextInt(int minimum, int maximum) {
            if (maximum <= minimum) {
                throw new ArgumentException("The maximum " + maximum + " must be greater than the minimum "
                                            + minimum + ".", nameof(maximum));
            }

            var range = (ulong)((long)maximum - minimum);
            var threshold = (ulong)(-(long)range) % range;
            ulong draw;

            do {
                draw = NextUnsigned();
            } while (draw < threshold);

            return (int)(minimum + (long)(draw % range));
        }

        public bool NextBool() {
            return (NextUnsigned() >> 63) != 0;
        }

        public SeededStream Fork(ulong key) {
            return new SeededStream(SplitMix64Mix(Seed ^ RotateLeft(key, 32) ^ GoldenGamma));
        }

        public override string ToString() {
            return Seed.ToString(CultureInfo.InvariantCulture);
        }

        private static ulong SplitMix64Mix(ulong value) {
            value = (value ^ (value >> 30)) * 0xBF58476D1CE4E5B9UL;
            value = (value ^ (value >> 27)) * 0x94D049BB133111EBUL;
            return value ^ (value >> 31);
        }

        private static ulong RotateLeft(ulong value, int count) {
            return (value << count) | (value >> (64 - count));
        }
    }
}
