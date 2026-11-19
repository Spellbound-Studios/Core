// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using Spellbound.Core.Operations;
using UnityEditor;
using UnityEngine;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;
using PackageSource = UnityEditor.PackageManager.PackageSource;

namespace Spellbound.Core.Authoring.Editor {
    public static class DefinitionDrafts {
        public const string AssetExtension = ".asset";
        public const string RevisionSuffix = "-revision";
        public const string RevisionLabel = "spellbound-revision";

        private const string ScriptProperty = "m_Script";

        public static string Folder(OperationSettings settings) {
            var folder = settings != null ? settings.draftsFolder : null;
            var chosen = string.IsNullOrWhiteSpace(folder) ? OperationSettings.DefaultDraftsFolder : folder;
            var normalized = ProjectPaths.Normalize(chosen);

            if (!ProjectPaths.IsInside(normalized, ProjectPaths.AssetsRoot)) {
                throw new InvalidOperationException(
                    "OperationSettings.draftsFolder is '" + folder + "'; the drafts folder is under Assets.");
            }

            return normalized;
        }

        public static string PathFor(string name, OperationSettings settings) {
            if (string.IsNullOrWhiteSpace(name) || name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0
                || name.Contains("/")) {
                throw new ArgumentException("'" + name + "' is not a file name a draft can take.", nameof(name));
            }

            return Folder(settings) + "/" + name.Trim() + AssetExtension;
        }

        public static Draft<T> Create<T>(string name, Action<T> fill, OperationSettings settings,
            DefinitionValidationRunner runner = null) where T : ScriptableObject, IAuthoredDefinition {
            var path = PathFor(name, settings);

            if (AssetDatabase.LoadMainAssetAtPath(path) != null) {
                throw new ArgumentException(
                    "A draft already exists at " + path + "; pick another name or approve or delete it first.", nameof(name));
            }

            ProjectPaths.EnsureFolder(Folder(settings));
            var asset = ScriptableObject.CreateInstance<T>();
            fill?.Invoke(asset);
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            var run = (runner ?? RunnerFor(settings)).ValidateAsset(asset);
            return new Draft<T>(asset, path, run);
        }

        public static bool CanRevise(ScriptableObject asset, OperationSettings settings, out string revisionPath,
            out string refusal) {
            revisionPath = null;
            refusal = DefinitionRefusal(asset);

            if (refusal != null) {
                return false;
            }

            var path = AssetDatabase.GetAssetPath(asset);

            if (string.IsNullOrEmpty(path)) {
                refusal = asset.name + " is not saved as an asset";
                return false;
            }

            if (IsDraft(asset, settings)) {
                refusal = path + " is in the drafts folder " + Folder(settings) + "; revise an approved definition";
                return false;
            }

            if (IsInImmutablePackage(path)) {
                refusal = path + " is in an immutable package; an approved revision could never be written over it";
                return false;
            }

            refusal = SubAssetRefusal(path);

            if (refusal != null) {
                return false;
            }

            var namesakes = Originals(asset.name, asset.GetType(), Folder(settings), DefinitionAssets.FindAll());

            if (namesakes.Count > 1) {
                refusal = path + " shares its name with another " + asset.GetType().Name + " ("
                          + string.Join(", ", namesakes) + "); rename one before revising";
                return false;
            }

            revisionPath = PathFor(asset.name + RevisionSuffix, settings);

            if (AssetDatabase.LoadMainAssetAtPath(revisionPath) != null) {
                refusal = revisionPath + " exists already; approve or delete it first";
                return false;
            }

            return true;
        }

        public static Draft Revise(ScriptableObject asset, OperationSettings settings, out string refusal,
            DefinitionValidationRunner runner = null) {
            if (!CanRevise(asset, settings, out var revisionPath, out refusal)) {
                return null;
            }

            var path = AssetDatabase.GetAssetPath(asset);
            ProjectPaths.EnsureFolder(Folder(settings));
            AssetDatabase.SaveAssetIfDirty(asset);

            if (!AssetDatabase.CopyAsset(path, revisionPath)) {
                refusal = path + " could not be copied to " + revisionPath;
                return null;
            }

            var revision = AssetDatabase.LoadMainAssetAtPath(revisionPath) as ScriptableObject;
            var labels = new List<string>(AssetDatabase.GetLabels(revision));

            if (!labels.Contains(RevisionLabel)) {
                labels.Add(RevisionLabel);
                AssetDatabase.SetLabels(revision, labels.ToArray());
            }

            var run = (runner ?? RunnerFor(settings)).ValidateAsset(revision);
            return new Draft(revision, revisionPath, run, true, path);
        }

