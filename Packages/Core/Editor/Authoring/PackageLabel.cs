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

        public static string OwnValidation(Type definitionType) {
            return definitionType.Name + " (" + Of(definitionType) + ")";
        }
    }
}
