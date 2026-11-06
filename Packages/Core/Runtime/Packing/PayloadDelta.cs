// Copyright (c) Spellbound Studios. All rights reserved.

using System;

namespace Spellbound.Core.Packing {
    public static class PayloadDelta {
        private const int ShortestSkip = 4;

        public static int LargestDeltaBytes(int currentLength) {
            if (currentLength < 0) {
                throw new ArgumentOutOfRangeException(nameof(currentLength));
            }

            long header = 2 * VarIntCodec.BytesOf((uint)currentLength);
            long pairs = currentLength / ShortestSkip + 1;
            var total = VarIntCodec.BytesOf((uint)currentLength) + currentLength + header * pairs;
            return total > int.MaxValue ? int.MaxValue : (int)total;
        }

        public static bool TryReadUnpackedLength(ReadOnlySpan<byte> delta, out int length) {
            if (VarIntCodec.TryRead(delta, out uint value, out _) && value <= int.MaxValue) {
                length = (int)value;
                return true;
            }

            length = 0;
            return false;
        }

        public static bool Pack(ReadOnlySpan<byte> baseline, ReadOnlySpan<byte> current, Span<byte> destination,
            out int written) {
            written = 0;
            var position = 0;
            var shared = Math.Min(baseline.Length, current.Length);

            if (!VarIntCodec.TryWrite(destination, (uint)current.Length, out var cursor)) {
                return false;
            }

            while (position < current.Length) {
                var skip = 0;

                while (position + skip < shared && baseline[position + skip] == current[position + skip]) {
                    skip++;
                }

                var literalStart = position + skip;
                var literalEnd = literalStart;

                while (literalEnd < current.Length) {
                    if (literalEnd < shared && baseline[literalEnd] == current[literalEnd]) {
                        var run = 1;

                        while (literalEnd + run < shared && baseline[literalEnd + run] == current[literalEnd + run]) {
                            run++;
                        }

                        if (run >= ShortestSkip) {
                            break;
                        }

                        literalEnd += run;
                        continue;
                    }

                    literalEnd++;
                }

                var literal = literalEnd - literalStart;

                if (!VarIntCodec.TryWrite(destination[cursor..], (uint)skip, out var skipBytes)) {
                    return false;
                }

                cursor += skipBytes;

                if (!VarIntCodec.TryWrite(destination[cursor..], (uint)literal, out var literalBytes)) {
                    return false;
                }

                cursor += literalBytes;

                if (cursor + literal > destination.Length) {
                    return false;
                }

                current.Slice(literalStart, literal).CopyTo(destination[cursor..]);
                cursor += literal;
                position = literalEnd;
            }

            written = cursor;
            return true;
        }

        public static bool TryUnpack(ReadOnlySpan<byte> baseline, ReadOnlySpan<byte> delta, Span<byte> destination,
            out int written) {
            written = 0;

            if (!VarIntCodec.TryRead(delta, out uint length, out var cursor) || length > (uint)destination.Length) {
                return false;
            }

            var position = 0;

            while (cursor < delta.Length) {
                if (!VarIntCodec.TryRead(delta[cursor..], out uint skip, out var skipBytes)) {
                    return false;
                }

                cursor += skipBytes;

                if (!VarIntCodec.TryRead(delta[cursor..], out uint literal, out var literalBytes)) {
                    return false;
                }

                cursor += literalBytes;

                if (skip > (uint)Math.Max(0, baseline.Length - position)) {
                    return false;
                }

                if (literal > (uint)(delta.Length - cursor)) {
                    return false;
                }

                if (skip + literal > length - (uint)position) {
                    return false;
                }

                if (skip > 0) {
                    baseline.Slice(position, (int)skip).CopyTo(destination[position..]);
                    position += (int)skip;
                }

                if (literal > 0) {
                    delta.Slice(cursor, (int)literal).CopyTo(destination[position..]);
                    position += (int)literal;
                    cursor += (int)literal;
                }
            }

            if ((uint)position != length) {
                return false;
            }

            written = position;
            return true;
        }
    }
}
