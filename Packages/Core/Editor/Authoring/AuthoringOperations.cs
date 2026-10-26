// Copyright (c) Spellbound Studios. All rights reserved.

using System.Text;
using Spellbound.Core.Operations;
using UnityEditor;
using UnityEngine;

namespace Spellbound.Core.Authoring.Editor {
    public static class AuthoringOperations {
        [Operation("validate", "Validates authored definitions: one asset, a folder, a package folder, the drafts folder or everything that implements the contract, and writes the one report with its checks line.",
            OperationEffect.Reads, Audience = OperationAudiences.Anyone)]
        public static OperationResult Validate(OperationContext context,
            [Argument("An asset path, a folder under Assets, or a package folder as Packages/<id> or Packages/<Name>; leave it out to validate everything.")] string path = null,
            [Argument("True to validate the drafts folder named in OperationSettings.")] bool drafts = false,
            [Argument("True to append a table with the milliseconds each validator and check took.")] bool timings = false) {
            var runner = new DefinitionValidationRunner();
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
        public static string Preview([Argument("The definition asset, as asset:<path> or asset:<name>.")] ScriptableObject target) {
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

            foreach (var line in new DefinitionValidationRunner().PreviewLines(target)) {
                builder.Append('\n').Append(line);
            }

            return builder.ToString();
        }

        [Operation("drafts", "Lists the definitions waiting in the drafts folder with the state of their report.", OperationEffect.Reads)]
        public static OperationTable Drafts(OperationContext context) {
            var table = new OperationTable("draft", "type", "report", "path");

            foreach (var draft in DefinitionDrafts.List(context.Host.Settings)) {
                table.AddRow(draft.Asset.name, draft.Asset.GetType().Name, draft.Run.Report.Summary(), draft.Path);
            }

            return table;
        }

        [Operation("approve", "Moves a draft out of the drafts folder into the project, keeping its GUID; refused while the draft has an error finding.",
            OperationEffect.Writes, Audience = OperationAudiences.Approver)]
        public static OperationResult Approve(OperationContext context,
            [Argument("The draft, as asset:<path> or asset:<name>.")] ScriptableObject target,
            [Argument("The folder under Assets the approved asset moves to.")] string to) {
            var settings = context.Host.Settings;

            if (!DefinitionAssets.IsDefinition(target)) {
                return OperationResult.Refused(target.name + " is a " + target.GetType().Name
                                               + ", which does not implement IAuthoredDefinition");
            }

            if (context.DryRun) {
                var run = new DefinitionValidationRunner().ValidateAsset(target);
                var state = run.HasErrors
                    ? "would be refused: " + run.Report.Summary()
                    : "would move to " + ProjectPaths.Normalize(to);
                return OperationResult.Success(AssetDatabase.GetAssetPath(target) + " " + state);
            }

            if (!DefinitionDrafts.TryApprove(target, to, settings, out var approvedPath, out var refusal)) {
                return OperationResult.Refused(refusal);
            }

            return OperationResult.Success("approved " + target.name + " at " + approvedPath);
        }
    }
}
