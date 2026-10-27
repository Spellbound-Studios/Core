// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using Spellbound.Core.Authoring;

namespace Spellbound.Core.Controls.Editor {
    public sealed class FindingAction {
        public string Label { get; }
        public Action<ValidationFinding> Run { get; }

        public FindingAction(string label, Action<ValidationFinding> run) {
            if (string.IsNullOrWhiteSpace(label)) {
                throw new ArgumentException("A finding action needs a label.", nameof(label));
            }

            Label = label.Trim();
            Run = run ?? throw new ArgumentNullException(nameof(run));
        }
    }
}
