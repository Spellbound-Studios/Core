// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Collections.Generic;

namespace Spellbound.Core.Operations {
    internal sealed class EnumArgumentTypeParser : IArgumentTypeParser {
        public bool CanParse(Type type) {
            return type.IsEnum;
        }

        public string Expected(Type type) {
            return "one of " + string.Join(", ", Enum.GetNames(type));
        }

        public IReadOnlyList<string> LegalValues(Type type) {
            return Enum.GetNames(type);
        }

        public bool TryParse(string text, Type type, out object value) {
            value = null;
            var trimmed = text.Trim();

            if (trimmed.Length == 0 || char.IsDigit(trimmed[0]) || trimmed[0] == '-') {
                return false;
            }

            foreach (var name in Enum.GetNames(type)) {
                if (string.Equals(name, trimmed, StringComparison.OrdinalIgnoreCase)) {
                    value = Enum.Parse(type, name);
                    return true;
                }
            }

            return false;
        }
    }
}
