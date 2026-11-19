// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Globalization;

namespace Spellbound.Core.Observations {
    public readonly struct GuardPolicyValues {
        public static GuardPolicyValues Default { get; } = new GuardPolicyValues(3, 2, 5);

        public int Repairs { get; }
        public double WindowSeconds { get; }
        public double HoldSeconds { get; }

        public GuardPolicyValues(int repairs, double windowSeconds, double holdSeconds) {
            if (repairs < 1) {
                throw new ArgumentException(
                    "A guard policy allows at least one repair; " + repairs.ToString(CultureInfo.InvariantCulture)
                    + " was given.", nameof(repairs));
            }

            RequireSeconds(windowSeconds, nameof(windowSeconds));
            RequireSeconds(holdSeconds, nameof(holdSeconds));
            Repairs = repairs;
            WindowSeconds = windowSeconds;
            HoldSeconds = holdSeconds;
        }

        private static void RequireSeconds(double seconds, string parameter) {
            if (double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds < 0) {
                throw new ArgumentException(
                    "A guard policy's time is a finite number of seconds, zero or more; "
                    + seconds.ToString(CultureInfo.InvariantCulture) + " was given.", parameter);
            }
        }
    }
}
