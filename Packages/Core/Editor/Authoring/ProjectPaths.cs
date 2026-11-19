// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using UnityEditor;

namespace Spellbound.Core.Authoring.Editor {
    public static class ProjectPaths {
        public const string AssetsRoot = "Assets";
        public const string PackagesRoot = "Packages";
        public const string LibraryRoot = "Library/Spellbound";

        public static string Normalize(string path) {
            if (string.IsNullOrWhiteSpace(path)) {
                return string.Empty;
            }

            var normalized = path.Trim().Replace('\\', '/');

            while (normalized.Length > 1 && normalized.EndsWith("/")) {
                normalized = normalized.Substring(0, normalized.Length - 1);
            }

            return normalized;
        }

        public static bool IsWritable(string path) {
            var normalized = Normalize(path);
            return IsInside(normalized, AssetsRoot) || IsInside(normalized, LibraryRoot);
        }

        public static bool IsReadable(string path) {
            var normalized = Normalize(path);
            return IsWritable(normalized) || IsInside(normalized, PackagesRoot);
        }

        public static bool IsInside(string normalizedPath, string root) {
            if (string.IsNullOrEmpty(normalizedPath) || normalizedPath.Contains("..") || normalizedPath.Contains(":")) {
                return false;
            }

            return string.Equals(normalizedPath, root, StringComparison.Ordinal)
                   || normalizedPath.StartsWith(root + "/", StringComparison.Ordinal);
        }

        public static string EnsureFolder(string folder) {
            var normalized = Normalize(folder);

            if (!IsInside(normalized, AssetsRoot)) {
                throw new ArgumentException(
                    "A folder the authoring operations write is under Assets; '" + folder + "' is not.", nameof(folder));
            }

            if (AssetDatabase.IsValidFolder(normalized)) {
                return normalized;
            }

            var parts = normalized.Split('/');
            var current = parts[0];

            for (var i = 1; i < parts.Length; i++) {
                var next = current + "/" + parts[i];

                if (!AssetDatabase.IsValidFolder(next)) {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }

            return normalized;
        }
    }
}
