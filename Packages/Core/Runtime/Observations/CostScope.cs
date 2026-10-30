// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Diagnostics;

namespace Spellbound.Core.Observations {
    public readonly struct CostScope : IDisposable {
        private readonly Cost _cost;
        private readonly ObservationContext _context;
        private readonly long _startTimestamp;

        internal CostScope(Cost cost, ObservationContext context) {
            _cost = cost;
            _context = context;
            _startTimestamp = Stopwatch.GetTimestamp();
        }

        public void Dispose() {
            if (_cost == null) {
                return;
            }

            var elapsedTicks = Stopwatch.GetTimestamp() - _startTimestamp;
            _cost.Record(_context, elapsedTicks * 1000.0 / Stopwatch.Frequency);
        }
    }
}
