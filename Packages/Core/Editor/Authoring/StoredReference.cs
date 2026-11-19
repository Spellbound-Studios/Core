// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Collections.Generic;

namespace Spellbound.Core.Authoring.Editor {
    internal sealed class StoredReference {
        public const string RegistryKey = "references";
        public const string EntriesKey = "RefIds";
        public const string IdentifierKey = "rid";
        public const string TypeKey = "type";
        public const string DataKey = "data";

        private readonly List<StoredKey> _data = new List<StoredKey>();

        public long Identifier { get; private set; }
        public string ClassName { get; private set; } = string.Empty;
        public string Namespace { get; private set; } = string.Empty;
        public string AssemblyName { get; private set; } = string.Empty;
        public IReadOnlyList<StoredKey> Data => _data;
        public string FullName => Namespace.Length == 0 ? ClassName : Namespace + "." + ClassName;

        public static bool IsRegistryKey(StoredKey key) {
            return key.Path == RegistryKey || key.IsWithin(RegistryKey);
        }

        public static IReadOnlyList<StoredReference> Read(IReadOnlyList<StoredKey> keys) {
            var entriesPrefix = RegistryKey + "." + EntriesKey;
            var byEntry = new Dictionary<string, StoredReference>(StringComparer.Ordinal);
            var references = new List<StoredReference>();

            foreach (var key in keys) {
                if (!key.IsWithin(entriesPrefix) || key.Path[entriesPrefix.Length] != '[') {
                    continue;
                }

                var close = key.Path.IndexOf(']', entriesPrefix.Length);

                if (close < 0 || close + 1 >= key.Path.Length) {
                    continue;
                }

                var entry = key.Path.Substring(0, close + 1);

                if (!byEntry.TryGetValue(entry, out var reference)) {
                    reference = new StoredReference();
                    byEntry.Add(entry, reference);
                    references.Add(reference);
                }

                reference.Take(key.Relative(entry + "."));
            }

            return references;
        }

        public static bool TryIdentifier(string value, out long identifier) {
            var text = value.Trim();

            if (text.StartsWith("{", StringComparison.Ordinal)) {
                text = FlowValue(text, IdentifierKey);
            }

            return long.TryParse(text, out identifier);
        }

        private void Take(StoredKey key) {
            if (key.Path == IdentifierKey) {
                TryIdentifier(key.Value, out var identifier);
                Identifier = identifier;
            }
            else if (key.Path == TypeKey) {
                ClassName = FlowValue(key.Value, "class").Replace('/', '+');
                Namespace = FlowValue(key.Value, "ns");
                AssemblyName = FlowValue(key.Value, "asm");
            }
            else if (key.Path.StartsWith(DataKey + ".", StringComparison.Ordinal)) {
                _data.Add(key.Relative(DataKey + "."));
            }
        }

        private static string FlowValue(string flow, string name) {
            var text = flow.Trim();

            if (text.StartsWith("{", StringComparison.Ordinal) && text.EndsWith("}", StringComparison.Ordinal)) {
                text = text.Substring(1, text.Length - 2);
            }

            foreach (var part in TopLevelParts(text)) {
                var colon = part.IndexOf(':');

                if (colon > 0 && part.Substring(0, colon).Trim() == name) {
                    return part.Substring(colon + 1).Trim();
                }
            }

            return string.Empty;
        }

        private static List<string> TopLevelParts(string body) {
            var parts = new List<string>();
            var depth = 0;
            var quote = '\0';
            var start = 0;

            for (var index = 0; index < body.Length; index++) {
                var character = body[index];

                if (quote != '\0') {
                    if (character == quote) {
                        quote = '\0';
                    }

                    continue;
                }

                if (character == '"' || character == '\'') {
                    quote = character;
                }
                else if (character == '[' || character == '{') {
                    depth++;
                }
                else if ((character == ']' || character == '}') && depth > 0) {
                    depth--;
                }
                else if (character == ',' && depth == 0) {
                    parts.Add(body.Substring(start, index - start));
                    start = index + 1;
                }
            }

            parts.Add(body.Substring(start));
            return parts;
        }
    }
}
