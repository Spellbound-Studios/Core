// Copyright (c) Spellbound Studios. All rights reserved.

using UnityEngine;

namespace Spellbound.Core.Observations {
    [CreateAssetMenu(menuName = "Spellbound/Guard Policy", fileName = "GuardPolicy")]
    public sealed class GuardPolicy : ScriptableObject {
        public int repairs = GuardPolicyValues.Default.Repairs;
        public double windowSeconds = GuardPolicyValues.Default.WindowSeconds;
        public double holdSeconds = GuardPolicyValues.Default.HoldSeconds;

        public GuardPolicyValues Values => new GuardPolicyValues(repairs, windowSeconds, holdSeconds);
    }
}
