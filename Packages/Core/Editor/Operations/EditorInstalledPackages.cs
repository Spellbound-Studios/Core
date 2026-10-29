// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Spellbound.Core.Operations;
using Spellbound.Core.Tooling;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
using Assembly = System.Reflection.Assembly;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace Spellbound.Core.Operations.Editor {
    public static class EditorInstalledPackages {
        public const string IdentifierPrefix = "com.spellbound";
        public const string ManifestFileName = "package.json";

        [InitializeOnLoadMethod]
        private static void Register() {
            InstalledPackages.Source = Installed;
        }

        public static IReadOnlyList<InstalledPackageDescription> Installed() {
            var packages = new List<InstalledPackageDescription>();

            foreach (var package in PackageInfo.GetAllRegisteredPackages()) {
                if (package.name.StartsWith(IdentifierPrefix, StringComparison.Ordinal)) {
                    packages.Add(Describe(package));
                }
            }

            packages.Sort((left, right) => string.CompareOrdinal(left.Name, right.Name));
            return packages;
        }

        public static InstalledPackageDescription ForAssembly(Assembly assembly) {
            if (assembly == null) {
                return null;
            }

            var package = PackageInfo.FindForAssembly(assembly);
            return package == null ? FromFolder(assembly.GetName().Name) : Describe(package);
        }

        public static InstalledPackageDescription ForAssemblyName(string assemblyName) {
            foreach (var assembly in AssemblyScanning.ScannableAssemblies()) {
                if (string.Equals(assembly.GetName().Name, assemblyName, StringComparison.Ordinal)) {
                    return ForAssembly(assembly);
                }
            }

            return FromFolder(assemblyName);
        }

        public static bool IsRegistered(InstalledPackageDescription package) {
            if (package == null) {
                return false;
            }

            foreach (var registered in PackageInfo.GetAllRegisteredPackages()) {
                if (string.Equals(registered.name, package.Name, StringComparison.Ordinal)) {
                    return true;
                }
            }

            return false;
        }

        public static string VersionOf(string packageName) {
            foreach (var package in PackageInfo.GetAllRegisteredPackages()) {
                if (string.Equals(package.name, packageName, StringComparison.Ordinal)) {
                    return package.version;
                }
            }

            return null;
        }

        public static InstalledPackageDescription FromFolder(string assemblyName) {
            if (string.IsNullOrEmpty(assemblyName)) {
                return null;
            }

            var definitionPath = CompilationPipeline.GetAssemblyDefinitionFilePathFromAssemblyName(assemblyName);

            if (string.IsNullOrEmpty(definitionPath)) {
                return null;
            }

            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var directory = Path.GetDirectoryName(Path.GetFullPath(definitionPath));

            while (!string.IsNullOrEmpty(directory) && directory.StartsWith(projectRoot, StringComparison.OrdinalIgnoreCase)) {
                var manifest = Path.Combine(directory, ManifestFileName);

                if (File.Exists(manifest)) {
                    return DescribeManifest(File.ReadAllText(manifest), directory);
                }

                directory = Path.GetDirectoryName(directory);
            }

            return null;
        }

        public static InstalledPackageDescription DescribeManifest(string manifestText, string folder) {
            var name = ManifestField(manifestText, "name");
            return name == null ? null : new InstalledPackageDescription(name, ManifestField(manifestText, "displayName"), ManifestField(manifestText, "version"), folder);
        }

        private static string ManifestField(string manifestText, string field) {
            var match = Regex.Match(manifestText ?? string.Empty, "\"" + Regex.Escape(field) + "\"\\s*:\\s*\"([^\"]*)\"");
            return match.Success ? match.Groups[1].Value : null;
        }

        private static InstalledPackageDescription Describe(PackageInfo package) {
            return new InstalledPackageDescription(package.name, package.displayName, package.version, package.resolvedPath);
        }
    }
}
