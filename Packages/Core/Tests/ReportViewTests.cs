// Copyright (c) Spellbound Studios. All rights reserved.

using System.Collections.Generic;
using NUnit.Framework;
using Spellbound.Core.Authoring;
using Spellbound.Core.Authoring.Editor;
using Spellbound.Core.Controls.Editor;
using UnityEngine.UIElements;

namespace Spellbound.Core.Tests {
    public class ReportViewTests {
        private const string Subject = "Assets/Fixture.asset";
        private const string CheckName = "fixture.threshold (Fixture)";
        private const string OwnValidation = "FixtureDefinition (Core)";

        private ValidationReport _report;
        private ValidationFinding _error;
        private ReportView _view;

        [SetUp]
        public void SetUp() {
            _report = new ValidationReport(Subject);
            _report.Add(new ValidationFinding("fixture.threshold-high", FindingSeverity.Error, Subject,
                "the threshold is above the ceiling", "threshold", "Lower it.", "fixture.lower-threshold",
                new Dictionary<string, string> { { "blender", "Rename it in Blender" } }));
            _report.StampCheck(0, CheckName);
            _report.Information("fixture.note", "a note");
            _report.Warning("fixture.title-long", "the title is long", "title", "Shorten it.");
            _error = _report.Ordered()[0];
            _view = new ReportView();
            _view.Bind(_report, new[] { OwnValidation });
        }

        [Test]
        public void TheRowsFollowTheReportOrderWithTheirSeverityClasses() {
            Assert.IsTrue(_view.ClassListContains(ReportView.ClassName));
            var rows = _view.FindingRows;
            Assert.AreEqual(3, rows.Count);
            Assert.IsTrue(rows[0].ClassListContains(ReportView.ErrorClassName));
            Assert.IsTrue(rows[1].ClassListContains(ReportView.WarningClassName));
            Assert.IsTrue(rows[2].ClassListContains(ReportView.InformationClassName));

            foreach (var row in rows) {
                Assert.IsTrue(row.ClassListContains(ReportView.FindingClassName));
                Assert.IsNotNull(row.Q<Label>(className: ReportView.CodeClassName));
                Assert.IsNotNull(row.Q<Label>(className: ReportView.MessageClassName));
            }

            Assert.AreEqual("fixture.threshold-high", rows[0].Q<Label>(className: ReportView.CodeClassName).text);
            Assert.AreEqual("the title is long", rows[1].Q<Label>(className: ReportView.MessageClassName).text);
            Assert.IsNull(rows[2].Q<Label>(className: ReportView.HintClassName));
        }

        [Test]
        public void TheChecksLineLeadsTheFullForm() {
            var checks = _view.Q<Label>(className: ReportView.ChecksClassName);
            Assert.AreEqual("checks: " + OwnValidation, checks.text);
            Assert.AreSame(checks, _view[0]);
        }

        [Test]
        public void OnlyAFindingWithACheckShowsTheCheckName() {
            var rows = _view.FindingRows;
            Assert.AreEqual(CheckName, rows[0].Q<Label>(className: ReportView.CheckClassName).text);
            Assert.IsNull(rows[1].Q<Label>(className: ReportView.CheckClassName));
            Assert.IsNull(rows[2].Q<Label>(className: ReportView.CheckClassName));
        }

        [Test]
        public void NoButtonIsBuiltWithoutActionsFor() {
            Assert.AreEqual(0, _view.Query<Button>().ToList().Count);
            Assert.IsNull(_view.Q<VisualElement>(className: ReportView.ActionsClassName));
        }

        [Test]
        public void AtMostTwoButtonsByDefaultAndOnlyOnAFindingWithAFixCode() {
            _view.ActionsFor = ThreeActions;
            _view.Rebind();
            var rows = _view.FindingRows;
            var buttons = rows[0].Query<Button>().ToList();
            Assert.AreEqual(ReportView.DefaultMaximumActions, _view.MaximumActions);
            Assert.AreEqual(2, buttons.Count);
            Assert.AreEqual("First", buttons[0].text);
            Assert.AreEqual("Second", buttons[1].text);
            Assert.IsTrue(buttons[0].ClassListContains(ReportView.ActionClassName));
            Assert.IsTrue(buttons[0].parent.ClassListContains(ReportView.ActionsClassName));
            Assert.AreEqual(0, rows[1].Query<Button>().ToList().Count);
            Assert.AreEqual(0, rows[2].Query<Button>().ToList().Count);
        }

