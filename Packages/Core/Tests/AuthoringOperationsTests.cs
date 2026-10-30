// Copyright (c) Spellbound Studios. All rights reserved.

using System.Reflection;
using NUnit.Framework;
using Spellbound.Core.Authoring.Editor;
using Spellbound.Core.Observations;
using Spellbound.Core.Operations;
using Spellbound.Core.Tests.AuthoringFixture;
using UnityEditor;
using UnityEngine;

namespace Spellbound.Core.Tests {
    public class AuthoringOperationsTests {
        private static readonly Assembly Core = typeof(OperationHost).Assembly;
        private static readonly Assembly CoreEditor = typeof(AuthoringOperations).Assembly;

        private AuthoringTestFolder _folder;
        private OperationSettings _settings;
        private OperationHost _host;
        private OperationCaller _developer;
        private OperationCaller _approver;

        [SetUp]
        public void SetUp() {
            _folder = new AuthoringTestFolder();
            _settings = ScriptableObject.CreateInstance<OperationSettings>();
            _settings.draftsFolder = _folder.Path + "/Drafts";
            _host = new OperationHost(new OperationCatalog(() => new[] { Core, CoreEditor }), () => _settings);
            _developer = OperationCaller.Development();
            _approver = new OperationCaller("window", OperationAudiences.Developer, OperationAudiences.Approver);
        }

        [TearDown]
        public void TearDown() {
            _folder.Dispose();
            UnityEngine.Object.DestroyImmediate(_settings);
        }

        private string Text(string line, OperationCaller caller = null) => _host.Run(line, caller ?? _developer, ObservationContext.Discard).Result.Text;

        [Test]
        public void TheFiveOperationsJoinTheCoreGroup() {
            foreach (var name in new[] { "validate", "preview", "drafts", "approve" }) {
                Assert.IsNotNull(_host.Catalog.Describe("core", name), name);
            }

            var help = Text("core help --group core");
            StringAssert.Contains("core validate", help);
            StringAssert.Contains("core approve", help);
            Assert.AreEqual(OperationAudiences.Approver, _host.Catalog.Describe("core", "approve").Audience);
            Assert.AreEqual(OperationEffect.Writes, _host.Catalog.Describe("core", "approve").Effect);
        }

        [Test]
        public void ValidateOneCleanAssetSucceedsWithTheChecksLineSecond() {
            var asset = _folder.Create("Clean", "clean", 1);
            var lines = Text("core validate --path " + AssetDatabase.GetAssetPath(asset)).Split('\n');
            Assert.AreEqual("succeeded: no findings in 1 asset", lines[0]);
            Assert.AreEqual("checks: FixtureDefinition (Core), fixturecheck.threshold-high (Core)", lines[1]);
            Assert.AreEqual(2, lines.Length);
        }

        [Test]
        public void ValidateAnAssetWithAnErrorFails() {
            var asset = _folder.Create("Bad", string.Empty, 1);
            var text = Text("core validate --path " + AssetDatabase.GetAssetPath(asset));
            StringAssert.StartsWith("failed: 1 error in 1 asset\nchecks: ", text);
            StringAssert.Contains("\n" + FixtureDefinition.TitleEmptyCode + " error ", text);
        }

        [Test]
        public void ValidateAFolderAndEverythingAndTheDrafts() {
            _folder.Create("A", "a", -1);
            var folder = Text("core validate --path " + _folder.Path);
            StringAssert.StartsWith("succeeded: 1 warning in 1 asset", folder);
            StringAssert.StartsWith("succeeded: ", Text("core validate --drafts"));
            StringAssert.Contains("no authored definitions under " + _folder.Path + "/Drafts", Text("core validate --drafts"));
            DefinitionDrafts.Create<FixtureDefinition>("D", definition => definition.title = "d", _settings);
            StringAssert.StartsWith("succeeded: no findings in 1 asset", Text("core validate --drafts"));
            var everything = Text("core validate");
            StringAssert.StartsWith("succeeded: ", everything);
            StringAssert.Contains("checks: ", everything);
        }

        [Test]
        public void ValidateWithTimingsAppendsATable() {
            var asset = _folder.Create("Timed", "timed", 1);
            var text = Text("core validate --path " + AssetDatabase.GetAssetPath(asset) + " --timings");
            StringAssert.Contains("\ncheck", text);
            StringAssert.Contains("fixturecheck.threshold-high (Core)", text.Substring(text.IndexOf("\ncheck", System.StringComparison.Ordinal)));
        }

        [Test]
        public void ValidateABadPathIsInvalid() {
            StringAssert.StartsWith("invalid: 'Assets/Nowhere' is neither an asset nor a folder", Text("core validate --path Assets/Nowhere"));
            StringAssert.StartsWith("invalid: '../x' is outside Assets and Packages", Text("core validate --path ../x"));
        }

        [Test]
        public void ValidateIsOpenToAnyone() {
            Assert.IsTrue(_host.Run("core validate --path " + _folder.Path, new OperationCaller("player"), ObservationContext.Discard).Result.Succeeded);
        }

