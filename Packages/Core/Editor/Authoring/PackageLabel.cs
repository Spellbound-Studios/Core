// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using Spellbound.Core.Operations.Editor;

namespace Spellbound.Core.Authoring.Editor {
    public static class PackageLabel {
        public const string StudioPrefix = "Spellbound.";

        public static string Of(Type type) {
            if (type == null) {
                return string.Empty;
            }

            var package = EditorInstalledPackages.ForAssembly(type.Assembly);

            if (package == null) {
                return type.Assembly.GetName().Name;
            }

            var label = string.IsNullOrEmpty(package.DisplayName) ? package.Name : package.DisplayName;

            if (label.StartsWith(StudioPrefix, StringComparison.Ordinal) && label.Length > StudioPrefix.Length) {
                label = label.Substring(StudioPrefix.Length);
            }

            return label;
        }

        internal static string OfAssemblyName(string assemblyName) {
            if (string.IsNullOrEmpty(assemblyName)
                || !assemblyName.StartsWith(StudioPrefix, StringComparison.Ordinal)
                || assemblyName.Length == StudioPrefix.Length) {
                return null;
            }

            var remainder = assemblyName.Substring(StudioPrefix.Length);
            var dot = remainder.IndexOf('.');
            return dot < 0 ? remainder : remainder.Substring(0, dot);
        }

        public static string OwnValidation(Type definitionType) {
            return definitionType.Name + " (" + Of(definitionType) + ")";
        }
    }
}
