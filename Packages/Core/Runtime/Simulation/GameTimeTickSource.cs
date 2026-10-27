// Copyright (c) Spellbound Studios. All rights reserved.

using System;

namespace Spellbound.Core.Simulation {
    public sealed class GameTimeTickSource : IFixedStepTickSource {
        private const long MicrosecondsPerSecond = 1_000_000L;

        public GameTimeTickSource(TickRate rate) {
            Rate = rate;
        }

        public TickRate Rate { get; }

        public long ElapsedMicroseconds { get; private set; }

        public double ElapsedSeconds => (double)ElapsedMicroseconds / MicrosecondsPerSecond;

        public void FixedStep(double stepSeconds) {
            if (double.IsNaN(stepSeconds) || double.IsInfinity(stepSeconds) || stepSeconds < 0) {
                throw new ArgumentOutOfRangeException(
                    nameof(stepSeconds), stepSeconds, "A fixed step must be a finite number of seconds, zero or more.");
            }

            ElapsedMicroseconds += (long)Math.Round(stepSeconds * MicrosecondsPerSecond);
        }

        public Tick Current() {
            var ticks = (decimal)ElapsedMicroseconds * (decimal)Rate.TicksPerSecond / MicrosecondsPerSecond;
            return new Tick((ulong)decimal.Floor(ticks));
        }
    }
}
