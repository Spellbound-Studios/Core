// Copyright (c) Spellbound Studios. All rights reserved.

namespace Spellbound.Core.Authoring.Editor {
    internal sealed class StoredKey {
        public string Path { get; }
        public string Value { get; }
        public int LineNumber { get; }

        public StoredKey(string path, string value, int lineNumber) {
            Path = path;
            Value = value ?? string.Empty;
            LineNumber = lineNumber;
        }

        public string Name {
            get {
                var dot = Path.LastIndexOf('.');
                return dot < 0 ? Path : Path.Substring(dot + 1);
            }
        }

        public string ParentPath {
            get {
                var dot = Path.LastIndexOf('.');
                return dot < 0 ? string.Empty : Path.Substring(0, dot);
            }
        }

        public StoredKey Relative(string prefix) {
            return new StoredKey(Path.Substring(prefix.Length), Value, LineNumber);
        }

        public bool IsWithin(string ancestorPath) {
            return Path.Length > ancestorPath.Length && Path.StartsWith(ancestorPath, System.StringComparison.Ordinal)
                                                     && (Path[ancestorPath.Length] == '.'
                                                         || Path[ancestorPath.Length] == '[');
        }
    }
}
