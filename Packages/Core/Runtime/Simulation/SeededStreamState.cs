// Copyright (c) Spellbound Studios. All rights reserved.

using System;

namespace Spellbound.Core.Simulation {
    public readonly struct SeededStreamState : IEquatable<SeededStreamState> {
        public readonly ulong FirstWord;
        public readonly ulong SecondWord;
        public readonly ulong ThirdWord;
        public readonly ulong FourthWord;

        public SeededStreamState(ulong firstWord, ulong secondWord, ulong thirdWord, ulong fourthWord) {
            FirstWord = firstWord;
            SecondWord = secondWord;
            ThirdWord = thirdWord;
            FourthWord = fourthWord;
        }

        public bool IsZero => FirstWord == 0 && SecondWord == 0 && ThirdWord == 0 && FourthWord == 0;

        public bool Equals(SeededStreamState other) =>
            FirstWord == other.FirstWord && SecondWord == other.SecondWord && ThirdWord == other.ThirdWord
            && FourthWord == other.FourthWord;

        public override bool Equals(object obj) => obj is SeededStreamState other && Equals(other);

        public override int GetHashCode() {
            unchecked {
                var hash = FirstWord.GetHashCode();
                hash = hash * 31 + SecondWord.GetHashCode();
                hash = hash * 31 + ThirdWord.GetHashCode();
                return hash * 31 + FourthWord.GetHashCode();
            }
        }

        public static bool operator ==(SeededStreamState left, SeededStreamState right) => left.Equals(right);

        public static bool operator !=(SeededStreamState left, SeededStreamState right) => !left.Equals(right);
    }
}
