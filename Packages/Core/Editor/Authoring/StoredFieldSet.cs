// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Collections.Generic;

namespace Spellbound.Core.Authoring.Editor {
    internal sealed class StoredFieldSet {
        private readonly HashSet<string> _paths = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<string> _managedReferences = new HashSet<string>(StringComparer.Ordinal);
        private readonly Dictionary<string, string> _currentByFormerPath =
            new Dictionary<string, string>(StringComparer.Ordinal);

        public StoredFieldSet(IReadOnlyList<AuthoredField> fields) {
            foreach (var field in fields) {
                _paths.Add(field.Path);

                if (field.IsManagedReference) {
                    _managedReferences.Add(field.Path);
                }

                var dot = field.Path.LastIndexOf('.');
                var parent = dot < 0 ? string.Empty : field.Path.Substring(0, dot + 1);

                foreach (var formerName in field.FormerNames) {
                    _currentByFormerPath[parent + formerName] = field.Path;
                }
            }
        }

        public bool Holds(string normalizedPath) {
            return _paths.Contains(normalizedPath);
        }

        public bool IsManagedReference(string normalizedPath) {
            if (_managedReferences.Contains(normalizedPath)) {
                return true;
            }

            return normalizedPath.EndsWith(AuthoredFields.ElementMarker, StringComparison.Ordinal)
                   && _managedReferences.Contains(
                       normalizedPath.Substring(0, normalizedPath.Length - AuthoredFields.ElementMarker.Length));
        }

        public bool TryCurrentName(string normalizedPath, out string currentPath) {
            return _currentByFormerPath.TryGetValue(normalizedPath, out currentPath);
        }
    }
}
