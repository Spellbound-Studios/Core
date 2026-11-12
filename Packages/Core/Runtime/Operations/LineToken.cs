// Copyright (c) Spellbound Studios. All rights reserved.

namespace Spellbound.Core.Operations {
    internal readonly struct LineToken {
        public string Text { get; }
        public bool Literal { get; }

        public LineToken(string text, bool literal) {
            Text = text;
            Literal = literal;
        }

        public bool IsOption => !Literal && Text.Length >= 3 && Text.StartsWith("--") && char.IsLetter(Text[2]);

        public override string ToString() {
            return Text;
        }
    }
}
