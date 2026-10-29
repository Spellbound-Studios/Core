// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.IO;
using UnityEditor;
using UnityEngine.UIElements;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace Spellbound.Core.Controls.Editor {
    public static class ControlStyleSheet {
        public const string FileName = "SpellboundControls.uss";
        public const string PackageRelativePath = "Editor/Controls/" + FileName;

        private static Func<StyleSheet> _source = LoadShipped;
        private static StyleSheet _default;
        private static bool _searched;

        public static Func<StyleSheet> Source {
            get => _source;
            set {
                _source = value;
                _default = null;
                _searched = false;
            }
        }

        public static StyleSheet Default {
            get {
                if (_default == null && !_searched) {
                    _default = _source?.Invoke();
                    _searched = true;
                }

                return _default;
            }
        }

        public static void Apply(VisualElement element) {
            if (element == null) {
                throw new ArgumentNullException(nameof(element));
            }

            var sheet = Default;

            if (sheet != null && !element.styleSheets.Contains(sheet)) {
                element.styleSheets.Add(sheet);
            }
        }

        public static StyleSheet LoadShipped() {
            var package = PackageInfo.FindForAssembly(typeof(ControlStyleSheet).Assembly);

            if (package != null) {
                var shipped = AssetDatabase.LoadAssetAtPath<StyleSheet>(package.assetPath + "/" + PackageRelativePath);

                if (shipped != null) {
                    return shipped;
                }
            }

            var search = Path.GetFileNameWithoutExtension(FileName) + " t:StyleSheet";

            foreach (var guid in AssetDatabase.FindAssets(search)) {
                var path = AssetDatabase.GUIDToAssetPath(guid);

                if (path.EndsWith("/" + PackageRelativePath, StringComparison.Ordinal)) {
                    return AssetDatabase.LoadAssetAtPath<StyleSheet>(path);
                }
            }

            return null;
        }
    }
}
