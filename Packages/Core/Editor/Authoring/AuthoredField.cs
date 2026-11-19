// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Collections.Generic;

namespace Spellbound.Core.Authoring.Editor {
    public sealed class AuthoredField {
        public string Owner { get; }
        public string Path { get; }
        public string FieldType { get; }
        public bool IsManagedReference { get; }
        public IReadOnlyList<string> FormerNames { get; }
        public string Line => Owner + "." + Path + " : " + FieldType;

        internal bool IsDeclared { get; }

        internal AuthoredField(string owner, string path, string fieldType, bool isManagedReference,
            IReadOnlyList<string> formerNames) : this(owner, path, fieldType, isManagedReference, formerNames, true) { }

        internal AuthoredField(string owner, string path, string fieldType, bool isManagedReference,
            IReadOnlyList<string> formerNames, bool isDeclared) {
            Owner = owner ?? string.Empty;
            Path = path ?? throw new ArgumentNullException(nameof(path));
            FieldType = fieldType ?? string.Empty;
            IsManagedReference = isManagedReference;
            FormerNames = formerNames ?? Array.Empty<string>();
            IsDeclared = isDeclared;
        }

        public override string ToString() {
            return Line;
        }
    }
}
