// Copyright 2026 Spellbound Studio Inc.

using Spellbound.Core.Tooling;
using Unity.Entities;

namespace Spellbound.Core.EntityPrefabs {
    /// <summary>
    /// Copies entity prefabs from O(n) searchable buffer to O(1) searchable EntityPrefabRegistry (NativeHashMap)
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct EntityPrefabBootstrap : ISystem {
        public void OnUpdate(ref SystemState state) {
            var query = SystemAPI.QueryBuilder()
                    .WithAll<EntityPrefabRegistryTag>()
                    .Build();

            if (query.IsEmpty)
                return;

            if (!SingletonManager.TryGetSingletonInstance(out EntityPrefabRegistry registry))
                return;

            var registryEntity = query.GetSingletonEntity();
            var buffer = SystemAPI.GetBuffer<EntityPrefabBufferElement>(registryEntity);

            for (var i = 0; i < buffer.Length; i++)
                registry.PrefabLookup[buffer[i].Hash] = buffer[i].Prefab;

            state.Enabled = false;
        }
    }
}