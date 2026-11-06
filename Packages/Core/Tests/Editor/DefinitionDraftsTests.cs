// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using NUnit.Framework;
using Spellbound.Core.Authoring.Editor;
using Spellbound.Core.Observations;
using Spellbound.Core.Operations;
using Spellbound.Core.Tests.AuthoringFixture;
using UnityEditor;
using UnityEngine;

namespace Spellbound.Core.Tests {
    public class DefinitionDraftsTests {
        private AuthoringTestFolder _folder;
        private OperationSettings _settings;

        [SetUp]
        public void SetUp() {
            _folder = new AuthoringTestFolder();
            _settings = ScriptableObject.CreateInstance<OperationSettings>();
            _settings.draftsFolder = _folder.Path + "/Drafts";
        }

        [TearDown]
        public void TearDown() {
            _folder.Dispose();
            UnityEngine.Object.DestroyImmediate(_settings);
        }

        [Test]
        public void TheFolderComesFromTheSettingsWithADefaultUnderAssets() {
            Assert.AreEqual(_folder.Path + "/Drafts", DefinitionDrafts.Folder(_settings));
            Assert.AreEqual(OperationSettings.DefaultDraftsFolder, DefinitionDrafts.Folder(null));
            _settings.draftsFolder = "Packages/Core/Drafts";
            Assert.Throws<InvalidOperationException>(() => DefinitionDrafts.Folder(_settings));
        }

        [Test]
        public void CreateMakesTheAssetInTheDraftsFolderAndReturnsItsRun() {
            var draft = DefinitionDrafts.Create<FixtureDefinition>("Ore", definition => {
                definition.title = "ore";
                definition.threshold = 3;
            }, _settings);
            Assert.AreEqual(_folder.Path + "/Drafts/Ore.asset", draft.Path);
            Assert.IsNotNull(AssetDatabase.LoadMainAssetAtPath(draft.Path));
            Assert.AreEqual("ore", draft.Definition.title);
            Assert.IsFalse(draft.Run.HasErrors);
            Assert.AreEqual(1, DefinitionDrafts.List(_settings).Count);
            Assert.IsTrue(DefinitionDrafts.IsDraft(draft.Asset, _settings));
        }

        [Test]
        public void CreateRefusesANameThatExistsOrIsNotAFileName() {
            DefinitionDrafts.Create<FixtureDefinition>("Twice", null, _settings);
            Assert.Throws<ArgumentException>(() => DefinitionDrafts.Create<FixtureDefinition>("Twice", null, _settings));
            Assert.Throws<ArgumentException>(() => DefinitionDrafts.Create<FixtureDefinition>("Sub/Folder", null, _settings));
            Assert.Throws<ArgumentException>(() => DefinitionDrafts.Create<FixtureDefinition>(" ", null, _settings));
        }

        [Test]
        public void ApproveIsRefusedWhileTheDraftHasAnError() {
            var draft = DefinitionDrafts.Create<FixtureDefinition>("Empty", null, _settings);
            var approved = DefinitionDrafts.TryApprove(draft.Asset, _folder.Path + "/Content", _settings, out var path, out var refusal);
            Assert.IsFalse(approved);
            Assert.IsNull(path);
            StringAssert.Contains("1 error finding", refusal);
            StringAssert.Contains(FixtureDefinition.TitleEmptyCode, refusal);
            Assert.AreEqual(draft.Path, AssetDatabase.GetAssetPath(draft.Asset));
        }

        [Test]
        public void ApproveIsRefusedForAnAssetOutsideTheDraftsFolder() {
            var asset = _folder.Create("Settled", "settled", 1);
            Assert.IsFalse(DefinitionDrafts.TryApprove(asset, _folder.Path + "/Content", _settings, out _, out var refusal));
            StringAssert.Contains("is not in the drafts folder", refusal);
        }

