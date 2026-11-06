// Copyright 2026 Spellbound Studio Inc.

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Spellbound.Core.Tooling {
    /// <summary>
    /// Fixed-length observable array intended for containers whose sizes change rarely. Cannot add, insert, or remove.
    /// </summary>
    [Serializable]
    public sealed class ObservableArray<T> : IList<T>, ISerializationCallbackReceiver {
        [SerializeField] private List<T> serializedItems;
        [SerializeField] private int length;

        /// <summary>
        /// Fired after any mutating operations: set, clear, resize.
        /// </summary>
        public delegate void ObservableArrayChanged(ObservableArrayChange<T> change);

        public event ObservableArrayChanged onChanged;

        private T[] _inner;

        /// <summary>
        /// Creates an observable array with elements initialized as default.
        /// </summary>
        public ObservableArray(int size) {
            _inner = new T[size];
            length = size;

            serializedItems = new List<T>(size);

            for (var i = 0; i < size; i++)
                serializedItems.Add(default);
        }

        public void OnBeforeSerialize() {
            length = _inner.Length;
            serializedItems.Clear();
            serializedItems.Capacity = _inner.Length;

            foreach (var t in _inner)
                serializedItems.Add(t);
        }

        public void OnAfterDeserialize() {
            serializedItems ??= new List<T>();
            _inner = new T[serializedItems.Count];
            serializedItems.CopyTo(_inner);
            length = _inner.Length;
        }

        /// <summary>
        /// Random-access getter / setter.
        /// </summary>
        public T this[int index] {
            get {
                if (index < 0 || index >= length)
                    throw new IndexOutOfRangeException();

                return _inner[index];
            }
            set {
                if (index < 0 || index >= length)
                    throw new IndexOutOfRangeException();

                if (EqualityComparer<T>.Default.Equals(_inner[index], value))
                    return;

                _inner[index] = value;

                InvokeChange(new ObservableArrayChange<T>(ObservableArrayOperation.Set, value, index));
            }
        }

        public int Length {
            get => _inner.Length;
            set => Resize(value);
        }

        /// <summary>
        /// Current number of slots.
        /// </summary>
        public int Count => _inner.Length;

        public bool IsReadOnly => false;

        /// <summary>
        /// Sets every slot back to default and informs listeners once.
        /// </summary>
        public void Clear() {
            Array.Clear(_inner, 0, length);
            InvokeChange(new ObservableArrayChange<T>(ObservableArrayOperation.Clear));
        }

        /// <summary>
        /// Changes the array capacity. Copies existing elements.
        /// </summary>
        public void Resize(int newSize) {
            if (newSize < 0)
                throw new ArgumentOutOfRangeException(nameof(newSize));

            if (newSize == _inner.Length)
                return;

            Array.Resize(ref _inner, newSize);

            // Sync the inspector list.
            serializedItems.Clear();
            serializedItems.Capacity = newSize;

            foreach (var val in _inner)
                serializedItems.Add(val);

            length = _inner.Length;
            InvokeChange(new ObservableArrayChange<T>(ObservableArrayOperation.Resize));
        }

        public T[] ToArray() {
            var result = new T[_inner.Length];
            Array.Copy(_inner, result, _inner.Length);

            return result;
        }

        public void CopyTo(T[] array, int arrayIndex) {
            if (array == null)
                throw new ArgumentNullException(nameof(array));

            if (arrayIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(arrayIndex));

            if (array.Length - arrayIndex < length)
                throw new ArgumentException("Destination array is not long enough.");

            Array.Copy(_inner, 0, array, arrayIndex, _inner.Length);
        }

        public int IndexOf(T item) {
            for (var i = 0; i < _inner.Length; i++) {
                if (EqualityComparer<T>.Default.Equals(_inner[i], item))
                    return i;
            }

            return -1;
        }

        public bool Contains(T item) => IndexOf(item) != -1;

        public void Add(T item) =>
                throw new NotSupportedException(
                    "ObservableArray does not support insertion. Use Resize and Set operations instead.");

        public void Insert(int index, T item) =>
                throw new NotSupportedException(
                    "ObservableArray does not support insertion. Use Resize and Set operations instead.");

        public bool Remove(T item) =>
                throw new NotSupportedException(
                    "ObservableArray does not support insertion. Use Resize and Set operations instead.");

        public void RemoveAt(int index) =>
                throw new NotSupportedException(
                    "ObservableArray does not support insertion. Use Resize and Set operations instead.");

        public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)_inner).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _inner.GetEnumerator();

        private void InvokeChange(ObservableArrayChange<T> change) => onChanged?.Invoke(change);
    }

    public enum ObservableArrayOperation {
        Set,
        Clear,
        Resize
    }

    /// <summary>
    /// Immutable payload describing a single mutation.
    /// </summary>
    public readonly struct ObservableArrayChange<T> {
        public ObservableArrayOperation Operation { get; }
        public T NewValue { get; }
        public int Index { get; }

        /// <summary>
        /// Constructor used when raising onChanged event.
        /// </summary>
        public ObservableArrayChange(ObservableArrayOperation operation, T newVal = default, int index = -1) {
            Operation = operation;
            NewValue = newVal;
            Index = index;
        }

        public override string ToString() => $"New: {NewValue}\nIndex: {Index}\nOperation: {Operation}";
    }
}