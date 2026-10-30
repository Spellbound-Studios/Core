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
                                         + actual.Hexadecimal);
        }

        public static void GoldenHash(string expectedHexadecimal, in GoldenHash actual, string region) {
            GoldenHash(ParseHexadecimal(expectedHexadecimal), actual, region);
        }

        private static ulong ParseHexadecimal(string hexadecimal) {
            var digits = hexadecimal == null ? string.Empty : hexadecimal.Trim();

            if (digits.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) {
                digits = digits.Substring(2);
            }

            if (!ulong.TryParse(digits, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture,
                    out var value)) {
                throw new ArgumentException("'" + hexadecimal + "' is not a 64-bit hexadecimal hash.",
                    nameof(hexadecimal));
            }

            return value;
        }
    }
}
