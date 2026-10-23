// Copyright 2026 Spellbound Studio Inc.

using UnityEngine;

namespace Spellbound.Core.EntityPrefabs {
    [CreateAssetMenu(fileName = "Preset Bake Manifest", menuName = "Spellbound/Presets/PresetBakeManifest")]
    public sealed class PresetBakeManifest : ScriptableObject {
        [SerializeField] private int version;

        public int Version => version;

#if UNITY_EDITOR
        public void Bump() {
            version++;
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.AssetDatabase.SaveAssetIfDirty(this);
        }
#endif
    }
}
