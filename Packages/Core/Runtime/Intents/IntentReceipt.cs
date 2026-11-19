// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Globalization;
using Spellbound.Core.Observations;

namespace Spellbound.Core.Intents {
    public readonly struct IntentReceipt : IEquatable<IntentReceipt> {
        public readonly ObservationReason Reason;
        public readonly uint Count;

        public IntentReceipt(ObservationReason reason, uint count) {
            Reason = reason;
            Count = count;
        }

        public static IntentReceipt None => default;

        public bool Equals(IntentReceipt other) => Reason.Id == other.Reason.Id && Count == other.Count;

        public override bool Equals(object obj) => obj is IntentReceipt other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Reason.Id, Count);

        public static bool operator ==(IntentReceipt left, IntentReceipt right) => left.Equals(right);

        public static bool operator !=(IntentReceipt left, IntentReceipt right) => !left.Equals(right);

        public override string ToString() => Reason + " x" + Count.ToString(CultureInfo.InvariantCulture);
    }
}
