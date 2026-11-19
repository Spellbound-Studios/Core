// Copyright (c) Spellbound Studios. All rights reserved.

namespace Spellbound.Core.Controls.Editor {
    public static class ChipTones {
        public const string ClassName = "sb-chip";
        public const string ClassPrefix = ClassName + "--";
        public const string NeutralClassName = ClassPrefix + HeaderStates.Neutral;
        public const string PassedClassName = ClassPrefix + HeaderStates.Passed;
        public const string InformationClassName = ClassPrefix + HeaderStates.Information;
        public const string WarningClassName = ClassPrefix + HeaderStates.Warning;
        public const string ErrorClassName = ClassPrefix + HeaderStates.Error;
        public const string StaleClassName = ClassPrefix + HeaderStates.Stale;

        public static string ClassNameFor(string tone) {
            return ClassPrefix + HeaderStates.Normalize(tone);
        }
    }
}
