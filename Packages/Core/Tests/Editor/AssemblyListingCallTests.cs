// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Spellbound.Core.Tooling;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace Spellbound.Core.Tests {
    public class AssemblyListingCallTests {
        private const string ListingDefine = "SPELLBOUND_CORE_UNITY_LISTS_ASSEMBLIES";
        private const string ScanningFile = "Runtime/Tooling/AssemblyScanning.cs";
        private const string UnityListingCall = "UnityEngine.Assemblies.CurrentAssemblies.GetLoadedAssemblies()";
        private const string FloorListingCall = "AppDomain.CurrentDomain.GetAssemblies()";
        private static readonly string[] ListingCallNames = { "GetAssemblies(", "GetLoadedAssemblies(" };

        [Test]
        public void OnlyTheScanningCallListsLoadedAssembliesInCoreRuntime() {
            var packageRoot = PackageRoot();
            var listing = new List<string>();

            foreach (var file in Directory.GetFiles(Path.Combine(packageRoot, "Runtime"), "*.cs",
                         SearchOption.AllDirectories)) {
                var text = File.ReadAllText(file);

                if (ListingCallNames.Any(call => text.Contains(call, StringComparison.Ordinal))) {
                    listing.Add(Path.GetRelativePath(packageRoot, file).Replace(Path.DirectorySeparatorChar, '/'));
                }
            }

            CollectionAssert.AreEqual(new[] { ScanningFile }, listing);
        }

        [Test]
        public void TheScanningCallReachesUnityBehindTheDefineAndKeepsTheFloorBranch() {
            var text = File.ReadAllText(Path.Combine(PackageRoot(), ScanningFile));
            var opening = text.IndexOf("#if " + ListingDefine, StringComparison.Ordinal);
            var unity = text.IndexOf(UnityListingCall, StringComparison.Ordinal);
            var otherwise = text.IndexOf("#else", StringComparison.Ordinal);
            var floor = text.IndexOf(FloorListingCall, StringComparison.Ordinal);
            var closing = text.IndexOf("#endif", StringComparison.Ordinal);

            Assert.GreaterOrEqual(opening, 0, ScanningFile + " has no #if " + ListingDefine + ".");
            Assert.Greater(unity, opening, ScanningFile + " does not call " + UnityListingCall + " under the define.");
            Assert.Greater(otherwise, unity, ScanningFile + " has no #else after Unity's call.");
            Assert.Greater(floor, otherwise, ScanningFile + " does not keep " + FloorListingCall + " for the floor.");
            Assert.Greater(closing, floor, ScanningFile + " has no #endif after the floor branch.");
        }

        private static string PackageRoot() {
            var package = PackageInfo.FindForAssembly(typeof(AssemblyScanning).Assembly);
            Assert.IsNotNull(package, "Core's package was not found.");
            return package.resolvedPath;
        }
    }
}
