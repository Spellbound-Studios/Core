// Copyright 2026 Spellbound Studio Inc.

using Spellbound.Core.Objects;
using Unity.Entities;
using UnityEngine;

namespace Spellbound.Core.EntityPrefabs {
    /// <summary>
    /// Combs the resource folder and populates buffer with entity prefabs and their guid.
    /// </summary>
    public sealed class EntityPrefabRegistryAuthoring : MonoBehaviour {
        public PresetBakeManifest presetManifest;
    }

    public sealed class PrefabRegistryBaker : Baker<EntityPrefabRegistryAuthoring> {
        public override void Bake(EntityPrefabRegistryAuthoring authoring) {
            if (authoring.presetManifest == null) {
                Debug.LogWarning(
                    $"{nameof(EntityPrefabRegistryAuthoring)} on '{authoring.name}' has no " +
                    $"{nameof(PresetBakeManifest)} assigned, so added or removed presets will not trigger a rebake.");
            }
            else
                DependsOn(authoring.presetManifest);

            var entity = GetEntity(TransformUsageFlags.None);

            AddComponent<EntityPrefabRegistryTag>(entity);

            var buffer = AddBuffer<EntityPrefabBufferElement>(entity);
            var presets = Resources.LoadAll<ObjectPreset>("");

            foreach (var preset in presets) {
                DependsOn(preset);

                if (preset.bakePrefab == null) {
                    Debug.LogWarning($"Preset {preset.name} has no bakePrefab assigned, skipping.");

                    continue;
                }

                buffer.Add(new EntityPrefabBufferElement {
                    Prefab = GetEntity(preset.bakePrefab, TransformUsageFlags.Renderable),
                    Hash = preset.Hash
                });
            }
        }
    }
}