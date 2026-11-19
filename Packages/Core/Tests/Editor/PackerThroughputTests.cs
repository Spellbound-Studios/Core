// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Diagnostics;
using NUnit.Framework;
using Spellbound.Core.Logging;
using Spellbound.Core.Packing;

namespace Spellbound.Core.Tests {
    [Category("Measurement")]
    public class PackerThroughputTests {
        private const int Count = 1_000_000;
        private const int Runs = 5;

        [Test]
        public void TheOnePathIsMeasured() {
            var buffer = new byte[sizeof(long) * Count];

            Log.Emit(LogLevel.Info, "PackerThroughput: " + Count + " values, median of " + Runs + " runs, ms"
                                    + "; int write " + Median(() => WriteInts(buffer))
                                    + ", int read " + Median(() => ReadInts(buffer))
                                    + ", float write " + Median(() => WriteFloats(buffer))
                                    + ", float read " + Median(() => ReadFloats(buffer))
                                    + ", long write " + Median(() => WriteLongs(buffer))
                                    + ", long read " + Median(() => ReadLongs(buffer)));
        }

        private static string Median(Action action) {
            var samples = new double[Runs];
            action();

            for (var run = 0; run < Runs; run++) {
                var stopwatch = Stopwatch.StartNew();
                action();
                stopwatch.Stop();
                samples[run] = stopwatch.ElapsedTicks * 1000.0 / Stopwatch.Frequency;
            }

            Array.Sort(samples);
            return samples[Runs / 2].ToString("F2");
        }

        private static void WriteInts(byte[] buffer) {
            var span = buffer.AsSpan();

            for (var index = 0; index < Count; index++) {
                Packer.WriteInt(ref span, index);
            }
        }

        private static void ReadInts(byte[] buffer) {
            ReadOnlySpan<byte> span = buffer;
            var sum = 0;

            for (var index = 0; index < Count; index++) {
                sum += Packer.ReadInt(ref span);
            }

            Keep(sum);
        }

        private static void WriteFloats(byte[] buffer) {
            var span = buffer.AsSpan();

            for (var index = 0; index < Count; index++) {
                Packer.WriteFloat(ref span, index * 0.1f);
            }
        }

        private static void ReadFloats(byte[] buffer) {
            ReadOnlySpan<byte> span = buffer;
            var sum = 0f;

            for (var index = 0; index < Count; index++) {
                sum += Packer.ReadFloat(ref span);
            }

            Keep(sum);
        }

        private static void WriteLongs(byte[] buffer) {
            var span = buffer.AsSpan();

            for (var index = 0; index < Count; index++) {
                Packer.WriteLong(ref span, index);
            }
        }

        private static void ReadLongs(byte[] buffer) {
            ReadOnlySpan<byte> span = buffer;
            var sum = 0L;

            for (var index = 0; index < Count; index++) {
                sum += Packer.ReadLong(ref span);
            }

            Keep(sum);
        }

        private static long _kept;

        private static void Keep(double value) {
            _kept += (long)value;
        }
    }
}