        [Test]
        public void PreviewPrintsThePreviewerAndTheChecksLines() {
            var asset = _folder.Create("Seen", "seen", 7);
            var text = Text("core preview asset:" + AssetDatabase.GetAssetPath(asset));
            Assert.AreEqual("succeeded: 'seen' at threshold 7\npictures: none\na fixture previewer line\nthreshold 7 of 100", text);
        }

        [Test]
        public void PreviewOfSomethingThatIsNotADefinitionFails() {
            var settings = ScriptableObject.CreateInstance<OperationSettings>();
            var path = _folder.Path + "/Plain.asset";
            AssetDatabase.CreateAsset(settings, path);
            StringAssert.StartsWith("failed: Plain is a OperationSettings, which does not implement IAuthoredDefinition.", Text("core preview asset:" + path));
        }

        [Test]
        public void DraftsListsTheDraftsFolderAsATable() {
            Assert.AreEqual("succeeded: draft  type  report  path", Text("core drafts"));
            DefinitionDrafts.Create<FixtureDefinition>("Ore", definition => definition.title = "ore", _settings);
            DefinitionDrafts.Create<FixtureDefinition>("Blank", null, _settings);
            var text = Text("core drafts");
            StringAssert.Contains("Ore    FixtureDefinition  no findings", text);
            StringAssert.Contains("Blank  FixtureDefinition  1 error", text);
            StringAssert.Contains(_folder.Path + "/Drafts/Ore.asset", text);
        }

        [Test]
        public void ApproveNeedsTheApproverAudience() {
            var draft = DefinitionDrafts.Create<FixtureDefinition>("Gated", definition => definition.title = "gated", _settings);
            var refused = Text("core approve asset:" + draft.Path + " --to " + _folder.Path + "/Content");
            StringAssert.StartsWith("refused: core approve needs audience 'approver'", refused);
            Assert.AreEqual(draft.Path, AssetDatabase.GetAssetPath(draft.Asset));
        }

        [Test]
        public void ApproveMovesTheDraftForAnApprover() {
            var draft = DefinitionDrafts.Create<FixtureDefinition>("Blessed", definition => definition.title = "blessed", _settings);
            var text = Text("core approve asset:" + draft.Path + " --to " + _folder.Path + "/Content", _approver);
            Assert.AreEqual("succeeded: approved Blessed at " + _folder.Path + "/Content/Blessed.asset", text);
            Assert.AreEqual(_folder.Path + "/Content/Blessed.asset", AssetDatabase.GetAssetPath(draft.Asset));
        }

        [Test]
        public void ApproveIsRefusedWhileErrorsExistAndPreviewsWithDryRun() {
            var draft = DefinitionDrafts.Create<FixtureDefinition>("Wrong", null, _settings);
            var refused = Text("core approve asset:" + draft.Path + " --to " + _folder.Path + "/Content", _approver);
            StringAssert.StartsWith("refused: " + draft.Path + " has 1 error finding(s); fix them first", refused);
            var previewed = Text("core approve asset:" + draft.Path + " --to " + _folder.Path + "/Content --dry-run", _approver);
            Assert.AreEqual("previewed: " + draft.Path + " would be refused: 1 error", previewed);
            draft.Definition.title = "fixed";
            EditorUtility.SetDirty(draft.Asset);
            AssetDatabase.SaveAssets();
            var wouldMove = Text("core approve asset:" + draft.Path + " --to " + _folder.Path + "/Content --dry-run", _approver);
            Assert.AreEqual("previewed: " + draft.Path + " would move to " + _folder.Path + "/Content", wouldMove);
            Assert.AreEqual(draft.Path, AssetDatabase.GetAssetPath(draft.Asset));
        }

        [Test]
        public void ApproveOfAnAssetOutsideTheDraftsIsRefused() {
            var asset = _folder.Create("Settled", "settled", 1);
            var text = Text("core approve asset:" + AssetDatabase.GetAssetPath(asset) + " --to " + _folder.Path + "/Content", _approver);
            StringAssert.StartsWith("refused: " + AssetDatabase.GetAssetPath(asset) + " is not in the drafts folder", text);
        }

        [Test]
        public void TheBridgeAudiencesSettingIsTheOneWayAnAgentApproves() {
            var draft = DefinitionDrafts.Create<FixtureDefinition>("Agentic", definition => definition.title = "agentic", _settings);
            var bridge = new OperationCaller("bridge", _settings.bridgeAudiences);
            StringAssert.StartsWith("refused: ", Text("core approve asset:" + draft.Path + " --to " + _folder.Path + "/Content", bridge));
            _settings.bridgeAudiences = new[] { OperationAudiences.Developer, OperationAudiences.Approver };
            var granted = new OperationCaller("bridge", _settings.bridgeAudiences);
            StringAssert.StartsWith("succeeded: approved Agentic", Text("core approve asset:" + draft.Path + " --to " + _folder.Path + "/Content", granted));
        }
    }
}
