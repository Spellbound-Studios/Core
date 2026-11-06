// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Spellbound.Core.Operations {
    internal static class BuiltInArgumentParsers {
        private const string Integer = "integer";
        private const string Number = "number";
        private const NumberStyles IntegerStyles = NumberStyles.Integer | NumberStyles.AllowThousands;
        private const NumberStyles FloatStyles = NumberStyles.Float | NumberStyles.AllowThousands;

        public static void Register(ArgumentParserRegistry registry) {
            registry.Register(new DelegateArgumentParser<string>("text", (string text, out string value) => {
                value = text;
                return true;
            }));
            registry.Register(new DelegateArgumentParser<int>(Integer, (string text, out int value) =>
                int.TryParse(text, IntegerStyles, CultureInfo.InvariantCulture, out value)));
            registry.Register(new DelegateArgumentParser<long>(Integer, (string text, out long value) =>
                long.TryParse(text, IntegerStyles, CultureInfo.InvariantCulture, out value)));
            registry.Register(new DelegateArgumentParser<short>(Integer, (string text, out short value) =>
                short.TryParse(text, IntegerStyles, CultureInfo.InvariantCulture, out value)));
            registry.Register(new DelegateArgumentParser<byte>(Integer, (string text, out byte value) =>
                byte.TryParse(text, IntegerStyles, CultureInfo.InvariantCulture, out value)));
            registry.Register(new DelegateArgumentParser<sbyte>(Integer, (string text, out sbyte value) =>
                sbyte.TryParse(text, IntegerStyles, CultureInfo.InvariantCulture, out value)));
            registry.Register(new DelegateArgumentParser<uint>(Integer, (string text, out uint value) =>
                uint.TryParse(text, IntegerStyles, CultureInfo.InvariantCulture, out value)));
            registry.Register(new DelegateArgumentParser<ulong>(Integer, (string text, out ulong value) =>
                ulong.TryParse(text, IntegerStyles, CultureInfo.InvariantCulture, out value)));
            registry.Register(new DelegateArgumentParser<ushort>(Integer, (string text, out ushort value) =>
                ushort.TryParse(text, IntegerStyles, CultureInfo.InvariantCulture, out value)));
            registry.Register(new DelegateArgumentParser<float>(Number, (string text, out float value) =>
                float.TryParse(text, FloatStyles, CultureInfo.InvariantCulture, out value)));
            registry.Register(new DelegateArgumentParser<double>(Number, (string text, out double value) =>
                double.TryParse(text, FloatStyles, CultureInfo.InvariantCulture, out value)));
            registry.Register(new DelegateArgumentParser<decimal>(Number, (string text, out decimal value) =>
                decimal.TryParse(text, FloatStyles, CultureInfo.InvariantCulture, out value)));
            registry.Register(new DelegateArgumentParser<bool>("true or false", TryParseBoolean));
            registry.Register(new DelegateArgumentParser<Vector2>("x,y", (string text, out Vector2 value) => {
                value = default;

                if (!TryParseComponents(text, 2, out var parts)) {
                    return false;
                }

                value = new Vector2(parts[0], parts[1]);
                return true;
            }));
            registry.Register(new DelegateArgumentParser<Vector3>("x,y,z", (string text, out Vector3 value) => {
                value = default;

                if (!TryParseComponents(text, 3, out var parts)) {
                    return false;
                }

                value = new Vector3(parts[0], parts[1], parts[2]);
                return true;
            }));
            registry.Register(new DelegateArgumentParser<Vector4>("x,y,z,w", (string text, out Vector4 value) => {
                value = default;

                if (!TryParseComponents(text, 4, out var parts)) {
                    return false;
                }

                value = new Vector4(parts[0], parts[1], parts[2], parts[3]);
                return true;
            }));
            registry.Register(new DelegateArgumentParser<Vector2Int>("x,y as integers", (string text, out Vector2Int value) => {
                value = default;

                if (!TryParseIntegerComponents(text, 2, out var parts)) {
                    return false;
                }

                value = new Vector2Int(parts[0], parts[1]);
                return true;
            }));
            registry.Register(new DelegateArgumentParser<Vector3Int>("x,y,z as integers", (string text, out Vector3Int value) => {
                value = default;

                if (!TryParseIntegerComponents(text, 3, out var parts)) {
                    return false;
                }

                value = new Vector3Int(parts[0], parts[1], parts[2]);
                return true;
            }));
            registry.Register(new DelegateArgumentParser<Quaternion>("euler x,y,z", (string text, out Quaternion value) => {
                value = Quaternion.identity;

                if (!TryParseComponents(text, 3, out var parts)) {
                    return false;
                }

                value = Quaternion.Euler(parts[0], parts[1], parts[2]);
                return true;
            }));
            registry.Register(new DelegateArgumentParser<Color>("colour as #rrggbb, a colour name or r,g,b[,a]", TryParseColor));
            registry.Register(new EnumArgumentTypeParser());
            registry.Register(new CollectionArgumentTypeParser(registry));
            registry.Register(new SceneObjectArgumentTypeParser());
        }

        private static bool TryParseBoolean(string text, out bool value) {
            switch (text.Trim().ToLowerInvariant()) {
                case "true":
                case "1":
                case "yes":
                case "on":
                    value = true;
                    return true;
                case "false":
                case "0":
                case "no":
                case "off":
                    value = false;
                    return true;
                default:
                    value = false;
                    return false;
            }
        }

        private static bool TryParseColor(string text, out Color value) {
            value = default;
            var trimmed = text.Trim();

            if (trimmed.Length == 0) {
                return false;
            }

            if (!trimmed.Contains(",") && ColorUtility.TryParseHtmlString(trimmed, out value)) {
                return true;
            }

            if (!TryParseComponents(trimmed, 3, 4, out var parts)) {
                return false;
            }

            value = new Color(parts[0], parts[1], parts[2], parts.Length > 3 ? parts[3] : 1f);
            return true;
        }

        private static bool TryParseComponents(string text, int count, out float[] parts) {
            return TryParseComponents(text, count, count, out parts);
        }

        private static bool TryParseComponents(string text, int minimum, int maximum, out float[] parts) {
            parts = null;
            var pieces = Split(text);

            if (pieces.Count < minimum || pieces.Count > maximum) {
                return false;
            }

            parts = new float[pieces.Count];

            for (var i = 0; i < pieces.Count; i++) {
                if (!float.TryParse(pieces[i], FloatStyles, CultureInfo.InvariantCulture, out parts[i])) {
                    return false;
                }
            }

            return true;
        }

        private static bool TryParseIntegerComponents(string text, int count, out int[] parts) {
            parts = null;
            var pieces = Split(text);

            if (pieces.Count != count) {
                return false;
            }

            parts = new int[count];

            for (var i = 0; i < count; i++) {
                if (!int.TryParse(pieces[i], IntegerStyles, CultureInfo.InvariantCulture, out parts[i])) {
                    return false;
                }
            }

            return true;
        }

        private static List<string> Split(string text) {
            var pieces = new List<string>();

            foreach (var piece in text.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries)) {
                pieces.Add(piece.Trim());
            }

            return pieces;
        }
    }
}
