// Copyright (c) Spellbound Studios. All rights reserved.

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Spellbound.Core.Controls.Editor;
using Spellbound.Core.Packages.Editor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Spellbound.Core.Tests {
    public class PackageIdentityInspectorTests {
        private const string Job = "Gives a game one plain thing.";

        private PackageIdentity _identity;
        private UnityEditor.Editor _inspector;

        [SetUp]
        public void SetUp() {
            _identity = ScriptableObject.CreateInstance<PackageIdentity>();
            _identity.kind = "library";
            _identity.layer = "mechanism";
            _identity.frontDoor = "FixtureKit";
            _identity.mustNotOwn = new[] { "anything drawn" };
        }

        [TearDown]
        public void TearDown() {
            if (_inspector != null) {
                UnityEngine.Object.DestroyImmediate(_inspector);
            }

            UnityEngine.Object.DestroyImmediate(_identity);
        }

        [Test]
        public void ThePackageIdentityIsDrawnByItsInspector() {
            Draw();
            Assert.IsInstanceOf<PackageIdentityInspector>(_inspector);
        }

        [Test]
        public void AHeaderNamesTheJobAndTheFourCardsThenTheReportFollow() {
            _identity.job = Job;
            var root = Draw();
            Assert.IsTrue(root.ClassListContains(PackageIdentityInspector.ClassName));
            Assert.IsInstanceOf<HeaderBar>(root[0]);
            Assert.AreEqual(Job, ((HeaderBar)root[0]).Title);
            var cards = root.Query<SectionCard>().ToList();
            CollectionAssert.AreEqual(
                new[] {
                    PackageIdentityInspector.PackageCardName, PackageIdentityInspector.MustNotOwnCardName,
                    PackageIdentityInspector.CheckedCardName, PackageIdentityInspector.SkeletonCardName
                },
                cards.Select(card => card.name).ToArray());
            CollectionAssert.AreEqual(
                new[] { "What the package is", "What it must not own", "How it is checked", "Skeleton" },
                cards.Select(card => card.Title).ToArray());
            var report = root.Q<ReportView>(PackageIdentityInspector.ReportName);
            Assert.IsNotNull(report);
            Assert.AreSame(report, root[root.childCount - 1]);
        }

        [Test]
        public void EachCardHoldsItsFields() {
            var root = Draw();
            CollectionAssert.AreEqual(
                new[] {
                    nameof(PackageIdentity.job), nameof(PackageIdentity.kind), nameof(PackageIdentity.layer),
                    nameof(PackageIdentity.status), nameof(PackageIdentity.frontDoor)
                },
                BindingPathsIn(Card(root, PackageIdentityInspector.PackageCardName)));
            CollectionAssert.AreEqual(new[] { nameof(PackageIdentity.mustNotOwn) },
                BindingPathsIn(Card(root, PackageIdentityInspector.MustNotOwnCardName)));
            CollectionAssert.AreEqual(
                new[] {
                    nameof(PackageIdentity.hotPath), nameof(PackageIdentity.deterministicOutput),
                    nameof(PackageIdentity.simulation), nameof(PackageIdentity.machineMatchedJobs)
                },
                BindingPathsIn(Card(root, PackageIdentityInspector.CheckedCardName)));
            CollectionAssert.AreEqual(new[] { nameof(PackageIdentity.skeletonVersion) },
                BindingPathsIn(Card(root, PackageIdentityInspector.SkeletonCardName)));
            Assert.IsInstanceOf<PropertyField>(FieldFor(root, nameof(PackageIdentity.mustNotOwn)));
            Assert.IsInstanceOf<IntegerField>(FieldFor(root, nameof(PackageIdentity.skeletonVersion)));
        }

        [Test]
        public void EveryJudgmentFieldIsMarkedForReview() {
            var root = Draw();
            Assert.AreEqual("sb-judgment", PackageIdentityInspector.JudgmentClassName);

            foreach (var field in PackageIdentity.JudgmentFields) {
                var element = FieldFor(root, field);
                Assert.IsTrue(element.ClassListContains(PackageIdentityInspector.JudgmentClassName), field);
                Assert.AreEqual("A code owner reviews every change to this field.", element.tooltip, field);
            }

            var skeleton = FieldFor(root, nameof(PackageIdentity.skeletonVersion));
            Assert.IsFalse(skeleton.ClassListContains(PackageIdentityInspector.JudgmentClassName));
            Assert.IsTrue(string.IsNullOrEmpty(skeleton.tooltip));
            Assert.AreEqual(PackageIdentity.JudgmentFields.Count,
                root.Query<VisualElement>(className: PackageIdentityInspector.JudgmentClassName).ToList().Count);
        }

        [Test]
        public void KindLayerAndStatusOfferTheKnownValues() {
            var root = Draw();
            AssertChoice(root, nameof(PackageIdentity.kind), PackageIdentity.Kinds, "library");
            AssertChoice(root, nameof(PackageIdentity.layer), PackageIdentity.Layers, "mechanism");
            AssertChoice(root, nameof(PackageIdentity.status), PackageIdentity.Statuses, PackageIdentity.ActiveStatus);
        }

        [Test]
        public void AnUnknownCurrentValueJoinsTheChoicesAndAnEmptyOneDoesNot() {
            _identity.kind = "plug-in";
            _identity.layer = string.Empty;
            _identity.status = "paused";
            var root = Draw();
            AssertChoice(root, nameof(PackageIdentity.kind), PackageIdentity.Kinds.Append("plug-in").ToList(),
                "plug-in");
            AssertChoice(root, nameof(PackageIdentity.layer), PackageIdentity.Layers, string.Empty);
            AssertChoice(root, nameof(PackageIdentity.status), PackageIdentity.Statuses.Append("paused").ToList(),
                "paused");
        }

        [Test]
        public void TheReportShowsAFindingForAnEmptyJobAndNoneOnceTheJobIsSet() {
            var report = Draw().Q<ReportView>(PackageIdentityInspector.ReportName);
            CollectionAssert.AreEqual(new[] { PackageIdentity.JobEmptyCode }, CodesIn(report));
            StringAssert.StartsWith("checks: " + nameof(PackageIdentity),
                report.Q<Label>(className: ReportView.ChecksClassName).text);
            var job = _inspector.serializedObject.FindProperty(nameof(PackageIdentity.job));
            job.stringValue = Job;
            _inspector.serializedObject.ApplyModifiedPropertiesWithoutUndo();
            report = _inspector.CreateInspectorGUI().Q<ReportView>(PackageIdentityInspector.ReportName);
            CollectionAssert.IsEmpty(CodesIn(report));
            Assert.AreEqual(ReportView.EmptyText, report.Q<Label>(className: ReportView.EmptyClassName).text);
        }

        [Test]
        public void TheReportAndTheHeaderFollowAFieldAsItChanges() {
            var root = Draw();
            var window = ControlTestWindow.Open(root);

            try {
                Assert.IsNotNull(root.panel, "the test window gave the inspector no panel");
                var report = root.Q<ReportView>(PackageIdentityInspector.ReportName);
                CollectionAssert.AreEqual(new[] { PackageIdentity.JobEmptyCode }, CodesIn(report));
                _identity.job = Job;
                ((TextField)FieldFor(root, nameof(PackageIdentity.job))).value = Job;
                CollectionAssert.IsEmpty(CodesIn(report));
                Assert.AreEqual(Job, ((HeaderBar)root[0]).Title);
            }
            finally {
                window.Close();
            }
        }

        private VisualElement Draw() {
            _inspector = UnityEditor.Editor.CreateEditor(_identity);
            return _inspector.CreateInspectorGUI();
        }

        private static SectionCard Card(VisualElement root, string name) {
            var card = root.Q<SectionCard>(name);
            Assert.IsNotNull(card, name);
            return card;
        }

        private static List<string> BindingPathsIn(VisualElement container) {
            return container.Query<VisualElement>().ToList().OfType<IBindable>()
                .Select(field => field.bindingPath)
                .Where(path => !string.IsNullOrEmpty(path))
                .ToList();
        }

        private static VisualElement FieldFor(VisualElement root, string field) {
            var found = root.Query<VisualElement>().ToList()
                .FirstOrDefault(element => element is IBindable bindable && bindable.bindingPath == field);
            Assert.IsNotNull(found, "no field is bound to " + field);
            return found;
        }

        private static void AssertChoice(VisualElement root, string field, IReadOnlyList<string> expected,
            string shown) {
            var choice = FieldFor(root, field) as DropdownField;
            Assert.IsNotNull(choice, field + " is not a dropdown");
            CollectionAssert.AreEqual(expected, choice.choices, field);
            Assert.AreEqual(shown, choice.value, field);
        }

        private static List<string> CodesIn(ReportView report) {
            return report.FindingRows.Select(row => row.Q<Label>(className: ReportView.CodeClassName).text).ToList();
        }
    }
}
