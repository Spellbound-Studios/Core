// Copyright (c) Spellbound Studios. All rights reserved.

using System;

namespace Spellbound.Core.Logging.Editor {
    public sealed class LogSinkDescription {
        public string QualifiedTypeName { get; }
        public string DisplayName { get; }

        public LogSinkDescription(string qualifiedTypeName, string displayName) {
            if (string.IsNullOrWhiteSpace(qualifiedTypeName)) {
                throw new ArgumentException("A log sink is named by its assembly-qualified type name.",
                    nameof(qualifiedTypeName));
            }

            QualifiedTypeName = qualifiedTypeName;
            DisplayName = displayName ?? string.Empty;
        }

        public override string ToString() {
            return DisplayName + " (" + QualifiedTypeName + ")";
        }
    }
}
