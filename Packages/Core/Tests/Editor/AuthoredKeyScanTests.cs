// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Spellbound.Core.Authoring;
using Spellbound.Core.Authoring.Editor;
using Spellbound.Core.Operations;
using Spellbound.Core.Tests.AuthoringFixture;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Spellbound.Core.Tests {
    public class AuthoredKeyScanTests {
        private const string MissingAssembly = "Spellbound.Core.Tests.AuthoringFixture.Missing";
        private const string AbsentAssembly = "Spellbound.Core.Tests.AuthoringFixture.Absent";
        private const string MissingLayerType =
            "Spellbound.Core.Tests.AuthoringFixtureMissing.FixtureMissingLayer, " + MissingAssembly;

        private AuthoringTestFolder _folder;
        private DefinitionValidationRunner _runner;
        private OperationSettings _settings;

        [SetUp]
        public void SetUp() {
            _folder = new AuthoringTestFolder();
            _runner = new DefinitionValidationRunner();
            _settings = ScriptableObject.CreateInstance<OperationSettings>();
            DefinitionReportCache.Clear();
        }

        [TearDown]
        public void TearDown() {
            Object.DestroyImmediate(_settings);
            _folder.Dispose();
        }

        [Test]
        public void AnImportedKeyTheTypeLacksStaysInTheTextAndIsAnError() {
            var path = AssetDatabase.GetAssetPath(_folder.Create("Stale", "stale", 3));
            InsertAfter(path, "  threshold: 3", "  oldThreshold: 7");
            var asset = Reimport<FixtureDefinition>(path);

            StringAssert.Contains("oldThreshold: 7", File.ReadAllText(path));
            Assert.AreEqual(3, asset.threshold);
            var findings = Findings(_runner.ValidateAsset(asset).Report, DefinitionValidationRunner.UnknownKeyCode);
            Assert.AreEqual(1, findings.Count);
            Assert.AreEqual(FindingSeverity.Error, findings[0].Severity);
            Assert.AreEqual("oldThreshold", findings[0].Location);
            Assert.IsNull(findings[0].Check);
            Assert.AreEqual("Stale oldThreshold: stored 7, the type has no field oldThreshold; re-author it under its "
                            + "new name before this asset is saved again", findings[0].Message);
        }

        [Test]
        public void SavingTheAssetAgainDropsTheKeyAndTheScanIsClean() {
            var path = AssetDatabase.GetAssetPath(_folder.Create("Saved", "saved", 3));
            InsertAfter(path, "  threshold: 3", "  oldThreshold: 7");
            var asset = Reimport<FixtureDefinition>(path);

            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();

            StringAssert.DoesNotContain("oldThreshold", File.ReadAllText(path));
            Assert.IsTrue(_runner.ValidateAsset(asset).Report.IsEmpty);
        }

        [Test]
        public void TheScanRunsAfterTheOwnValidationAndBeforeTheRegisteredChecks() {
            var path = AssetDatabase.GetAssetPath(_folder.Create("Ordered", string.Empty, 250));
            InsertAfter(path, "  threshold: 250", "  oldThreshold: 7");
            var report = _runner.ValidateAsset(Reimport<FixtureDefinition>(path)).Report;

            Assert.AreEqual(3, report.Findings.Count);
            Assert.AreEqual(FixtureDefinition.TitleEmptyCode, report.Findings[0].Code);
            Assert.AreEqual(DefinitionValidationRunner.UnknownKeyCode, report.Findings[1].Code);
            Assert.AreEqual("fixturecheck.threshold-high", report.Findings[2].Code);
        }

        [Test]
        public void AStaleKeyInsideAReferencedLayerNamesTheLayer() {
            var layered = CreateLayered("Layered", new FixtureLayer { label = "first" },
                new FixtureLayer { label = "second" });
            var path = AssetDatabase.GetAssetPath(layered);
            var identifier = ManagedReferenceUtility.GetManagedReferenceIdForObject(layered, layered.layers[1]);
            InsertIntoData(path, identifier, "oldWeight: 4");
            var asset = Reimport<FixtureLayeredDefinition>(path);

            var report = _runner.ValidateAsset(asset).Report;
            var findings = Findings(report, DefinitionValidationRunner.UnknownKeyCode);
            Assert.AreEqual(1, report.Findings.Count, report.ToText());
            Assert.AreEqual(1, findings.Count);
            Assert.AreEqual("layers[1].oldWeight", findings[0].Location);
            StringAssert.Contains("stored 4, the type has no field oldWeight", findings[0].Message);
        }

        [Test]
        public void AFormerNameIsARenamedKeyAndLoadsIntoTheNewField() {
            var renamed = ScriptableObject.CreateInstance<FixtureRenamedDefinition>();
            renamed.label = "kept";
            var path = _folder.Path + "/Renamed.asset";
            AssetDatabase.CreateAsset(renamed, path);
            AssetDatabase.SaveAssets();
            Replace(path, "  label: kept", "  oldName: kept");
            var asset = Reimport<FixtureRenamedDefinition>(path);

            Assert.AreEqual("kept", asset.label);
            var report = _runner.ValidateAsset(asset).Report;
            Assert.IsFalse(report.HasErrors, report.ToText());
            var findings = Findings(report, DefinitionValidationRunner.RenamedKeyCode);
            Assert.AreEqual(1, findings.Count);
            Assert.AreEqual(FindingSeverity.Information, findings[0].Severity);
            Assert.AreEqual("oldName", findings[0].Location);
            StringAssert.Contains("label", findings[0].Message);
        }

        [Test]
        public void AReferenceFromAnAssemblyNotLoadedIsOneMissingTypeAndItsKeysAreSkipped() {
            var layered = CreateLayered("Missing", MissingLayer("gone"), MissingLayer("also gone"),
                new FixtureLayer { label = "kept" });
            var path = AssetDatabase.GetAssetPath(layered);
            var identifier = ManagedReferenceUtility.GetManagedReferenceIdForObject(layered, layered.layers[0]);
            InsertIntoData(path, identifier, "oldWeight: 4");
            Replace(path, "asm: " + MissingAssembly + "}", "asm: " + AbsentAssembly + "}");
            ExpectMissingTypesWarning("Missing");
            var asset = Reimport<FixtureLayeredDefinition>(path);

            var report = _runner.ValidateAsset(asset).Report;
            var findings = Findings(report, DefinitionValidationRunner.MissingTypeCode);
            Assert.AreEqual(1, report.Findings.Count, report.ToText());
            Assert.AreEqual(1, findings.Count);
            Assert.AreEqual(FindingSeverity.Information, findings[0].Severity);
            StringAssert.Contains(AbsentAssembly, findings[0].Message);
            StringAssert.Contains("the Core package", findings[0].Message);
        }

        [Test]
        public void AClassGoneFromALoadedAssemblyIsAMissingTypeWarning() {
            var layered = CreateLayered("Gone", MissingLayer("gone"));
            var path = AssetDatabase.GetAssetPath(layered);
            Replace(path, "class: FixtureMissingLayer,", "class: FixtureRetiredLayer,");
            ExpectMissingTypesWarning("Gone");
            var asset = Reimport<FixtureLayeredDefinition>(path);

            var report = _runner.ValidateAsset(asset).Report;
            var findings = Findings(report, DefinitionValidationRunner.MissingTypeCode);
            Assert.AreEqual(1, report.Findings.Count, report.ToText());
            Assert.AreEqual(FindingSeverity.Warning, findings[0].Severity);
            Assert.AreEqual("layers[0]", findings[0].Location);
            StringAssert.Contains("FixtureRetiredLayer", findings[0].Message);
        }

        [Test]
        public void AReferenceToAUnityTypeIsComparedAndIsNotMissing() {
            var foreign = ScriptableObject.CreateInstance<FixtureForeignReferenceDefinition>();
            foreign.value = new UnityEvent();
            var path = _folder.Path + "/Foreign.asset";
            AssetDatabase.CreateAsset(foreign, path);
            AssetDatabase.SaveAssets();
            var identifier = ManagedReferenceUtility.GetManagedReferenceIdForObject(foreign, foreign.value);
            InsertIntoData(path, identifier, "oldListener: 5");
            var asset = Reimport<FixtureForeignReferenceDefinition>(path);

            var report = _runner.ValidateAsset(asset).Report;
            Assert.IsEmpty(Findings(report, DefinitionValidationRunner.MissingTypeCode), report.ToText());
            var findings = Findings(report, DefinitionValidationRunner.UnknownKeyCode);
            Assert.AreEqual(1, report.Findings.Count, report.ToText());
            Assert.AreEqual("value.oldListener", findings[0].Location);
        }

        [Test]
        public void AStoredValueLongerThanTheSettingIsCut() {
            _settings.maximumStoredValueLength = 10;
            var path = AssetDatabase.GetAssetPath(_folder.Create("Long", "long", 3));
            InsertAfter(path, "  threshold: 3", "  oldThreshold: 123456789012345");
            var asset = Reimport<FixtureDefinition>(path);
            var report = new ValidationReport(path);

            new AuthoredKeyScan(_settings).Scan(asset, report);

            Assert.AreEqual(1, report.Findings.Count);
            StringAssert.Contains("stored 1234567890" + AuthoredKeyScan.CutMarker + ", the type",
                report.Findings[0].Message);
        }

        [Test]
        public void AValueWithinTheSettingIsWhole() {
            var path = AssetDatabase.GetAssetPath(_folder.Create("Short", "short", 3));
            InsertAfter(path, "  threshold: 3", "  oldThreshold: 123456789012345");
            var report = new ValidationReport(path);

            new AuthoredKeyScan(_settings).Scan(Reimport<FixtureDefinition>(path), report);

            StringAssert.Contains("stored 123456789012345, the type", report.Findings[0].Message);
        }

        [Test]
        public void AnEmptyStoredValueReadsAsNothing() {
            var path = AssetDatabase.GetAssetPath(_folder.Create("Empty", "empty", 3));
            InsertAfter(path, "  threshold: 3", "  oldNote:");
            var report = new ValidationReport(path);

            new AuthoredKeyScan(_settings).Scan(Reimport<FixtureDefinition>(path), report);

            Assert.AreEqual(1, report.Findings.Count, report.ToText());
            StringAssert.Contains("oldNote: stored nothing, the type has no field oldNote", report.Findings[0].Message);
        }

        [Test]
        public void AProjectOnBinarySerializationGetsOneInformationLine() {
            var asset = _folder.Create("Binary", "binary", 3);
            var report = new ValidationReport(AssetDatabase.GetAssetPath(asset));

            new AuthoredKeyScan(_settings, SerializationMode.ForceBinary).Scan(asset, report);

            Assert.AreEqual(1, report.Findings.Count);
            Assert.AreEqual(DefinitionValidationRunner.ScanUnavailableCode, report.Findings[0].Code);
            Assert.AreEqual(FindingSeverity.Information, report.Findings[0].Severity);
        }

        [Test]
        public void AnAssetWithoutAPathIsNotScanned() {
            var asset = ScriptableObject.CreateInstance<FixtureDefinition>();
            var report = new ValidationReport("loose");

            try {
                new AuthoredKeyScan(_settings, SerializationMode.ForceBinary).Scan(asset, report);
            }
            finally {
                Object.DestroyImmediate(asset);
            }

            Assert.IsTrue(report.IsEmpty);
        }

        [Test]
        public void AScanThatThrowsStillGivesAReportWithTheScanError() {
            var layered = CreateLayered("Throwing", new FixtureThrowingLayer { label = "throwing" });
            FixtureThrowingLayer.ThrowOnConstruction = true;
            ValidationRun run;

            try {
                run = _runner.ValidateAsset(layered);
            }
            finally {
                FixtureThrowingLayer.ThrowOnConstruction = false;
            }

            var findings = Findings(run.Report, DefinitionValidationRunner.ScanThrewCode);
            Assert.AreEqual(1, findings.Count, run.Report.ToText());
            Assert.AreEqual(FindingSeverity.Error, findings[0].Severity);
            StringAssert.Contains(nameof(InvalidOperationException), findings[0].Message);
            StringAssert.Contains(FixtureThrowingLayer.ConstructionMessage, findings[0].Message);
        }

        [Test]
        public void ABackingFieldKeyDoesNotHideTheStaleKeyAfterIt() {
            var path = AssetDatabase.GetAssetPath(_folder.Create("Backed", "backed", 3));
            InsertAfter(path, "  threshold: 3", "  <Items>k__BackingField:\n  - 1\n  - 2\n  oldThreshold: 7");
            var asset = Reimport<FixtureDefinition>(path);

            var report = _runner.ValidateAsset(asset).Report;
            var locations = new List<string>();

            foreach (var finding in Findings(report, DefinitionValidationRunner.UnknownKeyCode)) {
                locations.Add(finding.Location);
            }

            Assert.AreEqual(new[] { "<Items>k__BackingField", "oldThreshold" }, locations, report.ToText());
        }

        private FixtureLayeredDefinition CreateLayered(string name, params FixtureLayer[] layers) {
            var layered = ScriptableObject.CreateInstance<FixtureLayeredDefinition>();
            layered.layers.AddRange(layers);
            AssetDatabase.CreateAsset(layered, _folder.Path + "/" + name + ".asset");
            AssetDatabase.SaveAssets();
            return layered;
        }

        private static void ExpectMissingTypesWarning(string assetName) {
            LogAssert.Expect(LogType.Warning,
                new Regex("^Missing types referenced from component FixtureLayeredDefinition on game object "
                          + assetName + ":"));
        }

        private static FixtureLayer MissingLayer(string label) {
            var type = Type.GetType(MissingLayerType, true);
            var layer = (FixtureLayer)Activator.CreateInstance(type);
            layer.label = label;
            return layer;
        }

        private static T Reimport<T>(string path) where T : ScriptableObject {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            Assert.IsNotNull(asset, path + " did not load as " + typeof(T).Name);
            return asset;
        }

        private static void InsertAfter(string path, string anchor, string line) {
            var lines = new List<string>(File.ReadAllLines(path));
            var index = lines.IndexOf(anchor);
            Assert.GreaterOrEqual(index, 0, "'" + anchor + "' is not a line of " + path);
            lines.Insert(index + 1, line);
            File.WriteAllText(path, string.Join("\n", lines) + "\n");
        }

        private static void InsertIntoData(string path, long identifier, string entry) {
            var lines = new List<string>(File.ReadAllLines(path));
            var registryIndex = lines.FindIndex(line => line.Trim() == "RefIds:");
            Assert.GreaterOrEqual(registryIndex, 0, "no references in " + path);
            var entryIndex = lines.FindIndex(registryIndex, line => line.Trim() == "- rid: " + identifier);
            Assert.GreaterOrEqual(entryIndex, 0, "no reference " + identifier + " in " + path);
            var dataIndex = lines.FindIndex(entryIndex, line => line.Trim() == "data:");
            var indent = lines[dataIndex + 1].Length - lines[dataIndex + 1].TrimStart(' ').Length;
            lines.Insert(dataIndex + 1, new string(' ', indent) + entry);
            File.WriteAllText(path, string.Join("\n", lines) + "\n");
        }

        private static void Replace(string path, string from, string to) {
            var text = File.ReadAllText(path);
            StringAssert.Contains(from, text);
            File.WriteAllText(path, text.Replace(from, to));
        }

        private static List<ValidationFinding> Findings(ValidationReport report, string code) {
            var findings = new List<ValidationFinding>();

            foreach (var finding in report.Findings) {
                if (finding.Code == code) {
                    findings.Add(finding);
                }
            }

            return findings;
        }
    }
}
