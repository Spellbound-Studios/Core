// Copyright (c) Spellbound Studios. All rights reserved.

using System.Globalization;
using System.IO;
using System.Text;
using Spellbound.Core.Operations;
using UnityEditor;
using UnityEngine;

namespace Spellbound.Core.Authoring.Editor {
    public static class AuthoringOperations {
        private const string NoOriginal = "not found";
        private const string SavedFormat = "yyyy-MM-dd HH:mm:ss";

        [Operation("validate", "Validates authored definitions: one asset, a folder, a package folder, the drafts folder or everything that implements the contract, and writes the one report with its checks line.",
            OperationEffect.Reads, Audience = OperationAudiences.Anyone)]
        public static OperationResult Validate(OperationContext context,
            [Argument("An asset path, a folder under Assets, or a package folder as Packages/<id> or Packages/<Name>; leave it out to validate everything.")] string path = null,
            [Argument("True to validate the drafts folder named in OperationSettings.")] bool drafts = false,
            [Argument("True to append a table with the milliseconds each validator and check took.")] bool timings = false) {
            var runner = new DefinitionValidationRunner(null, context.Observations, context.Host.Settings);
            ValidationRun run;

            if (drafts) {
                run = runner.ValidateDrafts(context.Host.Settings);
            }
            else if (!string.IsNullOrWhiteSpace(path)) {
                try {
                    run = runner.Validate(path);
                }
                catch (System.ArgumentException exception) {
                    return OperationResult.Invalid(exception.Message);
                }
            }
            else {
                run = runner.ValidateAll();
            }

            var text = run.ToText(timings);
            return run.HasErrors ? OperationResult.Failed(text) : OperationResult.Success(text, run);
        }

        [Operation("preview", "Describes an authored definition: its previewer's summary and pictures, and the lines every registered check contributes.",
            OperationEffect.Reads, Audience = OperationAudiences.Anyone)]
        public static string Preview(OperationContext context,
            [Argument("The definition asset, as asset:<path> or asset:<name>.")] ScriptableObject target) {
            if (!DefinitionAssets.IsDefinition(target)) {
                throw new OperationException(
                    target.name + " is a " + target.GetType().Name + ", which does not implement IAuthoredDefinition.",
                    "Run: core validate --path <folder> to list the definitions there.");
            }

            var preview = DefinitionPreviewerRegistry.Default.Preview(target);
            var builder = new StringBuilder(preview.Summary);
            builder.Append("\npictures: ");
            builder.Append(preview.Pictures.Count == 0 ? "none" : string.Join(", ", preview.Pictures));

            foreach (var line in preview.Lines) {
                builder.Append('\n').Append(line);
            }

            var runner = new DefinitionValidationRunner(null, context.Observations, context.Host.Settings);

            foreach (var line in runner.PreviewLines(target)) {
                builder.Append('\n').Append(line);
            }

            return builder.ToString();
        }

        [Operation("drafts",
            "Lists the definitions waiting in the drafts folder with the state of their report, when each was saved,"
            + " and for a revision its original and when that was saved.", OperationEffect.Reads)]
        public static OperationTable Drafts(OperationContext context) {
            var table = new OperationTable("draft", "type", "report", "path", "saved", "original", "original saved");

            foreach (var draft in DefinitionDrafts.List(context.Host.Settings)) {
                var original = draft.IsRevision ? draft.OriginalPath ?? NoOriginal : string.Empty;
                var originalSaved = draft.OriginalPath != null ? SavedAt(draft.OriginalPath) : string.Empty;
                table.AddRow(draft.Asset.name, draft.Asset.GetType().Name, draft.Run.Report.Summary(), draft.Path,
                    SavedAt(draft.Path), original, originalSaved);
            }

            return table;
        }

        [Operation("revise",
            "Copies an approved definition into the drafts folder as <name>-revision and names the definitions it"
            + " references; approving the revision writes it over the original and keeps its GUID.",
            OperationEffect.Writes)]
        public static OperationResult Revise(OperationContext context,
            [Argument("The approved definition, as asset:<path> or asset:<name>.")] ScriptableObject target) {
            var settings = context.Host.Settings;

            if (!DefinitionDrafts.CanRevise(target, settings, out var revisionPath, out var refusal)) {
                return OperationResult.Refused(refusal);
            }

            if (context.DryRun) {
                return OperationResult.Success(WithReferences(
                    AssetDatabase.GetAssetPath(target) + " would be copied to " + revisionPath, target, settings));
            }

            var draft = DefinitionDrafts.Revise(target, settings, out refusal);

            if (draft == null) {
                return OperationResult.Refused(refusal);
            }

            return OperationResult.Success(WithReferences("draft of " + target.name + " at " + draft.Path,
                draft.Asset, settings));
        }

        [Operation("approve",
            "Moves a draft out of the drafts folder into the project, keeping its GUID, or writes a revision over its"
            + " original; refused while the draft has an error finding.",
            OperationEffect.Writes, Audience = OperationAudiences.Approver)]
        public static OperationResult Approve(OperationContext context,
            [Argument("The draft, as asset:<path> or asset:<name>.")] ScriptableObject target,
            [Argument("The folder under Assets the approved asset moves to; a revision takes none.")]
            string to = null) {
            var settings = context.Host.Settings;

            if (!DefinitionAssets.IsDefinition(target)) {
                return OperationResult.Refused(target.name + " is a " + target.GetType().Name
                                               + ", which does not implement IAuthoredDefinition");
            }

            if (context.DryRun) {
                return OperationResult.Success(AssetDatabase.GetAssetPath(target) + " "
                                               + ApprovalPreview(context, target, to, settings));
            }

            var name = target.name;

            if (!DefinitionDrafts.TryApprove(target, to, settings, out var approvedPath, out var refusal)) {
                return OperationResult.Refused(refusal);
            }

            return OperationResult.Success("approved " + name + " at " + approvedPath);
        }

        private static string ApprovalPreview(OperationContext context, ScriptableObject target, string to,
            OperationSettings settings) {
            var folderRefusal = DefinitionDrafts.SubAssetRefusal(AssetDatabase.GetAssetPath(target))
                                ?? DefinitionDrafts.FolderRefusal(target, to, settings);

            if (folderRefusal != null) {
                return "would be refused: " + folderRefusal;
            }

            var run = new DefinitionValidationRunner(null, context.Observations, settings).ValidateAsset(target);

            if (run.HasErrors) {
                return "would be refused: " + run.Report.Summary();
            }

            if (!DefinitionDrafts.IsRevision(target, settings, out var originalPath)) {
                return "would move to " + ProjectPaths.Normalize(to);
            }

            return originalPath != null
                ? "would be written over " + originalPath
                : "would be refused: no single original of " + target.name + " outside the drafts folder";
        }

        private static string WithReferences(string headline, ScriptableObject asset, OperationSettings settings) {
            var builder = new StringBuilder(headline);

            foreach (var reference in DefinitionReferences.OutsideFolder(asset, DefinitionDrafts.Folder(settings))) {
                builder.Append('\n').Append(reference.Field).Append(" keeps its data in asset:")
                    .Append(reference.Target.name).Append(" (").Append(reference.Package)
                    .Append("); revise it with core revise asset:").Append(reference.Target.name);
            }

            return builder.ToString();
        }

        private static string SavedAt(string path) {
            return File.GetLastWriteTime(FileUtil.GetPhysicalPath(path))
                .ToString(SavedFormat, CultureInfo.InvariantCulture);
        }
    }
}
