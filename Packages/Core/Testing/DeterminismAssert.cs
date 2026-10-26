// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Globalization;
using NUnit.Framework;

namespace Spellbound.Core.Testing {
    public static class DeterminismAssert {
        public static void GoldenHash(ulong expected, in GoldenHash actual, string region) {
            if (expected == actual.Value) {
                return;
            }

            throw new AssertionException("region '" + region + "': golden 0x"
                                         + expected.ToString("x16", CultureInfo.InvariantCulture) + ", actual 0x"
                                         + actual.Hex);
        }

        public static void GoldenHash(string expectedHex, in GoldenHash actual, string region) {
            GoldenHash(ParseHex(expectedHex), actual, region);
        }

        private static ulong ParseHex(string hex) {
            var digits = hex == null ? string.Empty : hex.Trim();

            if (digits.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) {
                digits = digits.Substring(2);
            }

            if (!ulong.TryParse(digits, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture,
                    out var value)) {
                throw new ArgumentException("'" + hex + "' is not a 64-bit hexadecimal hash.", nameof(hex));
            }

            return value;
        }
    }
}
