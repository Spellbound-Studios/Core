// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Collections;
using System.Globalization;
using System.Text;

namespace Spellbound.Core.Operations {
    internal static class ArgumentText {
        public static string KebabCase(string name) {
            if (string.IsNullOrEmpty(name)) {
                return name;
            }

            var builder = new StringBuilder(name.Length + 4);

            for (var i = 0; i < name.Length; i++) {
                var character = name[i];

                if (char.IsUpper(character)) {
                    if (i > 0 && (char.IsLower(name[i - 1]) || char.IsDigit(name[i - 1])
                                  || (i + 1 < name.Length && char.IsLower(name[i + 1])))) {
                        builder.Append('-');
                    }

                    builder.Append(char.ToLowerInvariant(character));
                    continue;
                }

                builder.Append(character == '_' ? '-' : character);
            }

            return builder.ToString();
        }

        public static string Describe(object value) {
            switch (value) {
                case null:
                    return "none";
                case string text:
                    return text.Length == 0 ? "empty" : text;
                case bool flag:
                    return flag ? "true" : "false";
                case Enum member:
                    return member.ToString();
                case IFormattable formattable:
                    return formattable.ToString(null, CultureInfo.InvariantCulture);
                case IEnumerable items: {
                    var builder = new StringBuilder();

                    foreach (var item in items) {
                        if (builder.Length > 0) {
                            builder.Append(',');
                        }

                        builder.Append(Describe(item));
                    }

                    return builder.Length == 0 ? "empty" : builder.ToString();
                }
                default:
                    return value.ToString();
            }
        }

        public static string Join(string separator, System.Collections.Generic.IEnumerable<string> items) {
            return string.Join(separator, items);
        }
    }
}
