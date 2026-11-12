// Copyright (c) Spellbound Studios. All rights reserved.

using System;

namespace Spellbound.Core.Packing {
    public readonly struct Quantization {
        public float Minimum { get; }
        public float Maximum { get; }
        public int BitCount { get; }

        private Quantization(float minimum, float maximum, int bitCount) {
            Minimum = minimum;
            Maximum = maximum;
            BitCount = bitCount;
        }

        public static Quantization Linear(float minimum, float maximum, int bitCount) {
            if (bitCount < 1 || bitCount > 32) {
                throw new ArgumentOutOfRangeException(nameof(bitCount), "A quantisation uses 1 to 32 bits.");
            }

            if (!(maximum > minimum)) {
                throw new ArgumentOutOfRangeException(nameof(maximum), "The maximum must be above the minimum.");
            }

            if (float.IsNaN(minimum) || float.IsInfinity(minimum) || float.IsInfinity(maximum)) {
                throw new ArgumentOutOfRangeException(nameof(minimum), "The range must be finite.");
            }

            return new Quantization(minimum, maximum, bitCount);
        }

        public uint LargestCode => BitCount == 32 ? uint.MaxValue : (1u << BitCount) - 1u;

        public float Step => (float)(((double)Maximum - Minimum) / LargestCode);

        public uint Quantize(float value) {
            if (BitCount == 0) {
                throw new InvalidOperationException("A default Quantization has no range; make one with Linear.");
            }

            if (float.IsNaN(value)) {
                return 0;
            }

            var clamped = Math.Min(Math.Max((double)value, Minimum), Maximum);
            var unit = (clamped - Minimum) / ((double)Maximum - Minimum);
            return (uint)Math.Round(unit * LargestCode, MidpointRounding.AwayFromZero);
        }

        public float Dequantize(uint code) {
            if (BitCount == 0) {
                throw new InvalidOperationException("A default Quantization has no range; make one with Linear.");
            }

            var clamped = Math.Min(code, LargestCode);
            return (float)(Minimum + ((double)Maximum - Minimum) * (clamped / (double)LargestCode));
        }
    }
}
