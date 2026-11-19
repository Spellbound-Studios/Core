// Copyright (c) Spellbound Studios. All rights reserved.

using System;

namespace Spellbound.Core.Packing {
    public ref struct BitWriter {
        private readonly Span<byte> _buffer;
        private int _bitPosition;

        public BitWriter(Span<byte> buffer) {
            _buffer = buffer;
            _bitPosition = 0;
        }

        public int BitsWritten => _bitPosition;

        public int BytesWritten => (_bitPosition + 7) >> 3;

        public int BitsRemaining => (int)Math.Min(int.MaxValue, (long)_buffer.Length * 8 - _bitPosition);

        public ReadOnlySpan<byte> Written => _buffer[..BytesWritten];

        public bool TryWriteBits(uint value, int bitCount) {
            if (bitCount < 0 || bitCount > 32) {
                throw new ArgumentOutOfRangeException(nameof(bitCount), "A write takes 0 to 32 bits.");
            }

            if (bitCount > BitsRemaining) {
                return false;
            }

            var remaining = bitCount;

            while (remaining > 0) {
                var byteIndex = _bitPosition >> 3;
                var bitOffset = _bitPosition & 7;
                var take = Math.Min(8 - bitOffset, remaining);
                var mask = (1u << take) - 1u;
                var bits = (byte)((value & mask) << bitOffset);

                if (bitOffset == 0) {
                    _buffer[byteIndex] = bits;
                }
                else {
                    _buffer[byteIndex] = (byte)(_buffer[byteIndex] | bits);
                }

                value >>= take;
                remaining -= take;
                _bitPosition += take;
            }

            return true;
        }

        public bool TryWriteLongBits(ulong value, int bitCount) {
            if (bitCount < 0 || bitCount > 64) {
                throw new ArgumentOutOfRangeException(nameof(bitCount), "A write takes 0 to 64 bits.");
            }

            if (bitCount > BitsRemaining) {
                return false;
            }

            if (bitCount <= 32) {
                return TryWriteBits((uint)value, bitCount);
            }

            TryWriteBits((uint)value, 32);
            return TryWriteBits((uint)(value >> 32), bitCount - 32);
        }

        public void WriteBits(uint value, int bitCount) {
            if (!TryWriteBits(value, bitCount)) {
                throw new ArgumentOutOfRangeException(nameof(bitCount),
                    "The buffer has no room for " + bitCount + " bits.");
            }
        }

        public void WriteLongBits(ulong value, int bitCount) {
            if (!TryWriteLongBits(value, bitCount)) {
                throw new ArgumentOutOfRangeException(nameof(bitCount),
                    "The buffer has no room for " + bitCount + " bits.");
            }
        }

        public void WriteBool(bool value) => WriteBits(value ? 1u : 0u, 1);

        public bool TryWriteBool(bool value) => TryWriteBits(value ? 1u : 0u, 1);

        public void WriteVarUInt(uint value) {
            if (!TryWriteVarUInt(value)) {
                throw new ArgumentOutOfRangeException(nameof(value), "The buffer has no room for the varint.");
            }
        }

        public bool TryWriteVarUInt(uint value) {
            if (VarIntCodec.BytesOf(value) * 8 > BitsRemaining) {
                return false;
            }

            while (value >= 0x80) {
                TryWriteBits((value & 0x7Fu) | 0x80u, 8);
                value >>= 7;
            }

            TryWriteBits(value, 8);
            return true;
        }

        public void WriteVarInt(int value) => WriteVarUInt(VarIntCodec.ZigZag(value));

        public bool TryWriteVarInt(int value) => TryWriteVarUInt(VarIntCodec.ZigZag(value));

        public void WriteVarULong(ulong value) {
            if (!TryWriteVarULong(value)) {
                throw new ArgumentOutOfRangeException(nameof(value), "The buffer has no room for the varint.");
            }
        }

        public bool TryWriteVarULong(ulong value) {
            if (VarIntCodec.BytesOf(value) * 8 > BitsRemaining) {
                return false;
            }

            while (value >= 0x80) {
                TryWriteBits((uint)((value & 0x7FUL) | 0x80UL), 8);
                value >>= 7;
            }

            TryWriteBits((uint)value, 8);
            return true;
        }

        public void WriteVarLong(long value) => WriteVarULong(VarIntCodec.ZigZag(value));

        public bool TryWriteVarLong(long value) => TryWriteVarULong(VarIntCodec.ZigZag(value));

        public void WriteQuantized(float value, in Quantization quantization) =>
            WriteBits(quantization.Quantize(value), quantization.BitCount);

        public bool TryWriteQuantized(float value, in Quantization quantization) =>
            TryWriteBits(quantization.Quantize(value), quantization.BitCount);

        public void Align() {
            _bitPosition = BytesWritten * 8;
        }
    }
}
