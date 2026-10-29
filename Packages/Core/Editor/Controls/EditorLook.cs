// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Collections.Generic;
using Spellbound.Core.Logging;
using UnityEditor;
using UnityEngine.UIElements;

namespace Spellbound.Core.Controls.Editor {
    public static class EditorLook {
        public const string ThemeClassName = "sb-theme";

        private sealed class Dressing {
            public WeakReference<VisualElement> Root;
            public List<StyleSheet> Sheets = new List<StyleSheet>();
            public EditorSkin Skin;
            public bool HadThemeClass;
        }

        private static readonly Dictionary<string, LookSource> Registered =
            new Dictionary<string, LookSource>(StringComparer.Ordinal);

        private static readonly List<Dressing> Dressed = new List<Dressing>();

        private static Func<EditorLookSettings> _settings;
        private static EditorLookSettings _found;
        private static bool _searched;

        public static Func<EditorLookSettings> Settings {
            get => _settings ?? FindSettingsOnce;
            set {
                _settings = value;
                ForgetSettings();
            }
        }

        public static IReadOnlyList<LookSource> Sources {
            get {
                var sources = new List<LookSource>(Registered.Values);
                sources.Sort((left, right) => {
                    var byOrder = left.Order.CompareTo(right.Order);
                    return byOrder != 0 ? byOrder : string.CompareOrdinal(left.Key, right.Key);
                });

                return sources;
            }
        }

        public static void Register(LookSource source) {
            if (source == null) {
                throw new ArgumentNullException(nameof(source));
            }

            Registered[source.Key] = source;
        }

        public static bool Remove(string key) {
            return !string.IsNullOrWhiteSpace(key) && Registered.Remove(key.Trim());
        }

        public static void ForgetSettings() {
            _found = null;
            _searched = false;
        }

        public static IReadOnlyList<StyleSheet> SheetsFor(EditorSkin skin) {
            var sheets = new List<StyleSheet>();
            AddOnce(sheets, ControlStyleSheet.Default);
            var settings = Settings();

            foreach (var source in Sources) {
                if (settings != null && settings.Excludes(source.Key)) {
                    continue;
                }

                foreach (var sheet in SheetsOf(source, skin)) {
                    AddOnce(sheets, sheet);
                }
            }

            if (settings != null && settings.styleSheets != null) {
                foreach (var sheet in settings.styleSheets) {
                    AddOnce(sheets, sheet);
                }
            }

            return sheets;
        }

        public static void Dress(VisualElement root) {
            if (root == null) {
                throw new ArgumentNullException(nameof(root));
            }

            var skin = EditorSkin.Current;
            var sheets = SheetsFor(skin);
            var previous = FindDressing(root);
            var hadThemeClass = previous?.HadThemeClass ?? root.ClassListContains(ThemeClassName);
            var index = previous != null ? Undress(root, previous) : root.styleSheets.count;
            var dressing = new Dressing {
                Root = new WeakReference<VisualElement>(root), Skin = skin, HadThemeClass = hadThemeClass
            };

            root.AddToClassList(ThemeClassName);
            root.AddToClassList(skin.ClassName);

            foreach (var sheet in sheets) {
                if (root.styleSheets.Contains(sheet)) {
                    continue;
                }

                root.styleSheets.Insert(Math.Min(index, root.styleSheets.count), sheet);
                dressing.Sheets.Add(sheet);
                index++;
            }

            Dressed.Add(dressing);
        }

        public static bool IsDressed(VisualElement root) {
            return root != null && root.ClassListContains(ThemeClassName) && FindDressing(root) != null;
        }

        public static bool HasDressedAncestor(VisualElement element) {
            for (var parent = element?.hierarchy.parent; parent != null; parent = parent.hierarchy.parent) {
                if (IsDressed(parent)) {
                    return true;
                }
            }

            return false;
        }

        public static void Strip(VisualElement root) {
            var dressing = FindDressing(root);

            if (dressing == null) {
                return;
            }

            Undress(root, dressing);

            if (!dressing.HadThemeClass) {
                root.RemoveFromClassList(ThemeClassName);
            }
        }

        public static void RedressAll() {
            var roots = new List<VisualElement>();

            foreach (var dressing in Dressed) {
                if (dressing.Root.TryGetTarget(out var root)) {
                    roots.Add(root);
                }
            }

            foreach (var root in roots) {
                Dress(root);
            }
        }

        private static EditorLookSettings FindSettingsOnce() {
            if (!_searched) {
                _found = EditorLookSettings.Find();
                _searched = true;
            }

            return _found;
        }

        private static IReadOnlyList<StyleSheet> SheetsOf(LookSource source, EditorSkin skin) {
            try {
                return source.SheetsFor(skin);
            }
            catch (Exception exception) {
                Log.Error("The look source '" + source.Key + "' failed for the " + skin.Name + " skin and is skipped: "
                          + exception.Message);
                return Array.Empty<StyleSheet>();
            }
        }

        private static int Undress(VisualElement root, Dressing dressing) {
            Dressed.Remove(dressing);
            root.RemoveFromClassList(dressing.Skin.ClassName);
            var index = root.styleSheets.count;

            foreach (var sheet in dressing.Sheets) {
                if (sheet == null) {
                    continue;
                }

                var position = IndexOf(root, sheet);

                if (position < 0) {
                    continue;
                }

                index = Math.Min(index, position);
                root.styleSheets.Remove(sheet);
            }

            dressing.Sheets.Clear();
            return Math.Min(index, root.styleSheets.count);
        }

        private static int IndexOf(VisualElement root, StyleSheet sheet) {
            for (var position = 0; position < root.styleSheets.count; position++) {
                if (ReferenceEquals(root.styleSheets[position], sheet)) {
                    return position;
                }
            }

            return -1;
        }

        private static Dressing FindDressing(VisualElement root) {
            for (var position = Dressed.Count - 1; position >= 0; position--) {
                if (!Dressed[position].Root.TryGetTarget(out var dressed)) {
                    Dressed.RemoveAt(position);
                    continue;
                }

                if (ReferenceEquals(dressed, root)) {
                    return Dressed[position];
                }
            }

            return null;
        }

        private static void AddOnce(List<StyleSheet> sheets, StyleSheet sheet) {
            if (sheet != null && !sheets.Contains(sheet)) {
                sheets.Add(sheet);
            }
        }
    }
}
