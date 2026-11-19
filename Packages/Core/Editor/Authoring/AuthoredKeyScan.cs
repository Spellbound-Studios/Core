// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Spellbound.Core.Operations;
using Spellbound.Core.Tooling;
using UnityEditor;
using UnityEngine;

namespace Spellbound.Core.Authoring.Editor {
    public sealed class AuthoredKeyScan {
        public const string CutMarker = "...";

        private const string NothingStored = "nothing";

        private readonly OperationSettings _settings;
        private readonly SerializationMode _serializationMode;

        public AuthoredKeyScan(OperationSettings settings) : this(settings, EditorSettings.serializationMode) { }

        public AuthoredKeyScan(OperationSettings settings, SerializationMode serializationMode) {
            _settings = settings != null ? settings : throw new ArgumentNullException(nameof(settings));
            _serializationMode = serializationMode;
        }

        public void Scan(ScriptableObject asset, ValidationReport report) {
            if (asset == null) {
                throw new ArgumentNullException(nameof(asset));
            }

            if (report == null) {
                throw new ArgumentNullException(nameof(report));
            }

            var assetPath = AssetDatabase.GetAssetPath(asset);

            if (string.IsNullOrEmpty(assetPath)) {
                return;
            }

            if (_serializationMode != SerializationMode.ForceText) {
                report.Information(DefinitionValidationRunner.ScanUnavailableCode,
                    asset.name + ": the project serializes assets as " + _serializationMode
                    + ", so the scan could not read its stored keys",
                    null, "Set Asset Serialization Mode to Force Text to compare stored keys with the type.");
                return;
            }

            var file = FileUtil.GetPhysicalPath(assetPath);

            if (!File.Exists(file)) {
                return;
            }

            var keys = MainObjectKeys(File.ReadAllLines(file), asset);

            if (keys == null) {
                return;
            }

            var fieldSet = AuthoredFields.FieldSetOf(asset.GetType());
            var objectKeys = new List<StoredKey>();

            foreach (var key in keys) {
                if (!StoredReference.IsRegistryKey(key) || fieldSet.Holds(StoredReference.RegistryKey)) {
                    objectKeys.Add(key);
                }
            }

            var holders = new Dictionary<long, string>();
            Compare(asset.name, objectKeys, string.Empty, fieldSet, holders, report);
            CompareReferences(asset, StoredReference.Read(keys), holders, report);
        }

        private static IReadOnlyList<StoredKey> MainObjectKeys(string[] lines, ScriptableObject asset) {
            return AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out _, out long mainFileIdentifier)
                ? StoredAssetText.ObjectAt(lines, mainFileIdentifier)
                : null;
        }

        private void Compare(string assetName, IReadOnlyList<StoredKey> keys, string displayPrefix,
            StoredFieldSet fieldSet, Dictionary<long, string> holders, ValidationReport report) {
            var reported = new List<string>();

            foreach (var key in keys) {
                if (IsBeneath(key, reported)) {
                    continue;
                }

                var normalized = AuthoredFields.NormalizeStoredPath(key.Path);

                if (key.Name == StoredReference.IdentifierKey
                    && fieldSet.IsManagedReference(AuthoredFields.NormalizeStoredPath(key.ParentPath))) {
                    if (StoredReference.TryIdentifier(key.Value, out var listed)) {
                        holders[listed] = displayPrefix + key.ParentPath;
                    }

                    continue;
                }

                if (fieldSet.Holds(normalized)) {
                    if (fieldSet.IsManagedReference(normalized)
                        && StoredReference.TryIdentifier(key.Value, out var single)) {
                        holders[single] = displayPrefix + key.Path;
                    }

                    continue;
                }

                var path = displayPrefix + key.Path;
                reported.Add(key.Path);

                if (fieldSet.TryCurrentName(normalized, out var currentPath)) {
                    report.Information(DefinitionValidationRunner.RenamedKeyCode,
                        assetName + " " + path + ": stored under the former name of " + displayPrefix + currentPath,
                        path, "Save the asset again to store it under its new name.");
                    continue;
                }

                report.Error(DefinitionValidationRunner.UnknownKeyCode,
                    assetName + " " + path + ": stored " + Cut(key.Value) + ", the type has no field " + key.Name
                    + "; re-author it under its new name before this asset is saved again", path);
            }
        }

