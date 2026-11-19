// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using UnityEditor;

namespace Spellbound.Core.Tests {
    public sealed class FileSystemSnapshot {
        private readonly SortedDictionary<string, string> _files =
            new SortedDictionary<string, string>(StringComparer.Ordinal);

        private readonly SortedDictionary<string, string> _assets =
            new SortedDictionary<string, string>(StringComparer.Ordinal);

        public static FileSystemSnapshot Take(IEnumerable<string> diskPaths, IEnumerable<string> assetFolders) {
            var snapshot = new FileSystemSnapshot();

            foreach (var path in diskPaths) {
                snapshot.AddDiskPath(Path.GetFullPath(path));
            }

            foreach (var folder in assetFolders) {
                snapshot.AddAssetFolder(folder);
            }

            return snapshot;
        }

        public IReadOnlyList<string> Differences(FileSystemSnapshot later) {
            var differences = new List<string>();
            Compare(_files, later._files, "file", differences);
            Compare(_assets, later._assets, "asset", differences);
            return differences;
        }

        private void AddDiskPath(string path) {
            if (File.Exists(path)) {
                _files[path] = HashOf(path);
                return;
            }

            if (!Directory.Exists(path)) {
                return;
            }

            _files[path + Path.DirectorySeparatorChar] = string.Empty;

            foreach (var file in Directory.GetFiles(path, "*", SearchOption.AllDirectories)) {
                _files[file] = HashOf(file);
            }

            foreach (var directory in Directory.GetDirectories(path, "*", SearchOption.AllDirectories)) {
                _files[directory + Path.DirectorySeparatorChar] = string.Empty;
            }
        }

        private void AddAssetFolder(string folder) {
            if (!AssetDatabase.IsValidFolder(folder)) {
                return;
            }

            foreach (var guid in AssetDatabase.FindAssets(string.Empty, new[] { folder })) {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var physical = Path.GetFullPath(FileUtil.GetPhysicalPath(path));
                _assets[guid] = path + " " + (File.Exists(physical) ? HashOf(physical) : string.Empty);
            }
        }

        private static void Compare(SortedDictionary<string, string> before, SortedDictionary<string, string> after,
            string kind, List<string> differences) {
            foreach (var entry in before) {
                if (!after.TryGetValue(entry.Key, out var value)) {
                    differences.Add(kind + " removed: " + entry.Key);
                }
                else if (!string.Equals(entry.Value, value, StringComparison.Ordinal)) {
                    differences.Add(kind + " changed: " + entry.Key);
                }
            }

            foreach (var entry in after) {
                if (!before.ContainsKey(entry.Key)) {
                    differences.Add(kind + " added: " + entry.Key);
                }
            }
        }

        private static string HashOf(string path) {
            using (var algorithm = SHA256.Create())
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
                return Convert.ToBase64String(algorithm.ComputeHash(stream));
            }
        }
    }
}
