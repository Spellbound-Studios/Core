// Copyright 2026 Spellbound Studio Inc.

using System.Collections.Generic;

namespace Spellbound.Core.ObjectPooling {
    /// <summary>
    /// Generic reusable object pool. Items are rented off the top of a stack — or freshly created when the pool
    /// is empty — and returned to be reset and parked for the next rent. The pooling mechanics live here once;
    /// a concrete pool only says how its type is created and reset. Reuse for any poolable reference type:
    /// behaviour containers, skill instances, throwaway gameplay objects, and so on.
    /// </summary>
    public abstract class ObjectPool<T> where T : class {
        private readonly Stack<T> _free = new();

        /// <summary>How many items are parked and rentable without allocating.</summary>
        public int Available => _free.Count;

        public int MaximumRetained { get; set; }

        /// <summary>Rent an item — popped off the top, or freshly <see cref="Create"/>d when the pool is empty.</summary>
        public T Rent() => _free.Count > 0 ? _free.Pop() : Create();

        /// <summary>Reset an item and park it for the next <see cref="Rent"/>.</summary>
        public void Return(T item) {
            if (MaximumRetained > 0 && _free.Count >= MaximumRetained) {
                Discard(item);

                return;
            }

            Reset(item);
            _free.Push(item);
        }

        public void Prewarm(int count) {
            var target = MaximumRetained > 0 && count > MaximumRetained ? MaximumRetained : count;

            while (_free.Count < target) {
                var item = Create();
                Reset(item);
                _free.Push(item);
            }
        }

        /// <summary>Build a fresh instance when the pool has none to hand out.</summary>
        protected abstract T Create();

        /// <summary>Return an item to a clean state before it is reused. No-op by default.</summary>
        protected virtual void Reset(T item) { }

        protected virtual void Discard(T item) { }
    }
}
