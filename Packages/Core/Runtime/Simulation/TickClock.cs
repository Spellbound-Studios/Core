// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using Spellbound.Core.Logging;
using Spellbound.Core.Observations;

namespace Spellbound.Core.Simulation {
    public sealed class TickClock {
        private readonly ObservationContext _observations;
        private TickRate _lastSourceRate;
        private bool _rateChangeLogged;

        public TickClock(ITickSource source, ObservationContext observations) {
            Source = source ?? throw new ArgumentNullException(nameof(source));
            _observations = observations ?? throw new ArgumentNullException(nameof(observations));
            Rate = source.Rate;

            if (!(Rate.TicksPerSecond > 0)) {
                throw new ArgumentException("A tick source needs a rate made by TickRate.PerSecond.", nameof(source));
            }

            _lastSourceRate = Rate;
            Current = source.Current();
        }

        public event Action<Tick> Stepped;

        public ITickSource Source { get; }

        public TickRate Rate { get; }

        public ObservationContext Observations => _observations;

        public Tick Current { get; private set; }

        public double ElapsedSeconds => Rate.ToSeconds(new TickSpan(Current.Value));

        internal TickClockDriver Driver { get; set; }

        public bool Advance() {
            var sourceRate = Source.Rate;

            if (sourceRate != _lastSourceRate) {
                _lastSourceRate = sourceRate;

                if (!_observations.IsDiscard) {
                    CoreObservations.ClockRateChanged.Add(_observations,
                        CoreKit.Observations.Reason(sourceRate.ToString()));
                }

                if (!_rateChangeLogged) {
                    _rateChangeLogged = true;
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
                CoreObservations.ClockHeld.Add(_observations);
            }

            return false;
        }

        public void RaiseStepped() {
            Stepped?.Invoke(Current);
        }
    }
}
