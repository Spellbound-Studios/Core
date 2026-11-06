// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Collections.Generic;
using Spellbound.Core.Logging;
using Spellbound.Core.Operations;
using UnityEditor;

namespace Spellbound.Core.Authoring.Editor {
    public sealed class DefinitionCheckRegistry {
        private static DefinitionCheckRegistry _default;

        private readonly Func<IEnumerable<Type>> _typeSource;
        private List<DefinitionCheckDescription> _checks;

        public static DefinitionCheckRegistry Default => _default ??= new DefinitionCheckRegistry(TypeCacheTypes);

        public DefinitionCheckRegistry() : this(TypeCacheTypes) { }

        public DefinitionCheckRegistry(Func<IEnumerable<Type>> typeSource) {
            _typeSource = typeSource ?? TypeCacheTypes;
        }

        public DefinitionCheckRegistry(IEnumerable<DefinitionCheckDescription> checks) {
            _checks = Sorted(checks);
        }

        public IReadOnlyList<DefinitionCheckDescription> Checks {
            get {
                EnsureLoaded();
                return _checks;
            }
        }

        public static IEnumerable<Type> TypeCacheTypes() {
            var types = new List<Type>(TypeCache.GetTypesWithAttribute<RegisteredDefinitionCheckAttribute>());
            types.Sort((left, right) => string.CompareOrdinal(left.FullName, right.FullName));
            return types;
        }

        public static IEnumerable<Type> MarkedAssemblyTypes() {
            return MarkedAssemblyScan.TypesWith<RegisteredDefinitionCheckAttribute>();
        }

        public static void Reset() {
            _default = null;
        }

        public IReadOnlyList<DefinitionCheckDescription> For(Type assetType) {
            var applying = new List<DefinitionCheckDescription>();

            foreach (var check in Checks) {
                if (check.AppliesTo(assetType)) {
                    applying.Add(check);
                }
            }

            return applying;
        }

        public DefinitionCheckDescription Find(string name) {
            foreach (var check in Checks) {
                if (string.Equals(check.Name, name, StringComparison.Ordinal)) {
                    return check;
                }
            }

            return null;
        }

        private void EnsureLoaded() {
            if (_checks != null) {
                return;
            }

            var found = new List<DefinitionCheckDescription>();

            foreach (var type in _typeSource()) {
                try {
                    found.AddRange(DefinitionCheckDescription.From(type));
                }
                catch (Exception exception) {
                    Log.Error("The definition check " + type.FullName + " was not registered: " + exception.Message);
                }
            }

            _checks = Sorted(found);
        }

        private static List<DefinitionCheckDescription> Sorted(IEnumerable<DefinitionCheckDescription> checks) {
            var kept = new List<DefinitionCheckDescription>();

            if (checks == null) {
                return kept;
            }

            foreach (var check in checks) {
                if (check == null) {
                    continue;
                }

                var duplicate = FindDuplicate(kept, check);

                if (duplicate != null) {
                    Log.Error("Two definition checks are registered as '" + check.Name + "' over "
                              + check.DefinitionType.Name + ": " + duplicate.CheckType.FullName + " and "
                              + check.CheckType.FullName + "; the first is kept.");
                    continue;
                }

                kept.Add(check);
            }

            kept.Sort((left, right) => {
                var byName = string.CompareOrdinal(left.Name, right.Name);
                return byName != 0
                    ? byName
                    : string.CompareOrdinal(left.DefinitionType.FullName, right.DefinitionType.FullName);
            });

            return kept;
        }

        private static DefinitionCheckDescription FindDuplicate(List<DefinitionCheckDescription> kept,
            DefinitionCheckDescription check) {
            foreach (var existing in kept) {
                if (string.Equals(existing.Name, check.Name, StringComparison.Ordinal)
                    && existing.DefinitionType == check.DefinitionType && existing.CheckType != check.CheckType) {
                    return existing;
                }
            }

            return null;
        }
    }
}
