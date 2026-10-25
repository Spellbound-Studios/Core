// Copyright 2026 Spellbound Studio Inc.

using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace Spellbound.Core.ECS {
    public partial struct InstantiationSystem : ISystem {
        public void OnCreate(ref SystemState state) =>
                state.RequireForUpdate(state.GetEntityQuery(
                    ComponentType.ReadOnly<ReadyForObjectsTag>(),
                    ComponentType.ReadOnly<ChunkRequestComponent>(),
                    ComponentType.ReadOnly<EntitySpawnRequestElement>()
                ));

        [BurstCompile]
        public void OnUpdate(ref SystemState state) {
            state.CompleteDependency();

            var em = state.EntityManager;

            var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                    .CreateCommandBuffer(state.WorldUnmanaged);

            var deletionsLookup = SystemAPI.GetBufferLookup<DeletionBufferElement>(true);

            var entities = SystemAPI.QueryBuilder()
                    .WithAll<ReadyForObjectsTag, EntitySpawnRequestElement, ChunkRequestComponent>()
                    .Build()
                    .ToEntityArray(Allocator.Temp);

            foreach (var entity in entities) {
                var buffer = em.GetBuffer<EntitySpawnRequestElement>(entity, true);
                var request = em.GetComponentData<ChunkRequestComponent>(entity);

                var deletionsHashSet = new NativeHashSet<int>(0, Allocator.Temp);

                if (deletionsLookup.HasBuffer(entity)) {
                    var deletions = deletionsLookup[entity];
                    deletionsHashSet.Capacity = deletions.Length;

                    foreach (var deletion in deletions)
                        deletionsHashSet.Add(deletion.Value);

                    ecb.RemoveComponent<DeletionBufferElement>(entity);
                }

                foreach (var element in buffer) {
                    if (deletionsHashSet.Contains(element.InstanceIndex))
                        continue;

                    if (element.Prefab == Entity.Null)
                        continue;

                    var spawned = ecb.Instantiate(element.Prefab);
                    ecb.SetComponent(spawned, element.Transform);
                    ecb.SetComponent(spawned, new InstanceIndexComponent { Value = element.InstanceIndex });
                    ecb.SetSharedComponent(spawned, new ChunkParentComponent { ChunkCoord = request.ChunkCoord });
                }

                ecb.RemoveComponent<EntitySpawnRequestElement>(entity);
                //ecb.RemoveComponent<ReadyForObjectsTag>(entity);
                deletionsHashSet.Dispose();
            }

            entities.Dispose();
        }
    }
}