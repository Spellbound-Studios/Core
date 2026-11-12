// Copyright (c) Spellbound Studios. All rights reserved.

namespace Spellbound.Core.Observations {
    public readonly struct ObservationRatio {
        public readonly ObservationKey Key;
        public readonly double Before;
        public readonly double After;
        public readonly double Ratio;

        internal ObservationRatio(ObservationKey key, double before, double after) {
            Key = key;
            Before = before;
            After = after;
            Ratio = before == 0 ? double.NaN : after / before;
        }

        public override string ToString() => Key + " " + Before + " -> " + After + " (x" + Ratio + ")";
    }
}
