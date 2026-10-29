// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Spellbound.Core.Controls.Editor {
    public sealed class LookSource {
        private readonly Func<EditorSkin, IReadOnlyList<StyleSheet>> _sheetsFor;

        public LookSource(string key, Func<EditorSkin, IReadOnlyList<StyleSheet>> sheetsFor, int order = 0) {
            if (string.IsNullOrWhiteSpace(key)) {
                throw new ArgumentException("A look source needs a key.", nameof(key));
            }

            Key = key.Trim();
            _sheetsFor = sheetsFor ?? throw new ArgumentNullException(nameof(sheetsFor));
            Order = order;
        }

        public LookSource(string key, StyleSheet sheet, int order = 0)
            : this(key, _ => sheet != null ? new[] { sheet } : Array.Empty<StyleSheet>(), order) { }

        public LookSource(string key, StyleSheet dark, StyleSheet light, int order = 0)
            : this(key, skin => OneOf(skin.Equals(EditorSkin.Light) ? light : dark), order) { }

        public string Key { get; }
        public int Order { get; }

        public IReadOnlyList<StyleSheet> SheetsFor(EditorSkin skin) {
            return _sheetsFor(skin) ?? Array.Empty<StyleSheet>();
        }

        private static IReadOnlyList<StyleSheet> OneOf(StyleSheet sheet) {
            return sheet != null ? new[] { sheet } : Array.Empty<StyleSheet>();
        }
    }
}
