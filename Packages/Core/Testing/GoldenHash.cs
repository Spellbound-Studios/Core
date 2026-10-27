// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Globalization;
using System.Text;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Spellbound.Core.Testing {
    public readonly struct GoldenHash {
        private const ulong OffsetBasis = 14695981039346656037UL;
        private const ulong Prime = 1099511628211UL;

        private readonly ulong _state;

        private GoldenHash(ulong state) {
            _state = state;
        }

        public static GoldenHash Start() {
            return new GoldenHash(OffsetBasis);
        }

        public ulong Value => _state;

        public string Hex => _state.ToString("x16", CultureInfo.InvariantCulture);

        public GoldenHash Append(ReadOnlySpan<byte> bytes) {
            var state = _state;

            for (var index = 0; index < bytes.Length; index++) {
                state ^= bytes[index];
                state *= Prime;
            }

            return new GoldenHash(state);
        }

        public GoldenHash Append(byte value) {
            return new GoldenHash((_state ^ value) * Prime);
        }

        public GoldenHash Append(int value) {
            return Append(unchecked((uint)value));
        }

        public GoldenHash Append(uint value) {
            var state = _state;

            for (var shift = 0; shift < 32; shift += 8) {
                state ^= (byte)(value >> shift);
                state *= Prime;
            }

            return new GoldenHash(state);
        }

        public GoldenHash Append(long value) {
            return Append(unchecked((ulong)value));
        }

        public GoldenHash Append(ulong value) {
            var state = _state;

            for (var shift = 0; shift < 64; shift += 8) {
                state ^= (byte)(value >> shift);
                state *= Prime;
            }

            return new GoldenHash(state);
        }

        public GoldenHash Append(float value) {
            return Append(BitConverter.SingleToInt32Bits(value));
        }

        public GoldenHash Append(double value) {
            return Append(BitConverter.DoubleToInt64Bits(value));
        }

        public GoldenHash Append(string value) {
            if (string.IsNullOrEmpty(value)) {
                return this;
            }

            return Append(new ReadOnlySpan<byte>(Encoding.UTF8.GetBytes(value)));
        }

        public GoldenHash Append<T>(NativeArray<T> values) where T : unmanaged {
            if (!values.IsCreated || values.Length == 0) {
                return this;
            }

            var bytes = values.Reinterpret<byte>(UnsafeUtility.SizeOf<T>());
            var state = _state;

            for (var index = 0; index < bytes.Length; index++) {
                state ^= bytes[index];
                state *= Prime;
            }

            return new GoldenHash(state);
        }

        public override string ToString() {
            return Hex;
        }
    }
}
