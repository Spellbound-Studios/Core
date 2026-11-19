// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using UnityEditor;

namespace Spellbound.Core.Controls.Editor {
    public readonly struct EditorSkin : IEquatable<EditorSkin> {
        public const string DarkName = "dark";
        public const string LightName = "light";

        public static readonly EditorSkin Dark = new EditorSkin(DarkName);
        public static readonly EditorSkin Light = new EditorSkin(LightName);

        private readonly string _name;

        public EditorSkin(string name) {
            if (string.IsNullOrWhiteSpace(name)) {
                throw new ArgumentException("A skin needs a name.", nameof(name));
            }

            _name = name.Trim();
        }

        public string Name => _name ?? DarkName;
        public string ClassName => EditorLook.ThemeClassName + "--" + Name;

        public static EditorSkin Current => EditorGUIUtility.isProSkin ? Dark : Light;

        public bool Equals(EditorSkin other) {
            return string.Equals(Name, other.Name, StringComparison.Ordinal);
        }

        public override bool Equals(object obj) {
            return obj is EditorSkin other && Equals(other);
        }

        public override int GetHashCode() {
            return StringComparer.Ordinal.GetHashCode(Name);
        }

        public override string ToString() {
            return Name;
        }
    }
}
