// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Collections.Generic;
using Unity.Collections;

namespace Spellbound.Core.Observations {
    public sealed class FlightRecorder : IDisposable {
        private NativeArray<RecorderCell> _cells;

        internal FlightRecorder(int capacity) {
            if (capacity < 1) {
                throw new ArgumentOutOfRangeException(nameof(capacity), capacity,
                    "A flight recorder needs at least one cell.");
            }

            _cells = new NativeArray<RecorderCell>(capacity, Allocator.Persistent);
            Capacity = capacity;
        }

        public int Capacity { get; }

        public int Count { get; private set; }

        public int Read(List<RecordedOccurrence> records) {
            throw new NotImplementedException();
        }

        public void Dispose() {
            if (_cells.IsCreated) {
                _cells.Dispose();
            }

            Count = 0;
        }
    }
}
