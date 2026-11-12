// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Collections.Generic;
using Spellbound.Core.Authoring;

namespace Spellbound.Core.Controls.Editor {
    public static class HeaderStates {
        public const string Neutral = "neutral";
        public const string Passed = "passed";
        public const string Information = "information";
        public const string Warning = "warning";
        public const string Error = "error";
        public const string Stale = "stale";
        public const string ClassPrefix = HeaderBar.ClassName + "--";

        public static IReadOnlyList<string> Shipped { get; } = new[] {
            Neutral, Passed, Information, Warning, Error, Stale
        };

        public static string For(FindingSeverity severity) {
            switch (severity) {
                case FindingSeverity.Error:
                    return Error;
                case FindingSeverity.Warning:
                    return Warning;
                default:
                    return Information;
            }
        }

        public static string ClassNameFor(string state) {
            return ClassPrefix + Normalize(state);
        }

        public static string Normalize(string state) {
            if (string.IsNullOrWhiteSpace(state)) {
                return Neutral;
            }

            var id = state.Trim().ToLowerInvariant();

            foreach (var character in id) {
                if (!(character >= 'a' && character <= 'z') && !(character >= '0' && character <= '9')
                    && character != '-') {
                    throw new ArgumentException(
                        "A header state is one id of lower-case letters, digits and dashes: '" + state + "'.",
                        nameof(state));
                }
            }

            return id;
        }
    }
}
