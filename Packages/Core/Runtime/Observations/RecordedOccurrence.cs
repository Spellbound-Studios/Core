// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Globalization;
using Spellbound.Core.Simulation;

namespace Spellbound.Core.Observations {
    public readonly struct RecordedOccurrence {
        public const int MaximumValues = 12;

        private readonly uint _value0;
        private readonly uint _value1;
        private readonly uint _value2;
        private readonly uint _value3;
        private readonly uint _value4;
        private readonly uint _value5;
        private readonly uint _value6;
        private readonly uint _value7;
        private readonly uint _value8;
        private readonly uint _value9;
        private readonly uint _value10;
        private readonly uint _value11;

        internal RecordedOccurrence(uint keyHash, Tick tick, ulong subject, ReadOnlySpan<uint> values, bool flagged) {
            if (values.Length > MaximumValues) {
                throw new ArgumentException(
                    "An occurrence records at most " + MaximumValues.ToString(CultureInfo.InvariantCulture)
                    + " values; " + values.Length.ToString(CultureInfo.InvariantCulture) + " were given.",
                    nameof(values));
            }

            KeyHash = keyHash;
            Tick = tick;
            Subject = subject;
            ValueCount = values.Length;
            Flagged = flagged;
            _value0 = values.Length > 0 ? values[0] : 0u;
            _value1 = values.Length > 1 ? values[1] : 0u;
            _value2 = values.Length > 2 ? values[2] : 0u;
            _value3 = values.Length > 3 ? values[3] : 0u;
            _value4 = values.Length > 4 ? values[4] : 0u;
            _value5 = values.Length > 5 ? values[5] : 0u;
            _value6 = values.Length > 6 ? values[6] : 0u;
            _value7 = values.Length > 7 ? values[7] : 0u;
            _value8 = values.Length > 8 ? values[8] : 0u;
            _value9 = values.Length > 9 ? values[9] : 0u;
            _value10 = values.Length > 10 ? values[10] : 0u;
            _value11 = values.Length > 11 ? values[11] : 0u;
        }

        public uint KeyHash { get; }

        public Tick Tick { get; }

        public ulong Subject { get; }

        public int ValueCount { get; }

        public bool Flagged { get; }

        public uint Value(int index) {
            if (index < 0 || index >= ValueCount) {
                throw new ArgumentOutOfRangeException(nameof(index), index,
                    "This occurrence holds " + ValueCount.ToString(CultureInfo.InvariantCulture) + " values.");
            }

            switch (index) {
                case 0:
                    return _value0;
                case 1:
                    return _value1;
                case 2:
                    return _value2;
                case 3:
                    return _value3;
                case 4:
                    return _value4;
                case 5:
                    return _value5;
                case 6:
                    return _value6;
                case 7:
                    return _value7;
                case 8:
                    return _value8;
                case 9:
                    return _value9;
                case 10:
                    return _value10;
                default:
                    return _value11;
            }
        }
    }
}
