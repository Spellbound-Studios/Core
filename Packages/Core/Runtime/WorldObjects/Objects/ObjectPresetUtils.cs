// Copyright 2026 Spellbound Studio Inc.

using Spellbound.Core.EntityPrefabs;
using Spellbound.Core.Hashing;
using Spellbound.Core.Logging;
using Spellbound.Core.Tooling;
using Unity.Entities;

namespace Spellbound.Core.Objects {
    public static class ObjectPresetUtils {
        /// <summary>
        /// The preset for a stable hash, or null.
        /// </summary>
        public static ObjectPreset ResolvePreset(this uint hash) => PresetRegistry.ResolvePreset(hash);

        /// <summary>
        /// The preset for an asset-GUID string, or null. Bridges the GUID to its stable hash.
        /// </summary>
        public static ObjectPreset ResolvePresetGuid(this string uid) =>
                string.IsNullOrEmpty(uid) ? null : PresetRegistry.ResolvePreset(StableHash.Fnv1A32(uid));

        public static bool TryGetEntityPrefab(this uint hash, out Entity entity) {
            if (!SingletonManager.TryGetSingletonInstance(out EntityPrefabRegistry registry)) {
                Log.Warn("EntityPrefabRegistry not found");
                entity = Entity.Null;

                return false;
            }

            if (registry.PrefabLookup.TryGetValue(hash, out entity))
                return true;

            Log.Warn($"No entity bakePrefab found for preset {hash.ResolvePreset()}");

            return false;
        }
    }
}
