// Copyright 2026 Spellbound Studio Inc.

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Spellbound.Core.Sampling {
    [Serializable]
    public class WeightedTable<T> : ISerializationCallbackReceiver where T : class {
        [SerializeField] private List<WeightedEntry<T>> entries = new();
        [SerializeField, Min(0)] private int nothingWeight;

        private int[] _cumulativeWeights;
        private int _totalWeight;

        public IReadOnlyList<WeightedEntry<T>> Entries => entries ??= new List<WeightedEntry<T>>();

        public int NothingWeight => Math.Max(0, nothingWeight);

        public int TotalWeight {
            get {
                EnsureBuilt();

                return _totalWeight;
            }
        }

        public int PickIndex(int roll) {
            EnsureBuilt();

            if (_totalWeight <= 0 || roll < 0 || roll >= _totalWeight)
                return -1;

            var low = 0;
            var high = _cumulativeWeights.Length - 1;

            while (low < high) {
                var middle = (low + high) / 2;

                if (_cumulativeWeights[middle] > roll)
                    high = middle;
                else
                    low = middle + 1;
            }

            return low == entries.Count ? -1 : low;
        }

        public bool TryPick(int roll, out T candidate) {
            var index = PickIndex(roll);
            candidate = index >= 0 ? entries[index].candidate : null;

            return candidate != null;
        }

        public bool TryPick(System.Random rng, out T candidate) {
            var total = TotalWeight;

            if (total <= 0) {
                candidate = null;

                return false;
            }

            return TryPick(rng.Next(total), out candidate);
        }

        public List<T> Sample(int count, System.Random rng, bool withReplacement = false) {
            var result = new List<T>(Math.Max(count, 0));

            if (count <= 0)
                return result;

            if (withReplacement) {
                SampleWithReplacement(count, rng, result);

                return result;
            }

            SampleWithoutReplacement(count, rng, result);

            return result;
        }

        public void Define(IEnumerable<WeightedEntry<T>> definedEntries, int definedNothingWeight = 0) {
            entries = definedEntries != null
                    ? new List<WeightedEntry<T>>(definedEntries)
                    : new List<WeightedEntry<T>>();
            nothingWeight = Math.Max(0, definedNothingWeight);
            _cumulativeWeights = null;
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize() { }

        void ISerializationCallbackReceiver.OnAfterDeserialize() => _cumulativeWeights = null;

        private void SampleWithReplacement(int count, System.Random rng, List<T> result) {
            if (TotalWeight <= NothingWeight)
                return;

            for (var pick = 0; pick < count; pick++) {
                if (TryPick(rng, out var candidate))
                    result.Add(candidate);
            }
        }

        private void SampleWithoutReplacement(int count, System.Random rng, List<T> result) {
            var list = Entries;
            var remaining = new int[list.Count];
            var remainingTotal = NothingWeight;

            for (var index = 0; index < list.Count; index++) {
                remaining[index] = EffectiveWeight(index);
                remainingTotal += remaining[index];
            }

            for (var pick = 0; pick < count; pick++) {
                if (remainingTotal <= NothingWeight)
                    break;

                var roll = rng.Next(remainingTotal);
                var cumulative = 0;
                var picked = -1;

                for (var index = 0; index < remaining.Length; index++) {
                    cumulative += remaining[index];

                    if (roll >= cumulative)
                        continue;

                    picked = index;

                    break;
                }

                if (picked < 0)
                    continue;

                result.Add(list[picked].candidate);
                remainingTotal -= remaining[picked];
                remaining[picked] = 0;
            }
        }

        private void EnsureBuilt() {
            var list = Entries;

            if (_cumulativeWeights != null && _cumulativeWeights.Length == list.Count + 1)
                return;

            _cumulativeWeights = new int[list.Count + 1];
            var running = 0;

            for (var index = 0; index < list.Count; index++) {
                running += EffectiveWeight(index);
                _cumulativeWeights[index] = running;
            }

            running += NothingWeight;
            _cumulativeWeights[list.Count] = running;
            _totalWeight = running;
        }

        private int EffectiveWeight(int index) {
            var entry = entries[index];

            return entry.candidate != null && entry.weight > 0 ? entry.weight : 0;
        }
    }
}
