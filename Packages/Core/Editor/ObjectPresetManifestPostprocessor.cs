// Copyright 2026 Spellbound Studio Inc.

using Spellbound.Core.EntityPrefabs;
using Spellbound.Core.Objects;
using UnityEditor;

namespace Spellbound.Core {
    public sealed class ObjectPresetManifestPostprocessor : AssetPostprocessor {
        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets,
            string[] movedAssets, string[] movedFromAssetPaths) {
            if (!ContainsPreset(importedAssets) && !ContainsPreset(movedAssets) && !ContainsAsset(deletedAssets))
                return;

            foreach (var guid in AssetDatabase.FindAssets($"t:{nameof(PresetBakeManifest)}")) {
                var manifest = AssetDatabase.LoadAssetAtPath<PresetBakeManifest>(AssetDatabase.GUIDToAssetPath(guid));

                if (manifest != null)
                    manifest.Bump();
            }
        }

        private static bool ContainsPreset(string[] paths) {
            foreach (var path in paths) {
                if (!path.EndsWith(".asset"))
                    continue;

                if (typeof(ObjectPreset).IsAssignableFrom(AssetDatabase.GetMainAssetTypeAtPath(path)))
                    return true;
            }

            return false;
        }

        private static bool ContainsAsset(string[] paths) {
            foreach (var path in paths) {
                if (path.EndsWith(".asset"))
                    return true;
            }

            return false;
        }
    }
}
