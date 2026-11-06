// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Globalization;

namespace Spellbound.Core.Simulation {
    public readonly struct TickRate : IEquatable<TickRate> {
        public readonly double TicksPerSecond;

        private TickRate(double ticksPerSecond) {
            TicksPerSecond = ticksPerSecond;
        }

        public double StepSeconds => 1.0 / TicksPerSecond;

        public static TickRate PerSecond(double ticksPerSecond) {
            if (double.IsNaN(ticksPerSecond) || double.IsInfinity(ticksPerSecond) || ticksPerSecond <= 0) {
                throw new ArgumentOutOfRangeException(
                    nameof(ticksPerSecond), ticksPerSecond, "A tick rate must be a positive, finite number.");
            }

            return new TickRate(ticksPerSecond);
        }

        public TickSpan FromSeconds(double seconds) {
            if (double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds < 0) {
                throw new ArgumentOutOfRangeException(
                    nameof(seconds), seconds, "A duration must be a finite number of seconds, zero or more.");
            }

            var rounded = Math.Round(seconds * TicksPerSecond, MidpointRounding.AwayFromZero);

            if (rounded >= ulong.MaxValue) {
                throw new ArgumentOutOfRangeException(
                    nameof(seconds), seconds, "The duration is longer than a tick span can hold at " + this + ".");
            }

            if (rounded == 0 && seconds > 0) {
                return new TickSpan(1);
            }

            return new TickSpan((ulong)rounded);
        }

        public double ToSeconds(TickSpan span) => span.Steps / TicksPerSecond;

        public bool Equals(TickRate other) => TicksPerSecond.Equals(other.TicksPerSecond);

        public override bool Equals(object obj) => obj is TickRate other && Equals(other);

        public override int GetHashCode() => TicksPerSecond.GetHashCode();

        public static bool operator ==(TickRate left, TickRate right) => left.Equals(right);

        public static bool operator !=(TickRate left, TickRate right) => !left.Equals(right);

        public override string ToString() => TicksPerSecond.ToString(CultureInfo.InvariantCulture) + " ticks/s";
    }
}
