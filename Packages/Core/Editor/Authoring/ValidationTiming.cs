// Copyright (c) Spellbound Studios. All rights reserved.

namespace Spellbound.Core.Authoring.Editor {
    public sealed class ValidationTiming {
        public string Check { get; }
        public string Asset { get; }
        public double Milliseconds { get; }

        public ValidationTiming(string check, string asset, double milliseconds) {
            Check = check;
            Asset = asset;
            Milliseconds = milliseconds;
        }
    }
}
