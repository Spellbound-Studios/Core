// Copyright 2026 Spellbound Studio Inc.

using Unity.Burst;
using Unity.Entities;

namespace Spellbound.Core.ECS {
    /// <summary>
    /// DestroyTaggedEntitiesSystem is an essential script for the ECS portion of Core. It is the responsible system for
    /// handling all deletions of appropriately tagged entities.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct DestroyTaggedEntitiesSystem : ISystem {
        [BurstCompile]
        public void OnCreate(ref SystemState state) {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<PendingDestroyTag>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state) {
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (tag, entity) in SystemAPI.Query<RefRO<PendingDestroyTag>>().WithEntityAccess())
                ecb.DestroyEntity(entity);
        }
    }
}