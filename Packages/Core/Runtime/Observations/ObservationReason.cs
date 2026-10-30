// Copyright (c) Spellbound Studios. All rights reserved.

using System;

namespace Spellbound.Core.Observations {
    public readonly struct ObservationReason : IEquatable<ObservationReason> {
        public readonly uint Id;
        public readonly string Name;

        internal ObservationReason(uint id, string name) {
            Id = id;
            Name = name;
        }

        public static ObservationReason None => default;

        public bool IsNone => Id == 0;

        public bool Equals(ObservationReason other) => Id == other.Id;

        public override bool Equals(object obj) => obj is ObservationReason other && Equals(other);

        public override int GetHashCode() => (int)Id;

        public static bool operator ==(ObservationReason left, ObservationReason right) => left.Id == right.Id;

        public static bool operator !=(ObservationReason left, ObservationReason right) => left.Id != right.Id;

        public override string ToString() => Name ?? string.Empty;
    }
}
