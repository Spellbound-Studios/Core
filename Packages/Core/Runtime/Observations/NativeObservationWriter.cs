// Copyright (c) Spellbound Studios. All rights reserved.

using System.Threading;
using Spellbound.Core.Simulation;
using Unity.Collections.LowLevel.Unsafe;

namespace Spellbound.Core.Observations {
    public unsafe struct NativeObservationWriter {
        [NativeDisableUnsafePtrRestriction]
        private readonly NativeObservationEntry* _entries;

        [NativeDisableUnsafePtrRestriction]
        private readonly long* _head;

        [NativeDisableUnsafePtrRestriction]
        private readonly long* _tail;

        [NativeDisableUnsafePtrRestriction]
        private readonly long* _dropped;

        private readonly int _capacity;

        internal NativeObservationWriter(NativeObservationEntry* entries, int capacity, long* head, long* tail,
            long* dropped) {
            _entries = entries;
            _capacity = capacity;
            _head = head;
            _tail = tail;
            _dropped = dropped;
        }

        public bool IsCreated => _entries != null;

        public void Count(uint keyHash, long amount) {
            Write(new NativeObservationEntry(keyHash, ObservationKind.Counter, (ulong)amount, 0UL));
        }

        public void Record(uint keyHash, Tick tick, ulong subject) {
            Write(new NativeObservationEntry(keyHash, ObservationKind.Occurrence, subject, tick.Value));
        }

        private void Write(in NativeObservationEntry entry) {
            if (_entries == null) {
                return;
            }

            var claimed = Interlocked.Increment(ref *_tail) - 1;

            if (claimed - *_head >= _capacity) {
                Interlocked.Increment(ref *_dropped);
                return;
            }

            _entries[claimed % _capacity] = entry;
        }
    }
}
