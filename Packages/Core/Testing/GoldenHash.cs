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
        private readonly bool _begun;

        private GoldenHash(ulong state) {
            _state = state;
            _begun = true;
        }

        public static GoldenHash Start() {
            return new GoldenHash(OffsetBasis);
        }

        public ulong Value => _begun ? _state : OffsetBasis;

        public string Hexadecimal => Value.ToString("x16", CultureInfo.InvariantCulture);

        public GoldenHash Append(ReadOnlySpan<byte> bytes) {
            var state = Value;

            for (var index = 0; index < bytes.Length; index++) {
                state ^= bytes[index];
                state *= Prime;
            }

            return new GoldenHash(state);
        }

        public GoldenHash Append(byte value) {
            return new GoldenHash((Value ^ value) * Prime);
        }

        public GoldenHash Append(int value) {
            return Append(unchecked((uint)value));
        }

        public GoldenHash Append(uint value) {
            var state = Value;

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
            var state = Value;

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
            var bytes = string.IsNullOrEmpty(value) ? Array.Empty<byte>() : Encoding.UTF8.GetBytes(value);
            return Append(bytes.Length).Append(new ReadOnlySpan<byte>(bytes));
        }

        public GoldenHash Append<T>(NativeArray<T> values) where T : unmanaged {
            if (!values.IsCreated || values.Length == 0) {
                return this;
            }

            var bytes = values.Reinterpret<byte>(UnsafeUtility.SizeOf<T>());
            var state = Value;

            for (var index = 0; index < bytes.Length; index++) {
                state ^= bytes[index];
                state *= Prime;
            }

            return new GoldenHash(state);
        }

        public override string ToString() {
            return Hexadecimal;
        }
    }
}
