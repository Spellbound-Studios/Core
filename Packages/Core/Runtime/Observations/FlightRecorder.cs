// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Collections.Generic;
using System.Globalization;
using Spellbound.Core.Simulation;
using Unity.Collections;

namespace Spellbound.Core.Observations {
    public sealed class FlightRecorder : IDisposable {
        private const byte HeaderMark = 0x80;
        private const byte FlaggedMark = 0x40;
        private const byte ValueCountMask = 0x0F;
        private const int ValuesPerCell = 4;
        private const ulong HighPartMask = 0xFFFFFFFF00000000UL;
        private const ulong OneSpan = 1UL << 32;

        private NativeArray<RecorderCell> _cells;
        private NativeArray<byte> _marks;
        private int _oldest;

        internal FlightRecorder(int capacity) {
            if (capacity < 1) {
                throw new ArgumentOutOfRangeException(nameof(capacity), capacity,
                    "A flight recorder needs at least one cell.");
            }

            _cells = new NativeArray<RecorderCell>(capacity, Allocator.Persistent);
            _marks = new NativeArray<byte>(capacity, Allocator.Persistent);
            Capacity = capacity;
        }

        public int Capacity { get; }

        public int Count { get; private set; }

        public Tick LatestTick { get; private set; }

        internal int UsedCells { get; private set; }

        public int Read(List<RecordedOccurrence> records) {
            if (records == null) {
                throw new ArgumentNullException(nameof(records));
            }

            records.Clear();

            if (!_cells.IsCreated) {
                return 0;
            }

            Span<uint> values = stackalloc uint[RecordedOccurrence.MaximumValues];
            var index = _oldest;

            for (var record = 0; record < Count; record++) {
                var header = _cells[index];
                var mark = _marks[index];
                var valueCount = mark & ValueCountMask;
                var argumentCells = ArgumentCells(valueCount);

                for (var cell = 0; cell < argumentCells; cell++) {
                    var argument = _cells[(index + 1 + cell) % Capacity];
                    var first = cell * ValuesPerCell;
                    values[first] = argument.Key;
                    values[first + 1] = argument.TickLow;
                    values[first + 2] = (uint)argument.Subject;
                    values[first + 3] = (uint)(argument.Subject >> 32);
                }

                records.Add(new RecordedOccurrence(header.Key, Reconstruct(header.TickLow), header.Subject,
                    values.Slice(0, valueCount), (mark & FlaggedMark) != 0));
                index = (index + 1 + argumentCells) % Capacity;
            }

            return records.Count;
        }

        internal void Write(uint key, Tick tick, ulong subject, ReadOnlySpan<uint> values) {
            if (values.Length > RecordedOccurrence.MaximumValues) {
                throw new ArgumentException(
                    "An occurrence records at most "
                    + RecordedOccurrence.MaximumValues.ToString(CultureInfo.InvariantCulture) + " values; "
                    + values.Length.ToString(CultureInfo.InvariantCulture) + " were given.", nameof(values));
            }

            var argumentCells = ArgumentCells(values.Length);
            var needed = 1 + argumentCells;

            if (!_cells.IsCreated || needed > Capacity) {
                return;
            }

            while (Capacity - UsedCells < needed) {
                DropOldest();
            }

            var index = (_oldest + UsedCells) % Capacity;
            _cells[index] = new RecorderCell(key, (uint)tick.Value, subject);
            _marks[index] = (byte)(HeaderMark | values.Length);

            for (var cell = 0; cell < argumentCells; cell++) {
                var first = cell * ValuesPerCell;
                var at = (index + 1 + cell) % Capacity;
                _cells[at] = new RecorderCell(ValueAt(values, first), ValueAt(values, first + 1),
                    ValueAt(values, first + 2) | ((ulong)ValueAt(values, first + 3) << 32));
                _marks[at] = 0;
            }

            UsedCells += needed;
            Count++;

            if (tick > LatestTick) {
                LatestTick = tick;
            }
        }

        internal void FlagWindow(Tick from, Tick to) {
            if (!_cells.IsCreated) {
                return;
            }

            var index = _oldest;

            for (var record = 0; record < Count; record++) {
                var mark = _marks[index];
                var tick = Reconstruct(_cells[index].TickLow);

                if (tick >= from && tick <= to) {
                    _marks[index] = (byte)(mark | FlaggedMark);
                }

                index = (index + 1 + ArgumentCells(mark & ValueCountMask)) % Capacity;
            }
        }

        public void Dispose() {
            if (_cells.IsCreated) {
                _cells.Dispose();
            }

            if (_marks.IsCreated) {
                _marks.Dispose();
            }

            Count = 0;
            UsedCells = 0;
            _oldest = 0;
        }

        private void DropOldest() {
            var length = 1 + ArgumentCells(_marks[_oldest] & ValueCountMask);
            _oldest = (_oldest + length) % Capacity;
            UsedCells -= length;
            Count--;
        }

        private Tick Reconstruct(uint tickLow) {
            var latest = LatestTick.Value;
            var high = latest & HighPartMask;

            if (tickLow > (uint)latest && high >= OneSpan) {
                high -= OneSpan;
            }

            return new Tick(high | tickLow);
        }

        private static int ArgumentCells(int valueCount) => (valueCount + ValuesPerCell - 1) / ValuesPerCell;

        private static uint ValueAt(ReadOnlySpan<uint> values, int index) => index < values.Length ? values[index] : 0u;
    }
}