        [Test]
        public void ApproveIsRefusedForAFolderOutsideAssetsOrInsideTheDrafts() {
            var draft = DefinitionDrafts.Create<FixtureDefinition>("Placed", definition => definition.title = "placed", _settings);
            Assert.IsFalse(DefinitionDrafts.TryApprove(draft.Asset, "Packages/Core", _settings, out _, out var outside));
            StringAssert.Contains("not a folder under Assets", outside);
            Assert.IsFalse(DefinitionDrafts.TryApprove(draft.Asset, "../Elsewhere", _settings, out _, out _));
            Assert.IsFalse(DefinitionDrafts.TryApprove(draft.Asset, _folder.Path + "/Drafts/Inner", _settings, out _, out var inside));
            StringAssert.Contains("outside the drafts folder", inside);
        }

        [Test]
        public void ApproveMovesTheAssetAndKeepsItsGuid() {
            var draft = DefinitionDrafts.Create<FixtureDefinition>("Moved", definition => definition.title = "moved", _settings);
            var guid = AssetDatabase.AssetPathToGUID(draft.Path);
            Assert.IsTrue(DefinitionDrafts.TryApprove(draft.Asset, _folder.Path + "/Content/Ores", _settings, out var path, out var refusal), refusal);
            Assert.AreEqual(_folder.Path + "/Content/Ores/Moved.asset", path);
            Assert.AreEqual(guid, AssetDatabase.AssetPathToGUID(path));
            Assert.IsNull(AssetDatabase.LoadMainAssetAtPath(draft.Path));
            Assert.AreEqual(0, DefinitionDrafts.List(_settings).Count);
            Assert.IsTrue(DefinitionReportCache.TryGet(path, out _));
            Assert.IsFalse(DefinitionReportCache.TryGet(draft.Path, out _));
        }

        [Test]
        public void ApproveIsRefusedWhenTheDestinationExists() {
            var existing = _folder.Create("Clash", "clash", 1, _folder.Subfolder("Content"));
            var draft = DefinitionDrafts.Create<FixtureDefinition>("Clash", definition => definition.title = "draft", _settings);
            Assert.IsFalse(DefinitionDrafts.TryApprove(draft.Asset, _folder.Path + "/Content", _settings, out _, out var refusal));
            StringAssert.Contains("exists already", refusal);
            Assert.AreEqual("clash", existing.title);
        }

        [Test]
        public void ApproveOfARevisionCarriesAManagedReferenceListOverTheOriginal() {
            var original = Layered("Strata", _folder.Subfolder("Content"), "bedrock", "clay", "loam");
            var originalPath = AssetDatabase.GetAssetPath(original);
            var revision =
                (FixtureLayeredDefinition)DefinitionDrafts.Revise(original, _settings, out var refusal).Asset;
            Assert.IsNull(refusal);
            Assert.AreEqual(3, revision.layers.Count);
            revision.layers.RemoveAt(1);
            revision.layers[1].weight = 9;
            EditorUtility.SetDirty(revision);
            AssetDatabase.SaveAssets();

            Assert.IsTrue(DefinitionDrafts.TryApprove(revision, null, _settings, out var approvedPath, out refusal),
                refusal);
            Assert.AreEqual(originalPath, approvedPath);
            var reloaded = AssetDatabase.LoadMainAssetAtPath(originalPath) as FixtureLayeredDefinition;
            Assert.AreEqual(2, reloaded.layers.Count);
            Assert.AreEqual("bedrock", reloaded.layers[0].label);
            Assert.AreEqual(0, reloaded.layers[0].weight);
            Assert.AreEqual("loam", reloaded.layers[1].label);
            Assert.AreEqual(9, reloaded.layers[1].weight);
            StringAssert.DoesNotContain("clay", System.IO.File.ReadAllText(originalPath));
        }

