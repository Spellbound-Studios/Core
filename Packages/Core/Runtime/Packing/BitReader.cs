// Copyright (c) Spellbound Studios. All rights reserved.

using System;

namespace Spellbound.Core.Packing {
    public ref struct BitReader {
        private readonly ReadOnlySpan<byte> _buffer;
        private int _bitPosition;

        public BitReader(ReadOnlySpan<byte> buffer) {
            _buffer = buffer;
            _bitPosition = 0;
        }

        public int BitsRead => _bitPosition;

        public int BytesRead => (_bitPosition + 7) >> 3;

        public int BitsRemaining => (int)Math.Min(int.MaxValue, (long)_buffer.Length * 8 - _bitPosition);

        public bool TryReadBits(int bitCount, out uint value) {
            if (bitCount < 0 || bitCount > 32) {
                throw new ArgumentOutOfRangeException(nameof(bitCount), "A read takes 0 to 32 bits.");
            }

            value = 0;

            if (bitCount > BitsRemaining) {
                return false;
            }

            var remaining = bitCount;
            var shift = 0;

            while (remaining > 0) {
                var byteIndex = _bitPosition >> 3;
                var bitOffset = _bitPosition & 7;
                var take = Math.Min(8 - bitOffset, remaining);
                var mask = (1u << take) - 1u;
                value |= ((uint)(_buffer[byteIndex] >> bitOffset) & mask) << shift;
                shift += take;
                remaining -= take;
                _bitPosition += take;
            }

            return true;
        }

        public bool TryReadLongBits(int bitCount, out ulong value) {
            if (bitCount < 0 || bitCount > 64) {
                throw new ArgumentOutOfRangeException(nameof(bitCount), "A read takes 0 to 64 bits.");
            }

            value = 0;

            if (bitCount > BitsRemaining) {
                return false;
            }

            if (bitCount <= 32) {
                TryReadBits(bitCount, out var low32);
                value = low32;
                return true;
            }

            TryReadBits(32, out var low);
            TryReadBits(bitCount - 32, out var high);
            value = low | ((ulong)high << 32);
            return true;
        }

        public bool TryReadBool(out bool value) {
            var read = TryReadBits(1, out var bit);
            value = read && bit != 0;
            return read;
        }

        public bool TryReadVarUInt(out uint value) {
            value = 0;
            var start = _bitPosition;
            var shift = 0;

            for (var count = 0; count < VarIntCodec.LargestUnsignedBytes; count++) {
                if (!TryReadBits(8, out var current)) {
                    break;
                }

                value |= (current & 0x7Fu) << shift;

                if ((current & 0x80u) == 0) {
                    if (count == VarIntCodec.LargestUnsignedBytes - 1 && current > 0x0Fu) {
                        break;
                    }

                    return true;
                }

                shift += 7;
            }

            _bitPosition = start;
            value = 0;
            return false;
        }

        public bool TryReadVarInt(out int value) {
            var read = TryReadVarUInt(out var raw);
            value = read ? VarIntCodec.UnZigZag(raw) : 0;
            return read;
        }

        public bool TryReadVarULong(out ulong value) {
            value = 0;
            var start = _bitPosition;
            var shift = 0;

            for (var count = 0; count < VarIntCodec.LargestUnsignedLongBytes; count++) {
                if (!TryReadBits(8, out var current)) {
                    break;
                }

                value |= (ulong)(current & 0x7Fu) << shift;

                if ((current & 0x80u) == 0) {
                    if (count == VarIntCodec.LargestUnsignedLongBytes - 1 && current > 0x01u) {
                        break;
                    }

                    return true;
                }

                shift += 7;
            }

            _bitPosition = start;
            value = 0;
            return false;
        }

        public bool TryReadVarLong(out long value) {
            var read = TryReadVarULong(out var raw);
            value = read ? VarIntCodec.UnZigZag(raw) : 0;
            return read;
        }

        public bool TryReadQuantized(in Quantization quantization, out float value) {
            var read = TryReadBits(quantization.BitCount, out var code);
            value = read ? quantization.Dequantize(code) : 0f;
            return read;
        }

        public void Align() {
            _bitPosition = Math.Min(BytesRead * 8, _buffer.Length * 8);
        }
    }
}
