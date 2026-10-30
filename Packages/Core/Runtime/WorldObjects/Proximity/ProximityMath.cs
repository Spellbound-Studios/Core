// Copyright 2026 Spellbound Studio Inc.

using Unity.Burst;
using Unity.Mathematics;

namespace Spellbound.Core.Tooling {
    [BurstCompile]
    public static class ProximityMath {
        [BurstCompile]
        public static ProximityChange IsWithinActivationRange(in float3 a, in float3 b, in float2 thresholds) {
            var distSq = math.distancesq(a, b);

            if (distSq < thresholds.x * thresholds.x)
                return ProximityChange.Whitelist;

            return distSq > thresholds.y * thresholds.y
                    ? ProximityChange.Blacklist
                    : ProximityChange.None;
        }
    }
}