        [Test]
        public void ReviseCopiesAnApprovedDefinitionIntoTheDraftsWithItsReport() {
            var original = _folder.Create("Basalt", "basalt", 2, _folder.Subfolder("Content"));
            var originalPath = AssetDatabase.GetAssetPath(original);
            var draft = DefinitionDrafts.Revise(original, _settings, out var refusal);
            Assert.IsNull(refusal);
            Assert.AreEqual(_folder.Path + "/Drafts/Basalt-revision.asset", draft.Path);
            Assert.AreEqual("basalt", ((FixtureDefinition)draft.Asset).title);
            Assert.AreNotSame(original, draft.Asset);
            Assert.IsTrue(draft.IsRevision);
            Assert.AreEqual(originalPath, draft.OriginalPath);
            Assert.IsFalse(draft.Run.HasErrors);
            Assert.AreEqual(draft.Path, draft.Run.Subject);
            Assert.IsTrue(DefinitionDrafts.IsDraft(draft.Asset, _settings));
            Assert.IsTrue(DefinitionDrafts.IsRevision(draft.Asset, _settings, out var found));
            Assert.AreEqual(originalPath, found);
            Assert.IsFalse(DefinitionDrafts.IsRevision(original, _settings, out _));
            Assert.AreEqual(originalPath, AssetDatabase.GetAssetPath(original));
            var listed = DefinitionDrafts.List(_settings);
            Assert.AreEqual(1, listed.Count);
            Assert.AreEqual(originalPath, listed[0].OriginalPath);
        }

        [Test]
        public void ReviseIsRefusedWhileTheRevisionExistsAndForADraftOrAPlainAsset() {
            var original = _folder.Create("Gneiss", "gneiss", 2);
            var first = DefinitionDrafts.Revise(original, _settings, out _);
            Assert.IsNull(DefinitionDrafts.Revise(original, _settings, out var again));
            StringAssert.Contains(first.Path + " exists already", again);
            Assert.IsNull(DefinitionDrafts.Revise(first.Asset, _settings, out var ofDraft));
            StringAssert.Contains("is in the drafts folder", ofDraft);
            var plain = ScriptableObject.CreateInstance<OperationSettings>();
            AssetDatabase.CreateAsset(plain, _folder.Path + "/Plain.asset");
            Assert.IsNull(DefinitionDrafts.Revise(plain, _settings, out var ofPlain));
            StringAssert.Contains("does not implement IAuthoredDefinition", ofPlain);
            Assert.AreEqual(1, DefinitionDrafts.List(_settings).Count);
        }

        [Test]
        public void ApproveOfARevisionWritesOverTheOriginalAndKeepsItsGuid() {
            var original = _folder.Create("Marble", "marble", 2, _folder.Subfolder("Content"));
            var originalPath = AssetDatabase.GetAssetPath(original);
            var guid = AssetDatabase.AssetPathToGUID(originalPath);
            var draft = DefinitionDrafts.Revise(original, _settings, out _);
            ((FixtureDefinition)draft.Asset).title = "polished marble";
            EditorUtility.SetDirty(draft.Asset);
            AssetDatabase.SaveAssets();

            Assert.IsTrue(DefinitionDrafts.TryApprove(draft.Asset, null, _settings, out var path, out var refusal),
                refusal);
            Assert.AreEqual(originalPath, path);
            Assert.AreEqual(guid, AssetDatabase.AssetPathToGUID(path));
            Assert.AreEqual("polished marble", original.title);
            Assert.AreEqual("Marble", original.name);
            Assert.IsNull(AssetDatabase.LoadMainAssetAtPath(draft.Path));
            Assert.AreEqual(0, DefinitionDrafts.List(_settings).Count);
            Assert.IsFalse(DefinitionReportCache.TryGet(draft.Path, out _));
        }

        [Test]
        public void ApproveOfARevisionIsRefusedWhileItHasAnErrorOrWithAFolder() {
            var original = _folder.Create("Slate", "slate", 2);
            var draft = DefinitionDrafts.Revise(original, _settings, out _);
            ((FixtureDefinition)draft.Asset).title = string.Empty;
            EditorUtility.SetDirty(draft.Asset);
            AssetDatabase.SaveAssets();

            Assert.IsFalse(DefinitionDrafts.TryApprove(draft.Asset, null, _settings, out var path, out var refusal));
            Assert.IsNull(path);
            StringAssert.Contains("1 error finding", refusal);
            StringAssert.Contains(FixtureDefinition.TitleEmptyCode, refusal);
            Assert.AreEqual("slate", original.title);
            Assert.IsFalse(DefinitionDrafts.TryApprove(draft.Asset, _folder.Path + "/Content", _settings, out _,
                out var withFolder));
            StringAssert.Contains("approve it without a folder", withFolder);
            Assert.AreEqual(draft.Path, AssetDatabase.GetAssetPath(draft.Asset));
        }

