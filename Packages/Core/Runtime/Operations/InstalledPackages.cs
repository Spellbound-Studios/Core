// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Collections.Generic;

namespace Spellbound.Core.Operations {
    public static class InstalledPackages {
        public static Func<IReadOnlyList<InstalledPackageDescription>> Source { get; set; }

        public static IReadOnlyList<InstalledPackageDescription> All() {
            return Source?.Invoke() ?? Array.Empty<InstalledPackageDescription>();
        }

        public static InstalledPackageDescription Find(string nameOrDisplayName) {
            if (string.IsNullOrWhiteSpace(nameOrDisplayName)) {
                return null;
            }

            foreach (var package in All()) {
                if (string.Equals(package.Name, nameOrDisplayName, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(package.DisplayName, nameOrDisplayName, StringComparison.OrdinalIgnoreCase)) {
                    return package;
                }
            }

            return null;
        }
    }
}
