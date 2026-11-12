// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace Spellbound.Core.Authoring.Editor {
    public static class DefinitionAssets {
        public static IReadOnlyList<Type> DefinitionTypes() {
            var types = new List<Type>();

            foreach (var type in TypeCache.GetTypesDerivedFrom<IAuthoredDefinition>()) {
                if (!type.IsAbstract && !type.ContainsGenericParameters
                    && typeof(ScriptableObject).IsAssignableFrom(type)) {
                    types.Add(type);
                }
            }

            types.Sort((left, right) => string.CompareOrdinal(left.FullName, right.FullName));
            return types;
        }

        public static bool IsDefinition(ScriptableObject asset) {
            return asset != null && asset is IAuthoredDefinition;
        }

        public static bool IsDefinitionPath(string assetPath) {
            if (string.IsNullOrEmpty(assetPath)) {
                return false;
            }

            var type = AssetDatabase.GetMainAssetTypeAtPath(assetPath);
            return type != null && typeof(IAuthoredDefinition).IsAssignableFrom(type)
                   && typeof(ScriptableObject).IsAssignableFrom(type);
        }

        public static IReadOnlyList<string> FindAll() {
            return Find(null);
        }

        public static IReadOnlyList<string> FindInFolder(string folder) {
            return Find(new[] { folder });
        }

        public static string ResolveFolder(string path) {
            var normalized = ProjectPaths.Normalize(path);

            if (normalized.Length == 0) {
                return null;
            }

            if (AssetDatabase.IsValidFolder(normalized)) {
                return normalized;
            }

            if (!normalized.StartsWith(ProjectPaths.PackagesRoot + "/", StringComparison.Ordinal)) {
                return null;
            }

            var remainder = normalized.Substring(ProjectPaths.PackagesRoot.Length + 1);
            var slash = remainder.IndexOf('/');
            var packageName = slash < 0 ? remainder : remainder.Substring(0, slash);
            var rest = slash < 0 ? string.Empty : remainder.Substring(slash);

            foreach (var package in PackageInfo.GetAllRegisteredPackages()) {
                if (Matches(package, packageName)) {
                    var candidate = package.assetPath + rest;
                    return AssetDatabase.IsValidFolder(candidate) ? candidate : null;
                }
            }

            return null;
        }

        private static bool Matches(PackageInfo package, string wanted) {
            return string.Equals(package.name, wanted, StringComparison.OrdinalIgnoreCase)
                   || string.Equals(package.displayName, wanted, StringComparison.OrdinalIgnoreCase)
                   || (!string.IsNullOrEmpty(package.resolvedPath)
                       && string.Equals(Path.GetFileName(package.resolvedPath.TrimEnd('/', '\\')), wanted,
                           StringComparison.OrdinalIgnoreCase));
        }

        private static IReadOnlyList<string> Find(string[] folders) {
            var paths = new HashSet<string>(StringComparer.Ordinal);

            foreach (var type in DefinitionTypes()) {
                var guids = folders == null
                    ? AssetDatabase.FindAssets("t:" + type.Name)
                    : AssetDatabase.FindAssets("t:" + type.Name, folders);

                foreach (var guid in guids) {
                    var path = AssetDatabase.GUIDToAssetPath(guid);

                    if (IsDefinitionPath(path)) {
                        paths.Add(path);
                    }
                }
            }

            var sorted = new List<string>(paths);
            sorted.Sort(string.CompareOrdinal);
            return sorted;
        }
    }
}
