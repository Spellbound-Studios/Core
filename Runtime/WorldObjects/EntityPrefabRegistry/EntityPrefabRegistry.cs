// Copyright 2026 Spellbound Studio Inc.

using Spellbound.Core.Tooling;
using Unity.Collections;
using Unity.Entities;

namespace Spellbound.Core.EntityPrefabs {
    /// <summary>
    /// Access point for O(1) Entity Prefab lookup by preset hash.
    /// </summary>
    public partial class EntityPrefabRegistry : SystemBase {
        private Entity _registryEntity;
        private bool _found;

        public NativeHashMap<uint, Entity> PrefabLookup;

        protected override void OnStartRunning() {
            if (PrefabLookup.IsCreated)
                PrefabLookup.Dispose();
            PrefabLookup = new NativeHashMap<uint, Entity>(64, Allocator.Persistent);
            SingletonManager.RegisterSingleton(this);
        }

        protected override void OnDestroy() {
            if (PrefabLookup.IsCreated)
                PrefabLookup.Dispose();
        }

        public Entity GetPrefab(int index) {
            if (!_found) {
                var query = SystemAPI.QueryBuilder()
                        .WithAll<EntityPrefabRegistryTag>()
                        .Build();

                if (query.IsEmpty)
                    return Entity.Null;

                _registryEntity = query.GetSingletonEntity();
                _found = true;
            }

            var buffer = SystemAPI.GetBuffer<EntityPrefabBufferElement>(_registryEntity);

            return buffer[index].Prefab;
        }

        protected override void OnUpdate() { }
    }
}