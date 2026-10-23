// Copyright 2026 Spellbound Studio Inc.

using System;
using System.Globalization;
using UnityEngine;

namespace Spellbound.Core.Console {
    /// <summary>
    /// Utility for parsing console command arguments into typed parameters.
    /// Used internally by PresetCommandRegistry for both preset and utility commands.
    /// </summary>
    internal static class CommandParameterParser {
        /// <summary>
        /// Try to parse a string argument into the target type.
        /// </summary>
        public static bool TryParse(string arg, Type targetType, out object result) {
            result = null;

            try {
                // Handle nullable types
                if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Nullable<>))
                    targetType = Nullable.GetUnderlyingType(targetType);

                // String
                if (targetType == typeof(string)) {
                    result = arg;

                    return true;
                }

                // Int
                if (targetType == typeof(int)) {
                    if (!int.TryParse(arg, out var intVal))
                        return false;

                    result = intVal;

                    return true;
                }

                // Float
                if (targetType == typeof(float)) {
                    if (!float.TryParse(arg, NumberStyles.Float, CultureInfo.InvariantCulture, out var floatVal))
                        return false;

                    result = floatVal;

                    return true;
                }

                // Double
                if (targetType == typeof(double)) {
                    if (!double.TryParse(arg, NumberStyles.Float, CultureInfo.InvariantCulture, out var doubleVal))
                        return false;

                    result = doubleVal;

                    return true;
                }

                // Bool
                if (targetType == typeof(bool)) {
                    if (bool.TryParse(arg, out var boolVal)) {
                        result = boolVal;

                        return true;
                    }

                    // Support 1/0
                    if (arg == "1" || arg.Equals("true", StringComparison.OrdinalIgnoreCase)) {
                        result = true;

                        return true;
                    }

                    if (arg != "0" && !arg.Equals("false", StringComparison.OrdinalIgnoreCase))
                        return false;

                    result = false;

                    return true;
                }

                // Byte
                if (targetType == typeof(byte)) {
                    if (!byte.TryParse(arg, out var byteVal))
                        return false;

                    result = byteVal;

                    return true;
                }

                // Enum
                if (targetType is not { IsEnum: true })
                    return false;

                result = Enum.Parse(targetType, arg, true);

                return true;
            }
            catch {
                return false;
            }
        }

        /// <summary>
        /// Try to parse multiple arguments into a Vector3.
        /// Consumes 3 arguments: x y z
        /// </summary>
        public static bool TryParseVector3(string[] args, int startIndex, out Vector3 result) {
            result = Vector3.zero;

            if (startIndex + 2 >= args.Length)
                return false;

            if (!float.TryParse(args[startIndex], NumberStyles.Float, CultureInfo.InvariantCulture, out var x))
                return false;

            if (!float.TryParse(args[startIndex + 1], NumberStyles.Float, CultureInfo.InvariantCulture, out var y))
                return false;

            if (!float.TryParse(args[startIndex + 2], NumberStyles.Float, CultureInfo.InvariantCulture, out var z))
                return false;

            result = new Vector3(x, y, z);

            return true;
        }

        /// <summary>
        /// Try to parse multiple arguments into a Vector2.
        /// Consumes 2 arguments: x y
        /// </summary>
        public static bool TryParseVector2(string[] args, int startIndex, out Vector2 result) {
            result = Vector2.zero;

            if (startIndex + 1 >= args.Length)
                return false;

            if (!float.TryParse(args[startIndex], NumberStyles.Float, CultureInfo.InvariantCulture, out var x))
                return false;

            if (!float.TryParse(args[startIndex + 1], NumberStyles.Float, CultureInfo.InvariantCulture, out var y))
                return false;

            result = new Vector2(x, y);

            return true;
        }

        /// <summary>
        /// Get a human-readable type name for error messages.
        /// </summary>
        public static string GetTypeName(Type type) {
            if (type == typeof(int)) return "int";
            if (type == typeof(float)) return "float";
            if (type == typeof(double)) return "double";
            if (type == typeof(bool)) return "bool";
            if (type == typeof(string)) return "string";
            if (type == typeof(byte)) return "byte";
            if (type == typeof(Vector3)) return "Vector3";

            return type == typeof(Vector2)
                    ? "Vector2"
                    : type.Name;
        }
    }
}