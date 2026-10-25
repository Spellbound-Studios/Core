// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Spellbound.Core.Tooling;

namespace Spellbound.Core.Operations {
    public static class BridgeDoors {
        public const string RootNamespace = "Spellbound";
        public const string DoorSuffix = "Kit";

        private const BindingFlags PublicStatic = BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly;

        public static IReadOnlyList<BridgeDoor> Find() {
            return Find(AssemblyScanning.ScannableAssemblies());
        }

        public static IReadOnlyList<BridgeDoor> Find(IEnumerable<Assembly> assemblies) {
            var doors = new List<BridgeDoor>();

            if (assemblies == null) {
                return doors;
            }

            foreach (var assembly in assemblies) {
                if (assembly == null || assembly.IsDynamic) {
                    continue;
                }

                var door = Describe(assembly);

                if (door != null) {
                    doors.Add(door);
                }
            }

            doors.Sort((left, right) => string.CompareOrdinal(left.AssemblyName, right.AssemblyName));
            return doors;
        }

        public static string ExpectedDoorName(string assemblyName) {
            if (string.IsNullOrEmpty(assemblyName)) {
                return null;
            }

            var segments = assemblyName.Split('.');

            if (segments.Length < 3 || !string.Equals(segments[0], RootNamespace, StringComparison.Ordinal)) {
                return null;
            }

            return segments[1] + segments[2] + DoorSuffix;
        }

        private static BridgeDoor Describe(Assembly assembly) {
            var assemblyName = assembly.GetName().Name;
            var doorName = ExpectedDoorName(assemblyName);

            if (doorName == null) {
                return null;
            }

            foreach (var type in AssemblyScanning.LoadableTypes(assembly)) {
                if (!type.IsClass || !type.IsPublic || !type.IsAbstract || !type.IsSealed || !string.Equals(type.Name, doorName, StringComparison.Ordinal)) {
                    continue;
                }

                var segments = assemblyName.Split('.');
                return new BridgeDoor(assemblyName, segments[1], segments[2], type.Name, Members(type));
            }

            return null;
        }

        private static IReadOnlyList<string> Members(Type type) {
            var lines = new List<string>();

            foreach (var property in type.GetProperties(PublicStatic)) {
                lines.Add(Signature(property));
            }

            foreach (var method in type.GetMethods(PublicStatic)) {
                if (!method.IsSpecialName) {
                    lines.Add(Signature(method));
                }
            }

            lines.Sort(string.CompareOrdinal);
            return lines;
        }

        public static string Signature(MethodInfo method) {
            var builder = new StringBuilder("static ");
            builder.Append(TypeName(method.ReturnType)).Append(' ').Append(method.Name);

            if (method.IsGenericMethodDefinition) {
                builder.Append('<');
                var arguments = method.GetGenericArguments();

                for (var i = 0; i < arguments.Length; i++) {
                    builder.Append(i > 0 ? ", " : string.Empty).Append(arguments[i].Name);
                }

                builder.Append('>');
            }

            builder.Append('(');
            var parameters = method.GetParameters();

            for (var i = 0; i < parameters.Length; i++) {
                if (i > 0) {
                    builder.Append(", ");
                }

                if (parameters[i].IsOut) {
                    builder.Append("out ");
                }
                else if (parameters[i].ParameterType.IsByRef) {
                    builder.Append("ref ");
                }

                builder.Append(TypeName(parameters[i].ParameterType)).Append(' ').Append(parameters[i].Name);

                if (parameters[i].HasDefaultValue) {
                    builder.Append(" = ").Append(ArgumentText.Describe(parameters[i].DefaultValue));
                }
            }

            return builder.Append(')').ToString();
        }

        public static string Signature(PropertyInfo property) {
            var builder = new StringBuilder("static ");
            builder.Append(TypeName(property.PropertyType)).Append(' ').Append(property.Name).Append(" { get;");

            if (property.CanWrite && property.SetMethod != null && property.SetMethod.IsPublic) {
                builder.Append(" set;");
            }

            return builder.Append(" }").ToString();
        }

        public static string TypeName(Type type) {
            if (type == null) {
                return "void";
            }

            if (type.IsByRef) {
                return TypeName(type.GetElementType());
            }

            var nullable = Nullable.GetUnderlyingType(type);

            if (nullable != null) {
                return TypeName(nullable) + "?";
            }

            if (type.IsArray) {
                return TypeName(type.GetElementType()) + "[]";
            }

            if (type.IsGenericType) {
                var name = type.Name;
                var tick = name.IndexOf('`');
                var builder = new StringBuilder(tick > 0 ? name.Substring(0, tick) : name).Append('<');
                var arguments = type.GetGenericArguments();

                for (var i = 0; i < arguments.Length; i++) {
                    builder.Append(i > 0 ? ", " : string.Empty).Append(TypeName(arguments[i]));
                }

                return builder.Append('>').ToString();
            }

            return Keywords.TryGetValue(type, out var keyword) ? keyword : type.Name;
        }

        private static readonly Dictionary<Type, string> Keywords = new Dictionary<Type, string> {
            { typeof(void), "void" },
            { typeof(string), "string" },
            { typeof(bool), "bool" },
            { typeof(int), "int" },
            { typeof(uint), "uint" },
            { typeof(long), "long" },
            { typeof(ulong), "ulong" },
            { typeof(short), "short" },
            { typeof(ushort), "ushort" },
            { typeof(byte), "byte" },
            { typeof(sbyte), "sbyte" },
            { typeof(float), "float" },
            { typeof(double), "double" },
            { typeof(decimal), "decimal" },
            { typeof(char), "char" },
            { typeof(object), "object" }
        };
    }
}
