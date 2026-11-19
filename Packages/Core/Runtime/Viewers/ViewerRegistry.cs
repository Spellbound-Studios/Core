// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Spellbound.Core.Viewers {
    public sealed class ViewerRegistry {
        public const int DefaultCapacity = 4;

        private Viewer[] _viewers;
        private int _count;
        private uint _lastIssued;

        public ViewerRegistry() : this(DefaultCapacity) { }

        public ViewerRegistry(int capacity) {
            if (capacity < 0) {
                throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "Capacity cannot be negative.");
            }

            _viewers = new Viewer[capacity];
        }

        public event ViewerHandler Added;
        public event ViewerHandler Removed;
        public event ViewerHandler Moved;

        public int Count => _count;

        public ReadOnlySpan<Viewer> Viewers => new ReadOnlySpan<Viewer>(_viewers, 0, _count);

        public ViewerId Add(Vector3 position, float collisionRadius, float detailRadius) {
            RequirePosition(position);
            RequireRadius(collisionRadius, nameof(collisionRadius));
            RequireRadius(detailRadius, nameof(detailRadius));

            if (_lastIssued == uint.MaxValue) {
                throw new InvalidOperationException("The registry has issued every viewer id it can.");
            }

            if (_count == _viewers.Length) {
                Array.Resize(ref _viewers, Math.Max(DefaultCapacity, _viewers.Length * 2));
            }

            var added = new Viewer(new ViewerId(++_lastIssued), position, collisionRadius, detailRadius);
            _viewers[_count] = added;
            _count++;
            Added?.Invoke(in added);
            return added.Id;
        }

        public bool Move(ViewerId id, Vector3 position) {
            RequirePosition(position);
            var index = IndexOf(id);

            if (index < 0) {
                return false;
            }

            var moved = _viewers[index].At(position);
            _viewers[index] = moved;
            Moved?.Invoke(in moved);
            return true;
        }

        public bool Update(ViewerId id, Vector3 position, float collisionRadius, float detailRadius) {
            RequirePosition(position);
            RequireRadius(collisionRadius, nameof(collisionRadius));
            RequireRadius(detailRadius, nameof(detailRadius));
            var index = IndexOf(id);

            if (index < 0) {
                return false;
            }

            var updated = new Viewer(id, position, collisionRadius, detailRadius);
            _viewers[index] = updated;
            Moved?.Invoke(in updated);
            return true;
        }

        public bool Remove(ViewerId id) {
            var index = IndexOf(id);

            if (index < 0) {
                return false;
            }

            var removed = _viewers[index];
            var last = _count - 1;
            _viewers[index] = _viewers[last];
            _viewers[last] = default;
            _count = last;
            Removed?.Invoke(in removed);
            return true;
        }

        public void Read(List<Viewer> into) {
            if (into == null) {
                throw new ArgumentNullException(nameof(into));
            }

            into.Clear();

            for (var i = 0; i < _count; i++) {
                into.Add(_viewers[i]);
            }
        }

        private int IndexOf(ViewerId id) {
            if (id.IsNone) {
                return -1;
            }

            for (var i = 0; i < _count; i++) {
                if (_viewers[i].Id == id) {
                    return i;
                }
            }

            return -1;
        }

        private static void RequirePosition(Vector3 position) {
            if (!IsFinite(position.x) || !IsFinite(position.y) || !IsFinite(position.z)) {
                throw new ArgumentOutOfRangeException(nameof(position), position,
                    "A position has finite components.");
            }
        }

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

        private static void RequireRadius(float radius, string name) {
            if (!(radius >= 0f) || float.IsInfinity(radius)) {
                throw new ArgumentOutOfRangeException(name, radius, "A radius is a finite distance of zero or more.");
            }
        }
    }
}
