// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Spellbound.Core.Operations {
    internal sealed class SceneObjectArgumentTypeParser : IArgumentTypeParser {
        private const string Prefix = "scene:";

        public bool CanParse(Type type) {
            return type == typeof(GameObject) || typeof(Component).IsAssignableFrom(type);
        }

        public string Expected(Type type) {
            return "scene:/Path/To/Object";
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

            var path = trimmed.Substring(Prefix.Length);
            var found = GameObject.Find(path);

            if (found == null) {
                return false;
            }

            if (type == typeof(GameObject)) {
                value = found;
                return true;
            }

            var component = found.GetComponent(type);

            if (component == null) {
                return false;
            }

            value = component;
            return true;
        }
    }
}
