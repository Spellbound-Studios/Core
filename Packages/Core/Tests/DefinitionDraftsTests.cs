// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using NUnit.Framework;
using Spellbound.Core.Authoring.Editor;
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
