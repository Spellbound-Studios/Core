// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Spellbound.Core.Tooling;
using UnityEditor.Compilation;
using UnityEngine;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace Spellbound.Core.Tests {
    public class BridgeVersionDefineTests {
        private const string PipelinePackageName = "com.unity.pipeline";
        private const string PipelineBridgeAssemblyName = "Spellbound.Core.Pipeline";
        private const string PipelineDefinePrefix = "SPELLBOUND_PIPELINE_";
        private const string CoreAssemblyName = "Spellbound.Core";
        private const string ListingDefine = "SPELLBOUND_CORE_UNITY_LISTS_ASSEMBLIES";

        private static readonly Regex ListingVersionDefine = new Regex(
            "\"name\"\\s*:\\s*\"Unity\"\\s*,\\s*\"expression\"\\s*:\\s*\"(?<minimum>[0-9]+(\\.[0-9]+)*)\"\\s*,"
            + "\\s*\"define\"\\s*:\\s*\"" + ListingDefine + "\"");

        private static readonly Regex LeadingNumber = new Regex("^[0-9]+");

        [Test]
        public void ThePipelineBridgeReceivesTheDefineOfTheInstalledPipelineVersion() {
            var pipeline = PackageInfo.GetAllRegisteredPackages()
                .FirstOrDefault(package => package.name == PipelinePackageName);
            Assert.IsNotNull(pipeline, PipelinePackageName + " is not installed in this project.");
            var expected = PipelineDefinePrefix + MajorAndMinor(pipeline.version);
            var bridge = CompiledAssembly(PipelineBridgeAssemblyName);
            var received = bridge.defines
                .Where(define => define.StartsWith(PipelineDefinePrefix, StringComparison.Ordinal))
                .ToArray();

            CollectionAssert.AreEqual(new[] { expected }, received,
                PipelineBridgeAssemblyName + " received " + string.Join(", ", received) + " for "
                + PipelinePackageName + " " + pipeline.version + "; expected " + expected + ".");
            Assert.IsTrue(
                AssemblyScanning.ScannableAssemblies()
                    .Any(assembly => assembly.GetName().Name == PipelineBridgeAssemblyName),
                PipelineBridgeAssemblyName + " is not loaded.");
        }

        [Test]
        public void CoreReceivesTheAssemblyListingDefineFromItsUnityVersionRange() {
            var minimum = ListingDefineMinimumVersion();
            var core = CompiledAssembly(CoreAssemblyName);

            if (IsAtOrPast(Application.unityVersion, minimum)) {
                CollectionAssert.Contains(core.defines, ListingDefine,
                    CoreAssemblyName + " lacks " + ListingDefine + " on " + Application.unityVersion + ", at or past "
                    + minimum + ".");
            }
            else {
                CollectionAssert.DoesNotContain(core.defines, ListingDefine,
                    CoreAssemblyName + " holds " + ListingDefine + " on " + Application.unityVersion + ", before "
                    + minimum + ".");
            }
        }

        private static string ListingDefineMinimumVersion() {
            var path = CompilationPipeline.GetAssemblyDefinitionFilePathFromAssemblyName(CoreAssemblyName);
            Assert.IsNotNull(path, CoreAssemblyName + " has no assembly definition.");
            var match = ListingVersionDefine.Match(File.ReadAllText(path));
            Assert.IsTrue(match.Success,
                path + " has no versionDefines entry named Unity with a minimum version that sets " + ListingDefine
                + ".");
            return match.Groups["minimum"].Value;
        }

        private static bool IsAtOrPast(string version, string minimum) {
            var current = VersionNumbers(version);
            var bound = VersionNumbers(minimum);

            for (var index = 0; index < bound.Length; index++) {
                var part = index < current.Length ? current[index] : 0;

                if (part != bound[index]) {
                    return part > bound[index];
                }
            }

            return true;
        }

        private static int[] VersionNumbers(string version) {
            return version.Split('.')
                .Select(part => LeadingNumber.Match(part))
                .TakeWhile(match => match.Success)
                .Select(match => int.Parse(match.Value))
                .ToArray();
        }

        private static Assembly CompiledAssembly(string name) {
            var assembly = CompilationPipeline.GetAssemblies().FirstOrDefault(candidate => candidate.name == name);
            Assert.IsNotNull(assembly, name + " is not among the compiled assemblies.");
            return assembly;
        }

        private static string MajorAndMinor(string version) {
            var parts = version.Split('.');
            return parts[0] + "_" + parts[1];
        }
    }
}