        [Test]
        public void ApproveOfARevisionWhoseOriginalWasDeletedIsRefused() {
            var original = _folder.Create("Chalk", "chalk", 2);
            var originalPath = AssetDatabase.GetAssetPath(original);
            var draft = DefinitionDrafts.Revise(original, _settings, out _);
            AssetDatabase.DeleteAsset(originalPath);

            Assert.IsTrue(DefinitionDrafts.IsRevision(draft.Asset, _settings, out var found));
            Assert.IsNull(found);
            Assert.IsFalse(DefinitionDrafts.TryApprove(draft.Asset, null, _settings, out _, out var refusal));
            StringAssert.Contains("is a revision of Chalk, which no longer exists outside the drafts folder", refusal);
            Assert.AreEqual(draft.Path, AssetDatabase.GetAssetPath(draft.Asset));
        }

        [Test]
        public void ReviseIsRefusedWhenTwoOriginalsShareTheNameAndTypeAndNamesBoth() {
            var first = _folder.Create("Chert", "chert", 2, _folder.Subfolder("North"));
            var second = _folder.Create("Chert", "chert", 3, _folder.Subfolder("South"));

            Assert.IsNull(DefinitionDrafts.Revise(first, _settings, out var refusal));
            StringAssert.Contains("shares its name with another FixtureDefinition", refusal);
            StringAssert.Contains(AssetDatabase.GetAssetPath(first), refusal);
            StringAssert.Contains(AssetDatabase.GetAssetPath(second), refusal);
            Assert.AreEqual(0, DefinitionDrafts.List(_settings).Count);
        }

        [Test]
        public void ReviseRecognisesAnOriginalInAnImmutablePackage() {
            Assert.IsTrue(DefinitionDrafts.IsInImmutablePackage("Packages/com.unity.test-framework/package.json"));
            Assert.IsFalse(DefinitionDrafts.IsInImmutablePackage("Packages/com.spellboundstudios.core/package.json"));
            Assert.IsFalse(DefinitionDrafts.IsInImmutablePackage(_folder.Path + "/Ore.asset"));
            var packaged = AssetDatabase.LoadMainAssetAtPath(
                    "Packages/com.spellboundstudios.core/Tests/Editor/AuthoringFixture/Assets/PackagedFixture.asset")
                as ScriptableObject;
            Assert.IsNotNull(packaged);
            Assert.IsTrue(DefinitionDrafts.CanRevise(packaged, _settings, out _, out var refusal), refusal);
        }

        [Test]
        public void ApproveOfARevisionIsRefusedWhenASecondOriginalAppearsAfterTheRevise() {
            var first = _folder.Create("Flint", "flint", 2, _folder.Subfolder("North"));
            var draft = DefinitionDrafts.Revise(first, _settings, out _);
            var second = _folder.Create("Flint", "flint", 3, _folder.Subfolder("South"));

            Assert.IsFalse(DefinitionDrafts.TryApprove(draft.Asset, null, _settings, out _, out var refusal));
            StringAssert.Contains("a name 2 definitions share", refusal);
            StringAssert.Contains(AssetDatabase.GetAssetPath(first), refusal);
            StringAssert.Contains(AssetDatabase.GetAssetPath(second), refusal);
        }

