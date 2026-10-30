// Copyright 2026 Spellbound Studio Inc.

using Spellbound.Core.Logging;
using Spellbound.Core.Objects;
using Spellbound.Core.ModuleContracts;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Spellbound.Core.ECS {
    // Attach this MonoBehaviour to the prefab GameObject
    public class ObjectPresetAuthoring : MonoBehaviour {
        public ObjectPreset preset; // drag in the ScriptableObject
    }

    public class ObjectPresetBaker : Baker<ObjectPresetAuthoring> {
        public override void Bake(ObjectPresetAuthoring authoring) {
            if (authoring.preset == null) {
                Log.Error($"[ObjectPresetBaker] Missing ObjectPreset on '{authoring.gameObject.name}'!");

                return;
            }

            var entity = GetEntity(TransformUsageFlags.Renderable);

            AddSharedComponentManaged(entity, new PresetHashComponent {
                Value = authoring.preset.Hash
            });

            AddComponent(entity, new ProximityThresholdComponent {
                Value = new float2(authoring.preset.interactionDistance)
            });

            AddComponent(entity, new InstanceIndexComponent {
                Value = -1
            });

            if (authoring.preset.isDynamic)
                AddComponent<DynamicTag>(entity);

            AddSharedComponent(entity, new ChunkParentComponent());

            if (authoring.preset.TryGetModules<ITimerModule>(out _))
                AddComponent(entity, new TimerModuleTag());
        }
    }
}