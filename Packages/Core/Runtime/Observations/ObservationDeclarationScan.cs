// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using Spellbound.Core.Logging;
using Spellbound.Core.Tooling;

namespace Spellbound.Core.Observations {
    internal static class ObservationDeclarationScan {
        private const BindingFlags DeclaredStaticFields =
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic
                                                          | BindingFlags.DeclaredOnly;

        private static readonly string CoreAssemblyName = typeof(ObservationDeclarationScan).Assembly.GetName().Name;
        private static bool _scanned;

        internal static void Run() {
            if (_scanned) {
                return;
            }

            _scanned = true;

            foreach (var assembly in AssemblyScanning.ScannableAssemblies()) {
                if (assembly.IsDynamic || !ReferencesCore(assembly)) {
                    continue;
                }

                try {
                    ScanAssembly(assembly);
                }
                catch (Exception exception) {
                    Log.Error("The observation declarations of " + assembly.GetName().Name + " could not be scanned: "
                              + exception.GetType().Name + ": " + exception.Message);
                }
            }
        }

        internal static void Reset() {
            _scanned = false;
        }

        private static bool ReferencesCore(Assembly assembly) {
            if (string.Equals(assembly.GetName().Name, CoreAssemblyName, StringComparison.Ordinal)) {
                return true;
            }

            foreach (var referenced in assembly.GetReferencedAssemblies()) {
                if (string.Equals(referenced.Name, CoreAssemblyName, StringComparison.Ordinal)) {
                    return true;
                }
            }

            return false;
        }

        private static void ScanAssembly(Assembly assembly) {
            foreach (var type in AssemblyScanning.LoadableTypes(assembly)) {
                if (type.ContainsGenericParameters || !type.IsClass) {
                    continue;
                }

                var fields = DeclarationOnlyFields(type);

                if (fields == null) {
                    continue;
                }

                try {
                    RuntimeHelpers.RunClassConstructor(type.TypeHandle);
                }
                catch (TypeInitializationException exception) {
                    var inner = exception.InnerException ?? exception;
                    Log.Error("The observations of " + type.FullName + " could not be declared: "
                              + inner.GetType().Name + ": "
                              + inner.Message);
                    continue;
                }

                foreach (var field in fields) {
                    Ensure(field.GetValue(null));
                }
            }
        }

        private static void Ensure(object declared) {
            switch (declared) {
                case Counter counter:
                    ObservationRegistry.Ensure(counter.Declaration);
                    break;
                case Occurrence occurrence:
                    ObservationRegistry.Ensure(occurrence.Declaration);
                    break;
                case Gauge gauge:
                    ObservationRegistry.Ensure(gauge.Declaration);
                    break;
                case Cost cost:
                    ObservationRegistry.Ensure(cost.Declaration);
                    break;
            }
        }

        private static List<FieldInfo> DeclarationOnlyFields(Type type) {
            FieldInfo[] fields;

            try {
                fields = type.GetFields(DeclaredStaticFields);
            }
            catch (TypeLoadException) {
                return null;
            }

            List<FieldInfo> declaring = null;

            foreach (var field in fields) {
                if (field.IsLiteral) {
                    continue;
                }

                if (!IsDeclaredType(field.FieldType)) {
                    return null;
                }

                declaring ??= new List<FieldInfo>();
                declaring.Add(field);
            }

            return declaring;
        }

        private static bool IsDeclaredType(Type fieldType) {
            return fieldType == typeof(Counter) || fieldType == typeof(Occurrence) || fieldType == typeof(Gauge)
                   || fieldType == typeof(Cost);
        }
    }
}
