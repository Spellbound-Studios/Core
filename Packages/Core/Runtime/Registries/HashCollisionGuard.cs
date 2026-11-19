// Copyright 2026 Spellbound Studio Inc.

#if UNITY_EDITOR
using Spellbound.Core.Hashing;
using Spellbound.Core.Logging;
using UnityEditor;

namespace Spellbound.Core.Registries {
    /// <summary>
    /// Edit-time hash-collision detection for <see cref="HashedScriptableObject"/>s. A collision can only be
    /// born when an asset is first created (the GUID never changes afterward), so a freshly created collider
    /// is reference-free and gets its GUID regenerated automatically; an existing asset only gets a loud error.
    /// </summary>
    internal static class HashCollisionGuard {
        public static void Check(HashedScriptableObject asset, string assetPath, bool isNewAsset) {
            foreach (var otherGuid in AssetDatabase.FindAssets("t:HashedScriptableObject")) {
                if (otherGuid == asset.Guid)
                    continue;

                if (StableHash.Fnv1A32(otherGuid) != asset.Hash)
                    continue;

                var otherPath = AssetDatabase.GUIDToAssetPath(otherGuid);

                if (isNewAsset) {
                    Log.Warn($"Hash collision at creation: '{assetPath}' collides with '{otherPath}' at " +
                             $"hash {asset.Hash}. Regenerating the new asset's GUID.");
                    RegenerateGuid(assetPath);
                }
                else {
                    Log.Error($"Hash collision: '{assetPath}' collides with '{otherPath}' at hash {asset.Hash}. " +
                              "This asset may already be referenced, so it was not touched — duplicate it, delete " +
                              "the original, and rename the copy back to resolve by hand.");
                }

                return;
            }
        }

        private static void RegenerateGuid(string assetPath) {
            EditorApplication.delayCall += () => {
                var tempPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);

                if (!AssetDatabase.CopyAsset(assetPath, tempPath)) {
                    Log.Error($"GUID regeneration failed: could not copy '{assetPath}'. Resolve by hand.");

                    return;
                }

                AssetDatabase.DeleteAsset(assetPath);
                var moveError = AssetDatabase.MoveAsset(tempPath, assetPath);

                if (!string.IsNullOrEmpty(moveError))
                    Log.Error($"GUID regeneration: copy succeeded but moving '{tempPath}' back failed " +
                              $"({moveError}). Rename it by hand.");
            };
        }
    }
}
#endif