        public static IReadOnlyList<Draft> List(OperationSettings settings, DefinitionValidationRunner runner = null) {
            var drafts = new List<Draft>();
            var folder = Folder(settings);

            if (!AssetDatabase.IsValidFolder(folder)) {
                return drafts;
            }

            var validator = runner ?? RunnerFor(settings);
            IReadOnlyList<string> definitions = null;

            foreach (var path in DefinitionAssets.FindInFolder(folder)) {
                var asset = AssetDatabase.LoadMainAssetAtPath(path) as ScriptableObject;

                if (!DefinitionAssets.IsDefinition(asset)) {
                    continue;
                }

                var run = validator.ValidateAsset(asset);

                if (!IsMarkedRevision(asset)) {
                    drafts.Add(new Draft(asset, path, run));
                    continue;
                }

                definitions = definitions ?? DefinitionAssets.FindAll();
                var originals = Originals(OriginalName(asset), asset.GetType(), folder, definitions);
                drafts.Add(new Draft(asset, path, run, true, originals.Count == 1 ? originals[0] : null));
            }

            return drafts;
        }

        public static bool IsDraft(ScriptableObject asset, OperationSettings settings) {
            var path = asset != null ? AssetDatabase.GetAssetPath(asset) : null;
            return !string.IsNullOrEmpty(path) && ProjectPaths.IsInside(ProjectPaths.Normalize(path), Folder(settings));
        }

        public static bool IsRevision(ScriptableObject asset, OperationSettings settings, out string originalPath) {
            originalPath = null;

            if (!DefinitionAssets.IsDefinition(asset) || !IsMarkedRevision(asset) || !IsDraft(asset, settings)) {
                return false;
            }

            var originals = Originals(OriginalName(asset), asset.GetType(), Folder(settings),
                DefinitionAssets.FindAll());
            originalPath = originals.Count == 1 ? originals[0] : null;
            return true;
        }

        public static bool IsInImmutablePackage(string assetPath) {
            var normalized = ProjectPaths.Normalize(assetPath);

            if (!ProjectPaths.IsInside(normalized, ProjectPaths.PackagesRoot)) {
                return false;
            }

            var package = PackageInfo.FindForAssetPath(normalized);
            return package == null
                   || (package.source != PackageSource.Embedded && package.source != PackageSource.Local);
        }

        public static bool TryApprove(ScriptableObject asset, string folder, OperationSettings settings,
            out string approvedPath, out string refusal, DefinitionValidationRunner runner = null) {
            approvedPath = null;
            refusal = DefinitionRefusal(asset);

            if (refusal != null) {
                return false;
            }

            var path = AssetDatabase.GetAssetPath(asset);

            if (!IsDraft(asset, settings)) {
                refusal = (string.IsNullOrEmpty(path) ? asset.name : path) + " is not in the drafts folder "
                          + Folder(settings);
                return false;
            }

            refusal = SubAssetRefusal(path) ?? FolderRefusal(asset, folder, settings);

            if (refusal != null) {
                return false;
            }

            if (IsMarkedRevision(asset)) {
                return TryApproveRevision(asset, path, settings, out approvedPath, out refusal, runner);
            }

            var target = ProjectPaths.Normalize(folder);
            var run = (runner ?? RunnerFor(settings)).ValidateAsset(asset);

            if (run.HasErrors) {
                refusal = ErrorRefusal(path, run);
                return false;
            }

            ProjectPaths.EnsureFolder(target);
            var destination = target + "/" + Path.GetFileName(path);

            if (AssetDatabase.LoadMainAssetAtPath(destination) != null) {
                refusal = destination + " exists already";
                return false;
            }

            var error = AssetDatabase.MoveAsset(path, destination);

            if (!string.IsNullOrEmpty(error)) {
                refusal = error;
                return false;
            }

            AssetDatabase.SaveAssets();
            DefinitionReportCache.Forget(path);
            DefinitionReportCache.Store(destination, run);
            approvedPath = destination;
            return true;
        }

        internal static string FolderRefusal(ScriptableObject asset, string folder, OperationSettings settings) {
            var path = AssetDatabase.GetAssetPath(asset);

            if (IsMarkedRevision(asset)) {
                return string.IsNullOrWhiteSpace(folder)
                    ? null
                    : path + " is a revision of " + OriginalName(asset)
                      + "; approve it without a folder and it is written over its original";
            }

            if (string.IsNullOrWhiteSpace(folder)) {
                return path + " is a draft and needs a folder under Assets to move to";
            }

            var target = ProjectPaths.Normalize(folder);

            if (!ProjectPaths.IsInside(target, ProjectPaths.AssetsRoot)
                || ProjectPaths.IsInside(target, Folder(settings))) {
                return "'" + folder + "' is not a folder under Assets outside the drafts folder";
            }

            return null;
        }

