// Copyright 2026 Spellbound Studio Inc.

using System.Collections.Generic;
using UnityEngine;

namespace Spellbound.Core.Sampling {
    public abstract class WeightedTableAsset<T> : ScriptableObject where T : class {
        [SerializeField] private WeightedTable<T> table = new();

        public WeightedTable<T> Table => table ??= new WeightedTable<T>();

        public bool TryPick(System.Random rng, out T candidate) => Table.TryPick(rng, out candidate);

        public List<T> Sample(int count, System.Random rng, bool withReplacement = false) =>
                Table.Sample(count, rng, withReplacement);
    }
}
