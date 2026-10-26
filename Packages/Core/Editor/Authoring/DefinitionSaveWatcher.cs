// Copyright (c) Spellbound Studios. All rights reserved.

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Spellbound.Core.Authoring.Editor {
    public sealed class DefinitionSaveWatcher : AssetModificationProcessor {
        private static string[] OnWillSaveAssets(string[] paths) {
            var definitions = DefinitionPathsAmong(paths);

            if (definitions.Count > 0) {
                EditorApplication.delayCall += () => Revalidate(definitions);
            }

            return paths;
        }

        public static IReadOnlyList<string> DefinitionPathsAmong(IReadOnlyList<string> paths) {
            var definitions = new List<string>();

            if (paths == null) {
                return definitions;
            }

            foreach (var path in paths) {
                if (DefinitionAssets.IsDefinitionPath(path)) {
                    definitions.Add(path);
                }
            }

            return definitions;
        }

        private static void Revalidate(IReadOnlyList<string> paths) {
            var runner = new DefinitionValidationRunner();

            foreach (var path in paths) {
                var asset = AssetDatabase.LoadMainAssetAtPath(path) as ScriptableObject;

                if (DefinitionAssets.IsDefinition(asset)) {
                    runner.ValidateAsset(asset);
                }
            }
        }
    }
}
