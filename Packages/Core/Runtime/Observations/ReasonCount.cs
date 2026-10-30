// Copyright (c) Spellbound Studios. All rights reserved.

namespace Spellbound.Core.Observations {
    public readonly struct ReasonCount {
        public readonly ObservationReason Reason;
        public readonly long Count;

        internal ReasonCount(ObservationReason reason, long count) {
            Reason = reason;
            Count = count;
        }

        public override string ToString() => Reason + " x" + Count;
    }
}