        [Test]
        public void TheActionCapIsAParameterAndZeroLiftsIt() {
            _view.ActionsFor = ThreeActions;
            _view.MaximumActions = 1;
            _view.Rebind();
            Assert.AreEqual(1, _view.FindingRows[0].Query<Button>().ToList().Count);
            _view.MaximumActions = 0;
            _view.Rebind();
            Assert.AreEqual(3, _view.FindingRows[0].Query<Button>().ToList().Count);
        }

        [Test]
        public void ReportView_CustomImplementation() {
            var view = new TaggedReportView { ActionsFor = ThreeActions, MaximumActions = 0 };
            view.Bind(_report, new[] { OwnValidation });
            Assert.AreEqual("tagged-banner", view.Banner.name);
            Assert.AreEqual("tagged-checks", view.Q<Label>(className: ReportView.ChecksClassName).name);
            Assert.AreEqual("tagged-row", view.FindingRows[0].name);
            Assert.AreEqual("tagged-text", view.FindingRows[0].Q<Label>(className: ReportView.CodeClassName).name);
            var actions = view.FindingRows[0].Q<VisualElement>(className: ReportView.ActionsClassName);
            Assert.AreEqual("tagged-actions", actions.name);
            Assert.AreEqual(3, actions.childCount);
            Assert.AreEqual("tagged-button", actions[0].name);
            Assert.IsTrue(view.styleSheets.Contains(ControlStyleSheet.Default));
            var empty = new TaggedReportView();
            empty.Bind(new ValidationReport(Subject));
            Assert.AreEqual("tagged-empty", empty.Q<Label>(className: ReportView.EmptyClassName).name);
        }

        private sealed class TaggedReportView : ReportView {
            protected override HeaderBar CreateBanner() => new HeaderBar { name = "tagged-banner" };

            protected override Label CreateChecksLine(string text) => new Label(text) { name = "tagged-checks" };

            protected override Label CreateEmpty() => new Label(EmptyText) { name = "tagged-empty" };

            protected override VisualElement CreateRow(ValidationFinding finding) =>
                new VisualElement { name = "tagged-row" };

            protected override Label CreateText(string text, string className) =>
                new Label(text) { name = "tagged-text" };

            protected override VisualElement CreateActions(ValidationFinding finding) =>
                new VisualElement { name = "tagged-actions" };

            protected override Button CreateActionButton(ValidationFinding finding, FindingAction action) =>
                new Button(() => action.Run(finding)) { name = "tagged-button", text = action.Label };
        }

        [Test]
        public void AButtonRunsItsActionWithTheFinding() {
            ValidationFinding ran = null;
            _view.ActionsFor = finding => new[] { new FindingAction("Fix", picked => ran = picked) };
            _view.Rebind();
            var button = _view.FindingRows[0].Q<Button>();
            var window = ControlTestWindow.Open(_view);

            try {
                Assert.IsNotNull(button.panel, "the test window gave the view no panel");

                using (var navigationSubmit = NavigationSubmitEvent.GetPooled()) {
                    navigationSubmit.target = button;
                    button.SendEvent(navigationSubmit);
                }

                Assert.AreSame(_error, ran);
            }
            finally {
                window.Close();
            }
        }

        [Test]
        public void ThePreferredNamedHintWinsWhereTheFindingHasIt() {
            Assert.AreEqual("Lower it.", _view.FindingRows[0].Q<Label>(className: ReportView.HintClassName).text);
            _view.PreferredHint = "blender";
            _view.Rebind();
            var rows = _view.FindingRows;
            Assert.AreEqual("Rename it in Blender", rows[0].Q<Label>(className: ReportView.HintClassName).text);
            Assert.AreEqual("Shorten it.", rows[1].Q<Label>(className: ReportView.HintClassName).text);
        }