        [Test]
        public void ReviseAndApproveRefuseAnAssetWithSubAssets() {
            var original = _folder.Create("Bundle", "bundle", 2, _folder.Subfolder("Content"));
            AddSubAsset(original);
            Assert.IsNull(DefinitionDrafts.Revise(original, _settings, out var reviseRefusal));
            StringAssert.Contains("sub-asset", reviseRefusal);
            Assert.AreEqual(0, DefinitionDrafts.List(_settings).Count);

            var draft = DefinitionDrafts.Create<FixtureDefinition>("Packed", definition => definition.title = "packed",
                _settings);
            AddSubAsset(draft.Asset);
            Assert.IsFalse(DefinitionDrafts.TryApprove(draft.Asset, _folder.Path + "/Content", _settings, out _,
                out var approveRefusal));
            StringAssert.Contains("sub-asset", approveRefusal);
            var host = new OperationHost(new OperationCatalog(() => new[] {
                typeof(OperationHost).Assembly, typeof(AuthoringOperations).Assembly
            }), () => _settings);
            var approver = new OperationCaller("window", OperationAudiences.Developer, OperationAudiences.Approver);
            var previewed = host.Run("core approve asset:" + draft.Path + " --to " + _folder.Path
                                     + "/Content --dry-run", approver, ObservationContext.Discard).Result.Text;
            Assert.AreEqual("previewed: " + draft.Path + " would be refused: " + approveRefusal, previewed);
            Assert.AreEqual(draft.Path, AssetDatabase.GetAssetPath(draft.Asset));
        }

        [Test]
        public void ReviseSavesADirtyOriginalBeforeCopyingIt() {
            var original = _folder.Create("Unsaved", "saved title", 2, _folder.Subfolder("Content"));
            original.title = "unsaved title";
            EditorUtility.SetDirty(original);

            var draft = DefinitionDrafts.Revise(original, _settings, out var refusal);
            Assert.IsNull(refusal);
            Assert.AreEqual("unsaved title", ((FixtureDefinition)draft.Asset).title);
            StringAssert.Contains("unsaved title", System.IO.File.ReadAllText(AssetDatabase.GetAssetPath(original)));
        }

        [Test]
        public void AnOrdinaryDraftNamedLikeARevisionApprovesIntoAFolder() {
            _folder.Create("thing", "thing", 1, _folder.Subfolder("Content"));
            var draft = DefinitionDrafts.Create<FixtureDefinition>("thing-revision",
                definition => definition.title = "thing", _settings);
            Assert.IsFalse(DefinitionDrafts.IsRevision(draft.Asset, _settings, out _));
            Assert.IsTrue(DefinitionDrafts.TryApprove(draft.Asset, _folder.Path + "/Approved", _settings,
                out var path, out var refusal), refusal);
            Assert.AreEqual(_folder.Path + "/Approved/thing-revision.asset", path);
        }

        private static void AddSubAsset(ScriptableObject asset) {
            var inner = ScriptableObject.CreateInstance<OperationSettings>();
            inner.name = "Inner";
            AssetDatabase.AddObjectToAsset(inner, asset);
            AssetDatabase.SaveAssets();
        }

        private static FixtureLayeredDefinition Layered(string name, string folder, params string[] labels) {
            var asset = ScriptableObject.CreateInstance<FixtureLayeredDefinition>();

            foreach (var label in labels) {
                asset.layers.Add(new FixtureLayer { label = label });
            }

            AssetDatabase.CreateAsset(asset, folder + "/" + name + ".asset");
            AssetDatabase.SaveAssets();
            return asset;
        }

        [Test]
        public void ProjectPathsConfineWritesToAssetsAndLibraryAndReadsToPackagesToo() {
            Assert.IsTrue(ProjectPaths.IsWritable("Assets/Drafts"));
            Assert.IsTrue(ProjectPaths.IsWritable("Assets"));
            Assert.IsTrue(ProjectPaths.IsWritable("Library/Spellbound/gym.txt"));
            Assert.IsFalse(ProjectPaths.IsWritable("Library/other.txt"));
            Assert.IsFalse(ProjectPaths.IsWritable("Packages/Core/x.asset"));
            Assert.IsFalse(ProjectPaths.IsWritable("Assets/../ProjectSettings"));
            Assert.IsFalse(ProjectPaths.IsWritable("C:/Assets/x"));
            Assert.IsFalse(ProjectPaths.IsWritable("AssetsX/y"));
            Assert.IsTrue(ProjectPaths.IsReadable("Packages/com.spellboundstudios.core/Tests"));
            Assert.IsFalse(ProjectPaths.IsReadable("ProjectSettings/ProjectVersion.txt"));
            Assert.AreEqual("Assets/Drafts", ProjectPaths.Normalize(" Assets\\Drafts/ "));
        }
    }
}
