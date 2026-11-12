// Copyright (c) Spellbound Studios. All rights reserved.

using System;

namespace Spellbound.Core.Packing {
    internal static class VarIntCodec {
        public const int LargestUnsignedBytes = 5;
        public const int LargestUnsignedLongBytes = 10;

        public static int BytesOf(uint value) {
            var count = 1;

            while (value >= 0x80) {
                value >>= 7;
                count++;
            }

            return count;
        }

        public static int BytesOf(ulong value) {
            var count = 1;

            while (value >= 0x80) {
                value >>= 7;
                count++;
            }

            return count;
        }

        public static bool TryWrite(Span<byte> destination, uint value, out int written) {
            written = 0;

            while (value >= 0x80) {
                if (written >= destination.Length) {
                    written = 0;
                    return false;
                }

                destination[written++] = (byte)(value | 0x80);
                value >>= 7;
            }

            if (written >= destination.Length) {
                written = 0;
                return false;
            }

            destination[written++] = (byte)value;
            return true;
        }

        public static bool TryWrite(Span<byte> destination, ulong value, out int written) {
            written = 0;

            while (value >= 0x80) {
                if (written >= destination.Length) {
                    written = 0;
                    return false;
                }

                destination[written++] = (byte)(value | 0x80);
                value >>= 7;
            }

            if (written >= destination.Length) {
                written = 0;
                return false;
            }

            destination[written++] = (byte)value;
            return true;
        }

        public static bool TryRead(ReadOnlySpan<byte> source, out uint value, out int read) {
            value = 0;
            read = 0;
            var shift = 0;

            while (read < source.Length && read < LargestUnsignedBytes) {
                var current = source[read++];
                value |= (uint)(current & 0x7F) << shift;

                if ((current & 0x80) == 0) {
                    if (read == LargestUnsignedBytes && current > 0x0F) {
                        break;
                    }

                    return true;
                }

                shift += 7;
            }

            value = 0;
            read = 0;
            return false;
        }

        public static bool TryRead(ReadOnlySpan<byte> source, out ulong value, out int read) {
            value = 0;
            read = 0;
            var shift = 0;

            while (read < source.Length && read < LargestUnsignedLongBytes) {
                var current = source[read++];
                value |= (ulong)(current & 0x7F) << shift;

                if ((current & 0x80) == 0) {
                    if (read == LargestUnsignedLongBytes && current > 0x01) {
                        break;
                    }

                    return true;
                }

                shift += 7;
            }

            value = 0;
            read = 0;
            return false;
        }

        public static uint ZigZag(int value) => (uint)((value << 1) ^ (value >> 31));

        public static int UnZigZag(uint value) => (int)(value >> 1) ^ -(int)(value & 1);

        public static ulong ZigZag(long value) => (ulong)((value << 1) ^ (value >> 63));

        public static long UnZigZag(ulong value) => (long)(value >> 1) ^ -(long)(value & 1);
    }
}