        private static bool TryApproveRevision(ScriptableObject revision, string path, OperationSettings settings,
            out string approvedPath, out string refusal, DefinitionValidationRunner runner) {
            approvedPath = null;
            refusal = null;
            var originalName = OriginalName(revision);
            var originals = Originals(originalName, revision.GetType(), Folder(settings), DefinitionAssets.FindAll());

            if (originals.Count == 0) {
                refusal = path + " is a revision of " + originalName
                          + ", which no longer exists outside the drafts folder";
                return false;
            }

            if (originals.Count > 1) {
                refusal = path + " is a revision of " + originalName + ", a name " + originals.Count
                          + " definitions share: " + string.Join(", ", originals);
                return false;
            }

            var run = (runner ?? RunnerFor(settings)).ValidateAsset(revision);

            if (run.HasErrors) {
                refusal = ErrorRefusal(path, run);
                return false;
            }

            var originalPath = originals[0];

            if (IsInImmutablePackage(originalPath)) {
                refusal = originalPath + " is in an immutable package; " + path + " cannot be written over it";
                return false;
            }

            if (!AssetDatabase.IsOpenForEdit(originalPath)) {
                refusal = originalPath + " is not open for edit; check it out, then approve " + path + " again";
                return false;
            }

            var original = AssetDatabase.LoadMainAssetAtPath(originalPath) as ScriptableObject;
            EditorUtility.CopySerialized(revision, original);
            original.name = originalName;
            EditorUtility.SetDirty(original);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(originalPath, ImportAssetOptions.ForceUpdate);
            var differing = DifferingProperty(revision, AssetDatabase.LoadMainAssetAtPath(originalPath));

            if (differing != null) {
                refusal = originalPath + " did not keep the revision: " + differing + " differs after saving; "
                          + path + " is kept";
                return false;
            }

            AssetDatabase.DeleteAsset(path);
            DefinitionReportCache.Forget(path);
            DefinitionReportCache.Forget(originalPath);
            approvedPath = originalPath;
            return true;
        }

        private static string DifferingProperty(ScriptableObject revision, UnityEngine.Object original) {
            if (original == null) {
                return "the asset";
            }

            using (var revisionObject = new SerializedObject(revision))
            using (var originalObject = new SerializedObject(original)) {
                var property = revisionObject.GetIterator();
                var enterChildren = true;

                while (property.NextVisible(enterChildren)) {
                    enterChildren = false;

                    if (property.propertyPath == ScriptProperty) {
                        continue;
                    }

                    var stored = originalObject.FindProperty(property.propertyPath);
                    return stored != null && SerializedProperty.DataEquals(property, stored)
                        ? null
                        : property.propertyPath;
                }
            }

            return null;
        }

        internal static string SubAssetRefusal(string path) {
            var subAssets = AssetDatabase.LoadAllAssetRepresentationsAtPath(path);

            if (subAssets.Length == 0) {
                return null;
            }

            var names = new List<string>();

            foreach (var subAsset in subAssets) {
                names.Add(subAsset != null ? subAsset.name : "a missing object");
            }

            return path + " holds sub-assets (" + string.Join(", ", names)
                   + "); a definition is revised and approved as one object";
        }

        private static DefinitionValidationRunner RunnerFor(OperationSettings settings) {
            return new DefinitionValidationRunner(null, null, settings);
        }

        private static string DefinitionRefusal(ScriptableObject asset) {
            if (asset == null) {
                return "no asset was given";
            }

            return DefinitionAssets.IsDefinition(asset)
                ? null
                : asset.name + " is a " + asset.GetType().Name + ", which does not implement IAuthoredDefinition";
        }

        private static string ErrorRefusal(string path, ValidationRun run) {
            return path + " has " + run.Report.Count(FindingSeverity.Error) + " error finding(s); fix them first\n"
                   + run.Report.ToText();
        }

        private static bool IsMarkedRevision(ScriptableObject asset) {
            return asset.name.Length > RevisionSuffix.Length
                   && asset.name.EndsWith(RevisionSuffix, StringComparison.Ordinal)
                   && Array.IndexOf(AssetDatabase.GetLabels(asset), RevisionLabel) >= 0;
        }

        private static string OriginalName(ScriptableObject revision) {
            return revision.name.Substring(0, revision.name.Length - RevisionSuffix.Length);
        }

        private static List<string> Originals(string originalName, Type type, string draftsFolder,
            IReadOnlyList<string> definitions) {
            var originals = new List<string>();

            foreach (var path in definitions) {
                if (!ProjectPaths.IsInside(path, draftsFolder)
                    && string.Equals(Path.GetFileNameWithoutExtension(path), originalName, StringComparison.Ordinal)
                    && AssetDatabase.GetMainAssetTypeAtPath(path) == type) {
                    originals.Add(path);
                }
            }

            return originals;
        }
    }
}
