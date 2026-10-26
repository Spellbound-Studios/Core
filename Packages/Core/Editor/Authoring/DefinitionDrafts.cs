// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using Spellbound.Core.Operations;
using UnityEditor;
using UnityEngine;

namespace Spellbound.Core.Authoring.Editor {
    public static class DefinitionDrafts {
        public const string AssetExtension = ".asset";

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
            var run = (runner ?? new DefinitionValidationRunner()).ValidateAsset(asset);
            return new Draft<T>(asset, path, run);
        }

        public static IReadOnlyList<Draft> List(OperationSettings settings, DefinitionValidationRunner runner = null) {
            var drafts = new List<Draft>();
            var folder = Folder(settings);

            if (!AssetDatabase.IsValidFolder(folder)) {
                return drafts;
            }

            var validator = runner ?? new DefinitionValidationRunner();

            foreach (var path in DefinitionAssets.FindInFolder(folder)) {
                var asset = AssetDatabase.LoadMainAssetAtPath(path) as ScriptableObject;

                if (DefinitionAssets.IsDefinition(asset)) {
                    drafts.Add(new Draft(asset, path, validator.ValidateAsset(asset)));
                }
            }

            return drafts;
        }

        public static bool IsDraft(ScriptableObject asset, OperationSettings settings) {
            var path = asset != null ? AssetDatabase.GetAssetPath(asset) : null;
            return !string.IsNullOrEmpty(path) && ProjectPaths.IsInside(ProjectPaths.Normalize(path), Folder(settings));
        }

        public static bool TryApprove(ScriptableObject asset, string folder, OperationSettings settings,
            out string approvedPath, out string refusal, DefinitionValidationRunner runner = null) {
            approvedPath = null;
            refusal = null;

            if (asset == null) {
                refusal = "no asset was given";
                return false;
            }

            var path = AssetDatabase.GetAssetPath(asset);

            if (!DefinitionAssets.IsDefinition(asset)) {
                refusal = asset.name + " is a " + asset.GetType().Name
                          + ", which does not implement IAuthoredDefinition";
                return false;
            }

            if (!IsDraft(asset, settings)) {
                refusal = (string.IsNullOrEmpty(path) ? asset.name : path) + " is not in the drafts folder "
                          + Folder(settings);
                return false;
            }

            var target = ProjectPaths.Normalize(folder);

            if (!ProjectPaths.IsInside(target, ProjectPaths.AssetsRoot)
                || ProjectPaths.IsInside(target, Folder(settings))) {
                refusal = "'" + folder + "' is not a folder under Assets outside the drafts folder";
                return false;
            }

            var run = (runner ?? new DefinitionValidationRunner()).ValidateAsset(asset);

            if (run.HasErrors) {
                refusal = path + " has " + run.Report.Count(FindingSeverity.Error) + " error finding(s); fix them first\n"
                          + run.Report.ToText();
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
    }
}
