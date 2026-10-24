// Copyright 2026 Spellbound Studio Inc.

using Unity.Collections;
using Unity.Entities;
using Unity.Rendering;

namespace Spellbound.Core.ECS {
    /// <summary>
    /// 
    /// </summary>
    [DisableAutoCreation] // attribute makes the isystem just not exist unless we activate it. Could be a setting.
    public partial struct EntityBatchConsolidationSystem : ISystem {
        private EntityQuery _query;
        private int _currentIndex;

        public void OnCreate(ref SystemState state) {
            _query = SystemAPI.QueryBuilder()
                    .WithAll<PresetHashComponent>()
                    .WithAllChunkComponent<EntitiesGraphicsChunkInfo>()
                    .Build();
            _currentIndex = 0;
        }

        public void OnUpdate(ref SystemState state) {
            state.EntityManager.GetAllUniqueSharedComponents(out NativeList<PresetHashComponent> uniqueValues,
                Allocator.Temp);

            if (uniqueValues.Length <= 1) {
                uniqueValues.Dispose();

                return;
            }

            _currentIndex %= uniqueValues.Length;
            var presetHash = uniqueValues[_currentIndex];
            _currentIndex++;

            _query.SetSharedComponentFilter(presetHash);
            var chunks = _query.ToArchetypeChunkArray(Allocator.Temp);
            _query.ResetFilter();

            foreach (var chunk in chunks)
                state.EntityManager.SetChunkComponentData(chunk, default(EntitiesGraphicsChunkInfo));

            chunks.Dispose();
            uniqueValues.Dispose();
        }
    }
}