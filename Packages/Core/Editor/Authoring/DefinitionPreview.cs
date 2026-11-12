// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Collections.Generic;

namespace Spellbound.Core.Authoring.Editor {
    public sealed class DefinitionPreview {
        public string Summary { get; }
        public IReadOnlyList<string> Pictures { get; }
        public IReadOnlyList<string> Lines { get; }

        public DefinitionPreview(string summary, IReadOnlyList<string> pictures = null,
            IReadOnlyList<string> lines = null) {
            Summary = string.IsNullOrWhiteSpace(summary) ? string.Empty : summary.Trim();
            Pictures = pictures ?? Array.Empty<string>();
            Lines = lines ?? Array.Empty<string>();
        }
    }
}