        private void CompareReferences(ScriptableObject asset, IReadOnlyList<StoredReference> references,
            Dictionary<long, string> holders, ValidationReport report) {
            var missing = new Dictionary<long, ManagedReferenceMissingType>();

            foreach (var missingType in SerializationUtility.GetManagedReferencesWithMissingTypes(asset)) {
                missing[missingType.referenceId] = missingType;
            }

            var pending = new List<StoredReference>(references);
            var progressed = true;

            while (progressed) {
                progressed = false;

                for (var index = 0; index < pending.Count; index++) {
                    var reference = pending[index];

                    if (!holders.TryGetValue(reference.Identifier, out var holder)) {
                        continue;
                    }

                    pending.RemoveAt(index);
                    index--;
                    progressed = true;

                    var type = missing.ContainsKey(reference.Identifier) ? null : FindType(reference);

                    if (type != null) {
                        Compare(asset.name, reference.Data, holder + ".", AuthoredFields.FieldSetOf(type), holders,
                            report);
                    }
                }
            }

            ReportMissingTypes(asset.name, missing.Values, holders, report);
        }

        private static void ReportMissingTypes(string assetName, IEnumerable<ManagedReferenceMissingType> missing,
            Dictionary<long, string> holders, ValidationReport report) {
            var reported = new HashSet<string>(StringComparer.Ordinal);

            foreach (var missingType in missing) {
                var className = missingType.className.Replace('/', '+');
                var fullName = string.IsNullOrEmpty(missingType.namespaceName)
                    ? className
                    : missingType.namespaceName + "." + className;

                if (!reported.Add(missingType.assemblyName + "|" + fullName)) {
                    continue;
                }

                var location = holders.TryGetValue(missingType.referenceId, out var holder)
                    ? holder
                    : StoredReference.RegistryKey;

                if (!AssemblyExists(missingType.assemblyName)) {
                    report.Information(DefinitionValidationRunner.MissingTypeCode,
                        assetName + " " + location + ": stored type " + fullName + " comes from assembly "
                        + missingType.assemblyName + ", which is not in this project"
                        + PackageClause(PackageLabel.OfAssemblyName(missingType.assemblyName)),
                        location,
                        "Install what declares it, or re-author the reference; its stored keys were not compared.");
                    continue;
                }

                report.Warning(DefinitionValidationRunner.MissingTypeCode,
                    assetName + " " + location + ": stored type " + fullName + " is gone from assembly "
                    + missingType.assemblyName,
                    location, "Re-author the reference under its new type before this asset is saved again.");
            }
        }

        private static string PackageClause(string package) {
            return package == null ? string.Empty : "; it ships in the " + package + " package";
        }

        private string Cut(string value) {
            if (value.Length == 0) {
                return NothingStored;
            }

            var maximum = _settings.maximumStoredValueLength;
            return maximum > 0 && value.Length > maximum ? value.Substring(0, maximum) + CutMarker : value;
        }

        private static bool IsBeneath(StoredKey key, List<string> ancestors) {
            foreach (var ancestor in ancestors) {
                if (key.IsWithin(ancestor)) {
                    return true;
                }
            }

            return false;
        }

        private static Type FindType(StoredReference reference) {
            var type = Type.GetType(reference.FullName + ", " + reference.AssemblyName, false);

            if (type != null) {
                return type;
            }

            var assembly = LoadedAssembly(reference.AssemblyName);
            return assembly?.GetType(reference.FullName, false);
        }

        private static bool AssemblyExists(string assemblyName) {
            try {
                return Assembly.Load(new AssemblyName(assemblyName)) != null;
            }
            catch (Exception exception) when (exception is FileNotFoundException || exception is FileLoadException
                                              || exception is BadImageFormatException
                                              || exception is ArgumentException) {
                return false;
            }
        }

        private static Assembly LoadedAssembly(string assemblyName) {
            foreach (var assembly in AssemblyScanning.ScannableAssemblies()) {
                if (assembly.GetName().Name == assemblyName) {
                    return assembly;
                }
            }

            return null;
        }
    }
}
