// Copyright (c) Spellbound Studios. All rights reserved.

using System;

namespace Spellbound.Core.Operations {
    public sealed class InstalledPackageDescription {
        public string Name { get; }
        public string DisplayName { get; }
        public string Version { get; }
        public string Path { get; }
        public string Line => Name + " " + Version;

        public InstalledPackageDescription(string name, string displayName, string version, string path) {
            if (string.IsNullOrWhiteSpace(name)) {
                throw new ArgumentException("A package name must not be null, empty or whitespace.", nameof(name));
            }

            Name = name;
            DisplayName = displayName ?? name;
            Version = version ?? string.Empty;
            Path = path;
        }

        public override string ToString() {
            return Line;
        }
    }
}
