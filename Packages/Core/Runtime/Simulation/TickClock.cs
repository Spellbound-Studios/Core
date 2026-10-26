// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using Spellbound.Core.Logging;

namespace Spellbound.Core.Simulation {
    public sealed class TickClock {
        public TickClock(ITickSource source) {
            Source = source ?? throw new ArgumentNullException(nameof(source));
            Rate = source.Rate;
            Current = source.Current();
        }

        public event Action<Tick> Stepped;

        public ITickSource Source { get; }

        public TickRate Rate { get; }

        public Tick Current { get; private set; }

        public ulong HeldSteps { get; private set; }

        public int RateChanges { get; private set; }

        public double ElapsedSeconds => Rate.ToSeconds(new TickSpan(Current.Value));

        internal TickClockDriver Driver { get; set; }

        public bool Advance() {
            var sourceRate = Source.Rate;

            if (sourceRate != Rate) {
                RateChanges++;

                if (RateChanges == 1) {
                    Log.Error("The tick source changed its rate from " + Rate + " to " + sourceRate
                        + "; the clock keeps " + Rate + ".");
                }
            }

            var answer = Source.Current();

            if (answer > Current) {
                Current = answer;
                return true;
            }

            if (answer < Current) {
                HeldSteps++;
            }

            return false;
        }

        public void RaiseStepped() {
            Stepped?.Invoke(Current);
        }
    }
}
