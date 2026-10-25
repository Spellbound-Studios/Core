// Copyright 2026 Spellbound Studio Inc.

using Spellbound.Core.Hashing;
using Spellbound.Core.Tooling;
using UnityEngine;

namespace Spellbound.Core.Registries {
    /// <summary>
    /// Base for ScriptableObjects that carry a stable registry identity: the asset GUID and its FNV-1a hash,
    /// stamped at edit time. The object name self-heals to the filename on disk so git never drifts. Subclasses
    /// needing extra edit-time validation override <see cref="OnValidateAsset"/> — never declare OnValidate
    /// (Unity calls the most-derived magic method, which would silently disable the identity stamp).
    /// </summary>
    public abstract class HashedScriptableObject : ScriptableObject, IRegistryEntry {
        [SerializeField, Immutable] private string guid;
        [SerializeField, Immutable] private uint hash;

        public string Guid => guid;
        public uint Hash => hash;

#if UNITY_EDITOR
        private void OnValidate() => Restamp();

        /// <summary>
        /// Stamps name, guid, and hash from the asset file on disk, dirtying only on change.
        /// </summary>
        internal void Restamp() {
            var assetPath = UnityEditor.AssetDatabase.GetAssetPath(this);

            if (string.IsNullOrEmpty(assetPath))
                return;

            var dirty = false;
            var fileName = System.IO.Path.GetFileNameWithoutExtension(assetPath);

            if (name != fileName) {
                name = fileName;
                dirty = true;
            }

            var assetGuid = UnityEditor.AssetDatabase.GUIDFromAssetPath(assetPath).ToString();
            var isNewAsset = string.IsNullOrEmpty(guid);

            if (guid != assetGuid) {
                guid = assetGuid;
                dirty = true;
            }

            var newHash = StableHash.Fnv1A32(guid);

            if (hash != newHash) {
                hash = newHash;
                dirty = true;
                HashCollisionGuard.Check(this, assetPath, isNewAsset);
            }

            if (dirty)
                UnityEditor.EditorUtility.SetDirty(this);

            OnValidateAsset();
        }

        /// <summary>
        /// Edit-time validation hook for subclasses. Runs after the identity stamp on every OnValidate.
        /// </summary>
        protected virtual void OnValidateAsset() { }
#endif
    }
}
