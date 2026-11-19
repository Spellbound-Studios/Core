// Copyright (c) Spellbound Studios. All rights reserved.

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Spellbound.Core.Authoring.Editor {
    internal static class DefinitionReferences {
        private const string StoredElement = ".Array.data[";

        public static IReadOnlyList<DefinitionReference> OutsideFolder(ScriptableObject asset, string folder) {
            var references = new List<DefinitionReference>();

            if (asset == null) {
                return references;
            }

            using (var serialized = new SerializedObject(asset)) {
                var property = serialized.GetIterator();

                while (property.Next(true)) {
                    if (property.propertyType != SerializedPropertyType.ObjectReference
                        || !(property.objectReferenceValue is ScriptableObject target) || target == asset
                        || !DefinitionAssets.IsDefinition(target)) {
                        continue;
                    }

                    var path = ProjectPaths.Normalize(AssetDatabase.GetAssetPath(target));

                    if (path.Length == 0 || ProjectPaths.IsInside(path, folder)) {
                        continue;
                    }

                    references.Add(new DefinitionReference(FieldName(property.propertyPath), target,
                        PackageLabel.Of(target.GetType())));
                }
            }

            return references;
        }

        private static string FieldName(string propertyPath) {
            return propertyPath.Replace(StoredElement, "[");
        }
    }
}
