// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using Spellbound.Core.Hashing;

namespace Spellbound.Core.Observations {
    public readonly struct ObservationKey : IEquatable<ObservationKey> {
        public readonly string Name;
        public readonly string Package;
        public readonly uint Hash;

        private ObservationKey(string name, string package) {
            Name = name;
            Package = package;
            Hash = StableHash.Fnv1A32(name);
        }

        public bool IsNone => Name == null;

        public static ObservationKey Parse(string text) {
            if (!TryParse(text, out var key, out var error)) {
                throw new ArgumentException(error, nameof(text));
            }

            return key;
        }

        public static bool TryParse(string text, out ObservationKey key, out string error) {
            key = default;

            if (string.IsNullOrEmpty(text)) {
                error = "An observation key must not be empty.";
                return false;
            }

            var segments = text.Split('.');

            if (segments.Length < 2) {
                error = "The observation key '" + text + "' needs at least two segments joined by dots,"
                        + " as in package.subject.name.";
                return false;
            }

            foreach (var segment in segments) {
                if (!IsSegment(segment)) {
                    error = "The observation key '" + text + "' has the segment '" + segment
                            + "'; a segment is lower-case letters, digits and underscores and starts with a letter.";
                    return false;
                }
            }

            key = new ObservationKey(text, segments[0]);
            error = null;
            return true;
        }

        private static bool IsSegment(string segment) {
            if (segment.Length == 0 || segment[0] < 'a' || segment[0] > 'z') {
                return false;
            }

            foreach (var character in segment) {
                var lower = character >= 'a' && character <= 'z';
                var digit = character >= '0' && character <= '9';

                if (!lower && !digit && character != '_') {
                    return false;
                }
            }

            return true;
        }

        public bool Equals(ObservationKey other) => string.Equals(Name, other.Name, StringComparison.Ordinal);

        public override bool Equals(object obj) => obj is ObservationKey other && Equals(other);

        public override int GetHashCode() => (int)Hash;

        public static bool operator ==(ObservationKey left, ObservationKey right) => left.Equals(right);

        public static bool operator !=(ObservationKey left, ObservationKey right) => !left.Equals(right);

        public override string ToString() => Name ?? string.Empty;
    }
}
