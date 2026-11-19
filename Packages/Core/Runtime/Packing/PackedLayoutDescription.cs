// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Spellbound.Core.Packing {
    internal static class PackedLayoutDescription {
        private const BindingFlags FieldFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

        private static readonly string[] NonExpandedAssemblyPrefixes = {
            "System", "mscorlib", "netstandard", "UnityEngine", "Unity."
        };

        public static string Describe(Type type) {
            if (type == null) {
                throw new ArgumentNullException(nameof(type));
            }

            var builder = new StringBuilder();
            builder.Append(TypeName(type));
            var expanding = new List<Type> { type };
            AppendFields(builder, type, 1, expanding);
            return builder.ToString();
        }

        private static void AppendFields(StringBuilder builder, Type type, int depth, List<Type> expanding) {
            var chain = new List<Type>();
            for (var current = type;
                 current != null && current != typeof(object) && current != typeof(ValueType);
                 current = current.BaseType) {
                chain.Add(current);
            }

            chain.Reverse();
            foreach (var declaringType in chain) {
                var fields = declaringType.GetFields(FieldFlags);
                Array.Sort(fields, (left, right) => left.MetadataToken.CompareTo(right.MetadataToken));
                foreach (var field in fields) {
                    AppendField(builder, field, depth, expanding);
                }
            }
        }

        private static void AppendField(StringBuilder builder, FieldInfo field, int depth, List<Type> expanding) {
            var fieldType = field.FieldType;
            builder.Append('\n').Append(' ', depth * 2).Append(field.Name).Append(" : ");
            if (fieldType.IsEnum) {
                builder.Append(TypeName(fieldType))
                    .Append('(')
                    .Append(TypeName(Enum.GetUnderlyingType(fieldType)))
                    .Append(')');
                return;
            }

            builder.Append(TypeName(fieldType));
            if (ShouldExpand(fieldType) && !expanding.Contains(fieldType)) {
                expanding.Add(fieldType);
                AppendFields(builder, fieldType, depth + 1, expanding);
                expanding.RemoveAt(expanding.Count - 1);
            }
        }

        private static bool ShouldExpand(Type type) {
            if (!type.IsValueType || type.IsPrimitive || type.IsEnum || type.IsGenericType || type.IsPointer) {
                return false;
            }

            var assemblyName = type.Assembly.GetName().Name;
            foreach (var prefix in NonExpandedAssemblyPrefixes) {
                if (assemblyName.StartsWith(prefix, StringComparison.Ordinal)) {
                    return false;
                }
            }

            return true;
        }

        private static string TypeName(Type type) {
            if (type.IsArray) {
                return TypeName(type.GetElementType()) + "[]";
            }

            if (type.IsGenericType && !type.IsGenericTypeDefinition) {
                var definitionName = type.GetGenericTypeDefinition().FullName ?? type.Name;
                var backtick = definitionName.IndexOf('`');
                if (backtick >= 0) {
                    definitionName = definitionName.Substring(0, backtick);
                }

                var arguments = type.GetGenericArguments();
                var names = new string[arguments.Length];
                for (var index = 0; index < arguments.Length; index++) {
                    names[index] = TypeName(arguments[index]);
                }

                return definitionName + "<" + string.Join(", ", names) + ">";
            }

            return type.FullName ?? type.Name;
        }
    }
}
