// Copyright 2026 Spellbound Studio Inc.

#if UNITY_EDITOR
using Spellbound.Core.Logging;
using UnityEditor;

namespace Spellbound.Core.Registries {
    /// <summary>
    /// Editor utilities for <see cref="HashedScriptableObject"/> assets.
    /// </summary>
    public static class HashedAssetTools {
        /// <summary>
        /// Forces the identity stamp on every hashed asset in the project and saves, so stale m_Name / guid /
        /// hash values from old commits get rewritten on disk in one pass. Run after pulls that touched assets,
        /// then commit the fixups.
        /// </summary>
        [MenuItem("Spellbound/Assets/Restamp All Hashed Assets")]
        private static void RestampAll() {
            var guids = AssetDatabase.FindAssets("t:HashedScriptableObject");

            foreach (var guid in guids) {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<HashedScriptableObject>(path);

                if (asset != null)
                    asset.Restamp();
            }

            AssetDatabase.SaveAssets();
            Log.Info($"Restamped {guids.Length} hashed assets.");
        }
    }
}
#endif
