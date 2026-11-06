// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Collections.Generic;
using System.Reflection;

namespace Spellbound.Core.Tooling {
    public static class AssemblyScanning {
        public static IEnumerable<Assembly> ScannableAssemblies() {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) {
                if (!IsSkipped(assembly.GetName().Name))
                    yield return assembly;
            }
        }

        public static IReadOnlyList<Type> LoadableTypes(Assembly assembly) {
            try {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e) {
                var loaded = new List<Type>();

                foreach (var type in e.Types) {
                    if (type != null)
                        loaded.Add(type);
                }

                return loaded;
            }
        }

        public static bool IsSkipped(string assemblyName) {
            if (string.IsNullOrEmpty(assemblyName)) {
                return true;
            }

            return string.Equals(assemblyName, "mscorlib", StringComparison.Ordinal)
                   || string.Equals(assemblyName, "netstandard", StringComparison.Ordinal)
                   || BelongsTo(assemblyName, "System")
                   || BelongsTo(assemblyName, "Mono")
                   || BelongsTo(assemblyName, "nunit")
                   || BelongsTo(assemblyName, "UnityEngine")
                   || BelongsTo(assemblyName, "UnityEditor")
                   || BelongsTo(assemblyName, "Unity");
        }

        private static bool BelongsTo(string assemblyName, string family) {
            if (string.Equals(assemblyName, family, StringComparison.Ordinal)) {
                return true;
            }

            return assemblyName.Length > family.Length
                   && assemblyName[family.Length] == '.'
                   && assemblyName.StartsWith(family, StringComparison.Ordinal);
        }
    }
}
