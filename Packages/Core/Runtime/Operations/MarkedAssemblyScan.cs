// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Collections.Generic;
using System.Reflection;
using Spellbound.Core.Tooling;

namespace Spellbound.Core.Operations {
    public static class MarkedAssemblyScan {
        public static IReadOnlyList<Assembly> Assemblies() {
            var loaded = new List<Assembly>();

            foreach (var assembly in AssemblyScanning.ScannableAssemblies()) {
                if (!assembly.IsDynamic) {
                    loaded.Add(assembly);
                }
            }

            return Assemblies(loaded);
        }

        public static IReadOnlyList<Assembly> Assemblies(IEnumerable<Assembly> candidates) {
            var marked = new List<Assembly>();

            if (candidates == null) {
                return marked;
            }

            foreach (var assembly in candidates) {
                if (assembly != null && assembly.GetCustomAttribute<OperationGroupAttribute>() != null) {
                    marked.Add(assembly);
                }
            }

            marked.Sort((left, right) => string.CompareOrdinal(left.GetName().Name, right.GetName().Name));
            return marked;
        }

        public static IReadOnlyList<Type> TypesWith<TAttribute>() where TAttribute : Attribute {
            return TypesWith<TAttribute>(Assemblies());
        }

        public static IReadOnlyList<Type> TypesWith<TAttribute>(IEnumerable<Assembly> markedAssemblies) where TAttribute : Attribute {
            var found = new List<Type>();

            if (markedAssemblies == null) {
                return found;
            }

            foreach (var assembly in markedAssemblies) {
                var types = new List<Type>(AssemblyScanning.LoadableTypes(assembly));
                types.Sort((left, right) => string.CompareOrdinal(left.FullName, right.FullName));

                foreach (var type in types) {
                    if (type.GetCustomAttribute<TAttribute>(false) != null) {
                        found.Add(type);
                    }
                }
            }

            return found;
        }
    }
}
