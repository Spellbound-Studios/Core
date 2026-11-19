// Copyright (c) Spellbound Studios. All rights reserved.

using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor.PackageManager;

namespace Spellbound.Core.Operations.Editor {
    public static class PipelineBridgeRanges {
        public const string PipelinePackageName = "com.unity.pipeline";
        public const string BridgeAssemblyName = "Spellbound.Core.Pipeline";
        public const string BridgeTypeName = "Spellbound.Core.Pipeline.SpellboundRunCommand, " + BridgeAssemblyName;

        private static readonly Regex ExpressionPattern = new Regex(
            "\"name\"\\s*:\\s*\"" + Regex.Escape(PipelinePackageName) + "\"\\s*,\\s*\"expression\"\\s*:\\s*\"([^\"]+)\"",
            RegexOptions.Compiled);

        public static string AssemblyDefinitionPath() {
            var core = PackageInfo.FindForAssembly(typeof(CoreKit).Assembly);
            return core == null ? null : Path.Combine(core.resolvedPath, "Runtime", "Pipeline", BridgeAssemblyName + ".asmdef");
        }

        public static IReadOnlyList<string> Read() {
            var path = AssemblyDefinitionPath();
            return path != null && File.Exists(path) ? Read(File.ReadAllText(path)) : new List<string>();
        }

        public static IReadOnlyList<string> Read(string assemblyDefinitionText) {
            var ranges = new List<string>();

            if (string.IsNullOrEmpty(assemblyDefinitionText)) {
                return ranges;
            }

            foreach (Match match in ExpressionPattern.Matches(assemblyDefinitionText)) {
                ranges.Add(match.Groups[1].Value);
            }

            return ranges;
        }

        public static bool Supports(string pipelineVersion, IReadOnlyList<string> ranges) {
            if (string.IsNullOrEmpty(pipelineVersion) || ranges == null) {
                return false;
            }

            foreach (var range in ranges) {
                if (PackageVersionRange.Parse(range).Contains(pipelineVersion)) {
                    return true;
                }
            }

            return false;
        }
    }
}
