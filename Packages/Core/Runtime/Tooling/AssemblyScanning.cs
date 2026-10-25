// Copyright 2026 Spellbound Studio Inc.

using System;
using System.Collections.Generic;
using System.Reflection;

namespace Spellbound.Core.Tooling {
    public static class AssemblyScanning {
        public static IEnumerable<Assembly> ScannableAssemblies() {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) {
                if (!ShouldSkip(assembly.GetName().Name))
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

        private static bool ShouldSkip(string name) =>
                string.IsNullOrEmpty(name)
                || name.StartsWith("System")
                || name.StartsWith("mscorlib")
                || name.StartsWith("netstandard")
                || name.StartsWith("Mono.")
                || name.StartsWith("nunit")
                || name.StartsWith("UnityEngine")
                || name.StartsWith("UnityEditor")
                || name.StartsWith("Unity.");
    }
}