        [Test]
        public void TheCompactFormIsOneBannerWithTheWorstSeverityAndTheFirstFinding() {
            _view.Compact = true;
            Assert.IsTrue(_view.ClassListContains(ReportView.CompactClassName));
            Assert.AreEqual(1, _view.childCount);
            Assert.AreSame(_view.Banner, _view[0]);
            Assert.IsTrue(_view.Banner.ClassListContains(ReportView.BannerClassName));
            Assert.IsTrue(_view.Banner.ClassListContains(HeaderStates.ClassNameFor(HeaderStates.Error)));
            StringAssert.StartsWith(_view.Run.Summary(), _view.Banner.Title);
            Assert.AreEqual(
                "1 error, 1 warning, 1 information in 1 asset: "
                + "fixture.threshold-high the threshold is above the ceiling",
                _view.Banner.Title);
            Assert.AreEqual(0, _view.FindingRows.Count);
        }

        [Test]
        public void LeavingTheCompactFormRestoresTheRows() {
            _view.Compact = true;
            _view.Compact = false;
            Assert.IsFalse(_view.ClassListContains(ReportView.CompactClassName));
            Assert.AreEqual(3, _view.FindingRows.Count);
            Assert.IsNull(_view.Banner.parent);
        }

        [Test]
        public void BindingNothingEmptiesTheView() {
            _view.Bind((ValidationRun)null);
            Assert.IsNull(_view.Run);
            Assert.AreEqual(0, _view.childCount);
            Assert.AreEqual(0, _view.FindingRows.Count);
        }

        [Test]
        public void AnEmptyReportSaysSo() {
            var view = new ReportView();
            view.Bind(new ValidationReport(Subject));
            Assert.AreEqual(ReportView.DefaultEmptyText, view.Q<Label>(className: ReportView.EmptyClassName).text);
            Assert.AreEqual("checks: none", view.Q<Label>(className: ReportView.ChecksClassName).text);
            Assert.AreEqual(0, view.FindingRows.Count);
            view.Compact = true;
            Assert.AreEqual(HeaderStates.Passed, view.Banner.State);
            Assert.AreEqual("no findings in 1 asset", view.Banner.Title);
        }

        [Test]
        public void AnEmptyReportShowsPassedAsAStateOfItsOwn() {
            var view = new ReportView();
            Assert.AreEqual(HeaderStates.Neutral, view.State);
            Assert.IsTrue(view.ClassListContains(ReportView.StateClassNameFor(HeaderStates.Neutral)));
            view.Bind(new ValidationReport(Subject));
            Assert.AreEqual(HeaderStates.Passed, view.State);
            Assert.IsTrue(view.ClassListContains("sb-report--passed"));
            Assert.IsFalse(view.ClassListContains("sb-report--neutral"));
            Assert.AreEqual(HeaderStates.Passed, view.Banner.State);
            view.Bind((ValidationRun)null);
            Assert.AreEqual(HeaderStates.Neutral, view.State);
            Assert.AreEqual(HeaderStates.Neutral, view.Banner.State);
        }

        [Test]
        public void AReportWithFindingsTakesTheStateOfItsWorstFinding() {
            Assert.AreEqual(HeaderStates.Error, _view.State);
            Assert.IsTrue(_view.ClassListContains("sb-report--error"));
            _view.Compact = true;
            Assert.AreEqual(HeaderStates.Error, _view.Banner.State);
            var warnings = new ValidationReport(Subject);
            warnings.Warning("fixture.title-long", "the title is long");
            var view = new ReportView();
            view.Bind(warnings);
            Assert.AreEqual(HeaderStates.Warning, view.State);
        }

        [Test]
        public void TheEmptyTextIsSettableBeforeAndAfterBindingAndBlankFallsBackToTheDefault() {
            var view = new ReportView { EmptyText = "In sync" };
            view.Bind(new ValidationReport(Subject));
            Assert.AreEqual("In sync", view.Q<Label>(className: ReportView.EmptyClassName).text);
            view.EmptyText = "All clear";
            Assert.AreEqual("All clear", view.Q<Label>(className: ReportView.EmptyClassName).text);
            view.EmptyText = " ";
            Assert.AreEqual(ReportView.DefaultEmptyText, view.EmptyText);
            Assert.AreEqual(ReportView.DefaultEmptyText, view.Q<Label>(className: ReportView.EmptyClassName).text);
        }

        private static IReadOnlyList<FindingAction> ThreeActions(ValidationFinding finding) {
            return new[] {
                new FindingAction("First", _ => { }),
                new FindingAction("Second", _ => { }),
                new FindingAction("Third", _ => { })
            };
        }
    }
}
