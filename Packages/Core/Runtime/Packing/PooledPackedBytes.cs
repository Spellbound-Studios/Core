// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Buffers;

namespace Spellbound.Core.Packing {
    public sealed class PooledPackedBytes : IDisposable {
        private byte[] _array;
        private readonly int _length;

        internal PooledPackedBytes(byte[] array, int length) {
            _array = array;
            _length = length;
        }

        public byte[] Array => _array ?? System.Array.Empty<byte>();

        public int Length => _array == null ? 0 : _length;

        public ReadOnlySpan<byte> Bytes => _array == null ? default : new ReadOnlySpan<byte>(_array, 0, _length);

        public void Dispose() {
            var array = _array;

            if (array == null) {
                return;
            }

            _array = null;
            ArrayPool<byte>.Shared.Return(array);
        }
    }
}
