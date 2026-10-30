// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Spellbound.Core.Tooling;
using UnityEditor;
using UnityEditor.Compilation;
using Assembly = System.Reflection.Assembly;

namespace Spellbound.Core.Operations.Editor {
    public static class EditorAssemblies {
        public const string TestsConstraint = "UNITY_INCLUDE_TESTS";
        public const string LegacyTestsReference = "TestAssemblies";

        private static readonly Regex ConstraintsPattern = new Regex(
            "\"defineConstraints\"\\s*:\\s*\\[(?<terms>[^\\]]*)\\]", RegexOptions.Compiled);

        private static readonly Regex LegacyReferencesPattern = new Regex(
            "\"optionalUnityReferences\"\\s*:\\s*\\[(?<terms>[^\\]]*)\\]", RegexOptions.Compiled);

        [InitializeOnLoadMethod]
        private static void Register() {
            BridgeDoors.AssemblySource = WithoutTests;
        }

        public static IEnumerable<Assembly> WithoutTests() {
            foreach (var assembly in AssemblyScanning.ScannableAssemblies()) {
                if (!IsTestAssembly(assembly.GetName().Name)) {
                    yield return assembly;
                }
            }
        }

        public static bool IsTestAssembly(string assemblyName) {
            if (string.IsNullOrEmpty(assemblyName)) {
                return false;
            }

            var path = CompilationPipeline.GetAssemblyDefinitionFilePathFromAssemblyName(assemblyName);
            return !string.IsNullOrEmpty(path) && File.Exists(path) && IsConstrainedOnTests(File.ReadAllText(path));
        }

        public static bool IsConstrainedOnTests(string assemblyDefinitionText) {
            if (string.IsNullOrEmpty(assemblyDefinitionText)) {
                return false;
            }

            return Names(ConstraintsPattern, assemblyDefinitionText, TestsConstraint)
                   || Names(LegacyReferencesPattern, assemblyDefinitionText, LegacyTestsReference);
        }

        private static bool Names(Regex pattern, string text, string wanted) {
            var match = pattern.Match(text);

            if (!match.Success) {
                return false;
            }

            foreach (var entry in match.Groups["terms"].Value.Split(',')) {
                foreach (var term in entry.Trim().Trim('"').Split(new[] { "||", "&&" }, StringSplitOptions.None)) {
                    if (string.Equals(term.Trim(), wanted, StringComparison.Ordinal)) {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
