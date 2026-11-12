// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Collections.Generic;
using Spellbound.Core.Operations;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Spellbound.Core.Operations.Editor {
    [ArgumentParser]
    public sealed class AssetArgumentTypeParser : IArgumentTypeParser {
        private const string Prefix = "asset:";

        public bool CanParse(Type type) {
            return typeof(Object).IsAssignableFrom(type) && type != typeof(GameObject) && !typeof(Component).IsAssignableFrom(type);
        }

        public string Expected(Type type) {
            return "asset:Assets/Path/To/File.asset or asset:Name";
        }

        public IReadOnlyList<string> LegalValues(Type type) {
            return Array.Empty<string>();
        }

        public bool TryParse(string text, Type type, out object value) {
            value = null;
            var trimmed = text.Trim();

            if (!trimmed.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase)) {
                return false;
            }

            var reference = trimmed.Substring(Prefix.Length);

            if (reference.Length == 0) {
                return false;
            }

            if (reference.Contains("/")) {
                value = AssetDatabase.LoadAssetAtPath(reference, type);
                return value != null;
            }

            var matches = new List<string>();

            foreach (var guid in AssetDatabase.FindAssets(reference + " t:" + type.Name)) {
                var path = AssetDatabase.GUIDToAssetPath(guid);

                if (string.Equals(System.IO.Path.GetFileNameWithoutExtension(path), reference, StringComparison.OrdinalIgnoreCase)) {
                    matches.Add(path);
                }
            }

            if (matches.Count != 1) {
                if (matches.Count > 1) {
                    throw new OperationException("'" + reference + "' names " + matches.Count + " assets: " + string.Join(", ", matches),
                        "Use asset:<path> to pick one.");
                }

                return false;
            }

            value = AssetDatabase.LoadAssetAtPath(matches[0], type);
            return value != null;
        }
    }
}
