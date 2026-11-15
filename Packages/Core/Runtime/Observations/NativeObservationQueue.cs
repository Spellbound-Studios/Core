// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Threading;
using Unity.Collections.LowLevel.Unsafe;

namespace Spellbound.Core.Observations {
    public sealed unsafe class NativeObservationQueue : IDisposable {
        private readonly ObservationContext _context;
        private NativeObservationEntry* _entries;
        private long* _head;
        private long* _tail;
        private long* _dropped;

        internal NativeObservationQueue(ObservationContext context, int capacity) {
            if (capacity < 1) {
                throw new ArgumentOutOfRangeException(nameof(capacity), capacity,
                    "A native observation queue needs room for at least one entry.");
            }

            _context = context ?? throw new ArgumentNullException(nameof(context));
            Capacity = capacity;
            _entries = (NativeObservationEntry*)UnsafeUtility.Malloc(
                (long)capacity * UnsafeUtility.SizeOf<NativeObservationEntry>(),
                UnsafeUtility.AlignOf<NativeObservationEntry>(), Unity.Collections.Allocator.Persistent);
            _head = AllocatePosition();
            _tail = AllocatePosition();
            _dropped = AllocatePosition();
        }

        public int Capacity { get; }

        public ObservationContext Context => _context;

        public bool IsDisposed => _entries == null;

        public NativeObservationWriter Writer => _entries == null
            ? default
            : new NativeObservationWriter(_entries, Capacity, _head, _tail, _dropped);

        public int Drain() {
            if (_entries == null) {
                return 0;
            }

            var head = *_head;
            var tail = Interlocked.Read(ref *_tail);
            var end = Math.Min(tail, head + Capacity);

            for (var index = head; index < end; index++) {
                _context.ApplyQueued(in _entries[index % Capacity]);
            }

            *_head = tail;
            var dropped = Interlocked.Exchange(ref *_dropped, 0L);

            if (dropped > 0) {
                CoreObservations.QueueDropped.Add(_context, dropped);
            }

            return (int)(end - head);
        }

        public void Dispose() {
            if (_entries == null) {
                return;
            }

            UnsafeUtility.Free(_entries, Unity.Collections.Allocator.Persistent);
            UnsafeUtility.Free(_head, Unity.Collections.Allocator.Persistent);
            UnsafeUtility.Free(_tail, Unity.Collections.Allocator.Persistent);
            UnsafeUtility.Free(_dropped, Unity.Collections.Allocator.Persistent);
            _entries = null;
            _head = null;
            _tail = null;
            _dropped = null;
        }

        private static long* AllocatePosition() {
            var position = (long*)UnsafeUtility.Malloc(sizeof(long), UnsafeUtility.AlignOf<long>(),
                Unity.Collections.Allocator.Persistent);
            *position = 0L;
            return position;
        }
    }
}
