// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Spellbound.Core.Hashing;

namespace Spellbound.Core.Operations {
    public sealed class OperationDescription {
        public string Group { get; }
        public string Name { get; }
        public string Summary { get; }
        public OperationEffect Effect { get; }
        public string Audience { get; }
        public OperationAvailability Availability { get; }
        public IReadOnlyList<ArgumentDescription> Arguments { get; }
        public uint Identifier { get; }
        public string Line => Group + " " + Name;

        internal MethodInfo Method { get; }
        internal object Instance { get; }
        internal int ContextParameterIndex { get; }
        internal string AssemblyName => Method.DeclaringType?.Assembly.GetName().Name;

        internal OperationDescription(string group, OperationAttribute attribute, MethodInfo method, object instance,
            IReadOnlyList<ArgumentDescription> arguments, int contextParameterIndex) {
            Group = group;
            Name = attribute.Name;
            Summary = attribute.Description;
            Effect = attribute.Effect;
            Audience = string.IsNullOrWhiteSpace(attribute.Audience) ? OperationAudiences.Developer : attribute.Audience;
            Availability = attribute.Availability;
            Arguments = arguments;
            Method = method;
            Instance = instance;
            ContextParameterIndex = contextParameterIndex;
            Identifier = ComputeIdentifier(group, attribute.Name);
        }

        public static uint ComputeIdentifier(string group, string name) {
            return StableHash.Fnv1A32(group + "." + name);
        }

        public string Usage() {
            var builder = new StringBuilder(Line);

            foreach (var argument in Arguments) {
                builder.Append(' ');

                if (argument.HasDefault) {
                    builder.Append("[--").Append(argument.Name).Append(" <").Append(TypeWord(argument.Type)).Append('>');

                    if (argument.Default != null) {
                        builder.Append(" = ").Append(argument.DefaultText);
                    }

                    builder.Append(']');
                }
                else {
                    builder.Append("--").Append(argument.Name).Append(" <").Append(TypeWord(argument.Type)).Append('>');
                }
            }

            return builder.ToString();
        }

        internal static string TypeWord(Type type) {
            var underlying = Nullable.GetUnderlyingType(type) ?? type;

            if (underlying.IsEnum) {
                return "one of " + string.Join(", ", Enum.GetNames(underlying));
            }

            if (underlying == typeof(string)) {
                return "text";
            }

            if (underlying == typeof(bool)) {
                return "true or false";
            }

            if (underlying == typeof(int) || underlying == typeof(long) || underlying == typeof(short)
                || underlying == typeof(byte) || underlying == typeof(sbyte) || underlying == typeof(uint)
                || underlying == typeof(ulong) || underlying == typeof(ushort)) {
                return "integer";
            }

            if (underlying == typeof(float) || underlying == typeof(double) || underlying == typeof(decimal)) {
                return "number";
            }

            if (underlying.IsArray) {
                return TypeWord(underlying.GetElementType()) + " list";
            }

            if (underlying.IsGenericType && underlying.GetGenericTypeDefinition() == typeof(List<>)) {
                return TypeWord(underlying.GetGenericArguments()[0]) + " list";
            }

            return ArgumentText.KebabCase(underlying.Name);
        }

        public override string ToString() {
            return Line;
        }
    }
}
