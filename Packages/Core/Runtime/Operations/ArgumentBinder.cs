// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Collections.Generic;

namespace Spellbound.Core.Operations {
    internal static class ArgumentBinder {
        public const string ConfirmFlag = "confirm";
        public const string DryRunFlag = "dry-run";

        public sealed class Bound {
            public object[] Values;
            public bool Confirmed;
            public bool DryRun;
        }

        public static bool TryBind(OperationDescription operation, List<LineToken> tokens, ArgumentParserRegistry parsers,
            out Bound bound, out string error) {
            bound = new Bound { Values = new object[operation.Arguments.Count] };
            error = null;
            var assigned = new bool[operation.Arguments.Count];
            var texts = new string[operation.Arguments.Count];
            var nextPositional = 0;

            for (var i = 0; i < tokens.Count; i++) {
                var token = tokens[i].Text;

                if (tokens[i].IsOption) {
                    var name = token.Substring(2);
                    string inlineValue = null;
                    var equals = name.IndexOf('=');

                    if (equals >= 0) {
                        inlineValue = name.Substring(equals + 1);
                        name = name.Substring(0, equals);
                    }

                    if (string.Equals(name, ConfirmFlag, StringComparison.OrdinalIgnoreCase)) {
                        bound.Confirmed = inlineValue == null || IsTruthy(inlineValue);
                        continue;
                    }

                    if (string.Equals(name, DryRunFlag, StringComparison.OrdinalIgnoreCase)) {
                        bound.DryRun = inlineValue == null || IsTruthy(inlineValue);
                        continue;
                    }

                    var index = IndexOf(operation, name);

                    if (index < 0) {
                        error = "no argument '--" + name + "' on " + operation.Line + "; " + ArgumentList(operation);
                        return false;
                    }

                    if (inlineValue != null) {
                        texts[index] = inlineValue;
                        assigned[index] = true;
                        continue;
                    }

                    var isFlag = (Nullable.GetUnderlyingType(operation.Arguments[index].Type) ?? operation.Arguments[index].Type) == typeof(bool);
                    var hasValue = i + 1 < tokens.Count && !tokens[i + 1].IsOption;

                    if (isFlag && (!hasValue || !IsBooleanWord(tokens[i + 1].Text))) {
                        texts[index] = "true";
                        assigned[index] = true;
                        continue;
                    }

                    if (!hasValue) {
                        error = "--" + name + " needs a value (expected: " + parsers.Expected(operation.Arguments[index].Type) + ")";
                        return false;
                    }

                    texts[index] = tokens[++i].Text;
                    assigned[index] = true;
                    continue;
                }

                while (nextPositional < assigned.Length && assigned[nextPositional]) {
                    nextPositional++;
                }

                if (nextPositional >= assigned.Length) {
                    error = "unexpected value '" + token + "'; " + ArgumentList(operation);
                    return false;
                }

                texts[nextPositional] = token;
                assigned[nextPositional] = true;
            }

            for (var i = 0; i < operation.Arguments.Count; i++) {
                var argument = operation.Arguments[i];

                if (!assigned[i]) {
                    if (argument.HasDefault) {
                        bound.Values[i] = argument.Default;
                        continue;
                    }

                    error = "--" + argument.Name + " is required (expected: " + parsers.Expected(argument.Type) + ")";
                    return false;
                }

                if (!parsers.CanParse(argument.Type)) {
                    error = "--" + argument.Name + ": no parser knows the type " + argument.Type.Name
                            + "; write an IArgumentParser<" + argument.Type.Name + "> marked [ArgumentParser]";
                    return false;
                }

                if (parsers.TryParse(texts[i], argument.Type, out var value)) {
                    bound.Values[i] = value;
                    continue;
                }

                var legal = parsers.LegalValues(argument.Type);

                if (legal.Count > 0) {
                    error = "--" + argument.Name + ": '" + texts[i] + "' is not one of " + string.Join(", ", legal);
                    return false;
                }

                error = "--" + argument.Name + ": '" + texts[i] + "' is not " + Article(parsers.Expected(argument.Type))
                        + " (expected: " + parsers.Expected(argument.Type)
                        + (argument.HasDefault ? ", default " + argument.DefaultText : string.Empty) + ")";
                return false;
            }

            return true;
        }

        public static bool TryBindValues(OperationDescription operation, IReadOnlyList<object> values, out object[] bound,
            out string error) {
            bound = new object[operation.Arguments.Count];
            error = null;

            if (values.Count != operation.Arguments.Count) {
                error = operation.Line + " takes " + operation.Arguments.Count + " values; " + values.Count + " were given";
                return false;
            }

            for (var i = 0; i < values.Count; i++) {
                var argument = operation.Arguments[i];
                var value = values[i];

                if (value == null) {
                    if (argument.Type.IsValueType && Nullable.GetUnderlyingType(argument.Type) == null) {
                        error = "--" + argument.Name + " cannot be null";
                        return false;
                    }

                    continue;
                }

                if (!argument.Type.IsInstanceOfType(value)) {
                    error = "--" + argument.Name + ": a " + value.GetType().Name + " is not a " + argument.Type.Name;
                    return false;
                }

                bound[i] = value;
            }

            return true;
        }

        private static int IndexOf(OperationDescription operation, string name) {
            for (var i = 0; i < operation.Arguments.Count; i++) {
                if (string.Equals(operation.Arguments[i].Name, name, StringComparison.OrdinalIgnoreCase)) {
                    return i;
                }
            }

            return -1;
        }

        private static string ArgumentList(OperationDescription operation) {
            if (operation.Arguments.Count == 0) {
                return operation.Line + " takes no arguments";
            }

            var names = new string[operation.Arguments.Count];

            for (var i = 0; i < names.Length; i++) {
                names[i] = "--" + operation.Arguments[i].Name;
            }

            return operation.Line + " takes " + string.Join(", ", names);
        }

        private static bool IsBooleanWord(string token) {
            switch (token.ToLowerInvariant()) {
                case "true":
                case "false":
                case "1":
                case "0":
                case "yes":
                case "no":
                case "on":
                case "off":
                    return true;
                default:
                    return false;
            }
        }

        private static bool IsTruthy(string text) {
            switch (text.ToLowerInvariant()) {
                case "false":
                case "0":
                case "no":
                case "off":
                    return false;
                default:
                    return true;
            }
        }

        private static string Article(string expected) {
            if (string.IsNullOrEmpty(expected)) {
                return "valid";
            }

            if (expected.StartsWith("one of") || expected.StartsWith("comma") || expected.StartsWith("true")
                || expected.StartsWith("scene:") || expected.StartsWith("asset:")) {
                return expected;
            }

            return ("aeiou".IndexOf(char.ToLowerInvariant(expected[0])) >= 0 ? "an " : "a ") + expected;
        }
    }
}
