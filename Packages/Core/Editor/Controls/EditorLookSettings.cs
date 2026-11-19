// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Spellbound.Core.Controls.Editor {
    [CreateAssetMenu(fileName = "EditorLookSettings", menuName = "Spellbound/Editor Look Settings")]
    public sealed class EditorLookSettings : ScriptableObject {
        public string[] excludedKeys = Array.Empty<string>();
        public StyleSheet[] styleSheets = Array.Empty<StyleSheet>();

        public static EditorLookSettings Find() {
            var candidates = new List<string>();

            foreach (var guid in AssetDatabase.FindAssets("t:" + nameof(EditorLookSettings))) {
                candidates.Add(AssetDatabase.GUIDToAssetPath(guid));
            }

            candidates.Sort((left, right) => {
                var leftInAssets = left.StartsWith("Assets/", StringComparison.Ordinal);
                var rightInAssets = right.StartsWith("Assets/", StringComparison.Ordinal);
                return leftInAssets != rightInAssets ? (leftInAssets ? -1 : 1) : string.CompareOrdinal(left, right);
            });

            foreach (var path in candidates) {
                var loaded = AssetDatabase.LoadAssetAtPath<EditorLookSettings>(path);

                if (loaded != null) {
                    return loaded;
                }
            }

            return null;
        }

        public bool Excludes(string key) {
            if (excludedKeys == null || string.IsNullOrEmpty(key)) {
                return false;
            }

            foreach (var excluded in excludedKeys) {
                if (string.Equals(excluded?.Trim(), key, StringComparison.Ordinal)) {
                    return true;
                }
            }

            return false;
        }

        private void OnValidate() {
            if (!AssetDatabase.Contains(this)) {
                return;
            }

            EditorLook.ForgetSettings();
            EditorLook.RedressAll();
        }
    }
}
