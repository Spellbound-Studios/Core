// Copyright 2026 Spellbound Studio Inc.

using System;
using UnityEngine;

namespace Spellbound.Core.Sampling {
    [Serializable]
    public struct WeightedEntry<T> {
        public T candidate;
        [Min(0)] public int weight;
    }
}
