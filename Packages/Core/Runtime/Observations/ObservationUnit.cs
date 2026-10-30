// Copyright (c) Spellbound Studios. All rights reserved.

using System;

namespace Spellbound.Core.Observations {
    public readonly struct ObservationUnit : IEquatable<ObservationUnit> {
        private const string CountName = "count";

        private readonly string _name;

        private ObservationUnit(string name) {
            _name = name;
        }

        public static ObservationUnit Count => new ObservationUnit(CountName);

        public static ObservationUnit Milliseconds => new ObservationUnit("ms");

        public static ObservationUnit Bytes => new ObservationUnit("bytes");

        public static ObservationUnit Seconds => new ObservationUnit("s");

        public static ObservationUnit Ratio => new ObservationUnit("ratio");

        public string Name => _name ?? CountName;

        public static ObservationUnit Named(string name) {
            if (string.IsNullOrWhiteSpace(name)) {
                throw new ArgumentException("An observation unit needs a name.", nameof(name));
            }

            return new ObservationUnit(name.Trim());
        }

        public bool Equals(ObservationUnit other) => string.Equals(Name, other.Name, StringComparison.Ordinal);

        public override bool Equals(object obj) => obj is ObservationUnit other && Equals(other);

        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Name);

        public static bool operator ==(ObservationUnit left, ObservationUnit right) => left.Equals(right);

        public static bool operator !=(ObservationUnit left, ObservationUnit right) => !left.Equals(right);

        public override string ToString() => Name;
    }
}
