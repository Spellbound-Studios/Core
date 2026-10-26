// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Collections.Generic;
using Spellbound.Core.Authoring;
using Spellbound.Core.Authoring.Editor;
using UnityEngine.UIElements;

namespace Spellbound.Core.Controls.Editor {
    public class ReportView : VisualElement {
        public const string ClassName = "sb-report";
        public const string ChecksClassName = ClassName + "__checks";
        public const string FindingClassName = ClassName + "__finding";
        public const string ErrorClassName = FindingClassName + "--error";
        public const string WarningClassName = FindingClassName + "--warning";
        public const string InformationClassName = FindingClassName + "--information";
        public const string CodeClassName = ClassName + "__code";
        public const string CheckClassName = ClassName + "__check";
        public const string MessageClassName = ClassName + "__message";
        public const string HintClassName = ClassName + "__hint";
        public const string ActionsClassName = ClassName + "__actions";
        public const string ActionClassName = ClassName + "__action";
        public const string BannerClassName = ClassName + "__banner";
        public const string CompactClassName = ClassName + "--compact";
        public const string EmptyClassName = ClassName + "__empty";
        public const string EmptyText = "no findings";
        public const int MaximumActions = 2;

        private readonly List<VisualElement> _findingRows = new List<VisualElement>();
        private bool _compact;

        public ReportView() {
            AddToClassList(ClassName);
            Banner = new HeaderBar();
            Banner.AddToClassList(BannerClassName);
            Banner.RegisterCallback<ClickEvent>(_ => Compact = false);
        }

        public Func<ValidationFinding, IReadOnlyList<FindingAction>> ActionsFor { get; set; }
        public string PreferredHint { get; set; }
        public ValidationRun Run { get; private set; }
        public HeaderBar Banner { get; }
        public IReadOnlyList<VisualElement> FindingRows => _findingRows;

        public bool Compact {
            get => _compact;
            set {
                if (_compact == value) {
                    return;
                }

                _compact = value;
                Rebind();
            }
        }

        public void Bind(ValidationRun run) {
            Run = run;
            Rebind();
        }

        public void Bind(ValidationReport report, IReadOnlyList<string> checks = null) {
            if (report == null) {
                Bind((ValidationRun)null);
                return;
            }

            Bind(new ValidationRun(report.Subject, new[] { report.Subject }, checks, Array.Empty<string>(), report,
                null, DateTime.Now));
        }

        public void Rebind() {
            Clear();
            _findingRows.Clear();
            EnableInClassList(CompactClassName, _compact);

            if (Run == null) {
                return;
            }

            if (_compact) {
                BuildCompact();
            }
            else {
                BuildFull();
            }
        }

        private void BuildCompact() {
            var ordered = Run.Report.Ordered();
            var title = Run.Summary();

            if (ordered.Count > 0) {
                var first = ordered[0];
                Banner.Severity = first.Severity;
                Banner.tooltip = first.ToText();
                title += ": " + first.Code + " " + first.Message;
            }
            else {
                Banner.Severity = null;
                Banner.tooltip = null;
            }

            Banner.Title = title;
            Add(Banner);
        }

        private void BuildFull() {
            var checks = new Label(Run.ChecksLine());
            checks.AddToClassList(ChecksClassName);
            checks.style.whiteSpace = WhiteSpace.Normal;
            Add(checks);
            var ordered = Run.Report.Ordered();

            if (ordered.Count == 0) {
                var empty = new Label(EmptyText);
                empty.AddToClassList(EmptyClassName);
                Add(empty);
                return;
            }

            foreach (var finding in ordered) {
                var row = BuildRow(finding);
                _findingRows.Add(row);
                Add(row);
            }
        }

        private VisualElement BuildRow(ValidationFinding finding) {
            var row = new VisualElement { tooltip = finding.ToText() };
            row.AddToClassList(FindingClassName);
            row.AddToClassList(ClassFor(finding.Severity));
            row.Add(TextLabel(finding.Code, CodeClassName));

            if (finding.Check != null) {
                row.Add(TextLabel(finding.Check, CheckClassName));
            }

            row.Add(TextLabel(finding.Message, MessageClassName));
            var hint = HintFor(finding);

            if (hint != null) {
                row.Add(TextLabel(hint, HintClassName));
            }

            if (finding.FixCode != null && ActionsFor != null) {
                var actions = BuildActions(finding);

                if (actions != null) {
                    row.Add(actions);
                }
            }

            return row;
        }

        private VisualElement BuildActions(ValidationFinding finding) {
            var actions = ActionsFor(finding);

            if (actions == null || actions.Count == 0) {
                return null;
            }

            var container = new VisualElement();
            container.AddToClassList(ActionsClassName);
            container.style.flexDirection = FlexDirection.Row;

            foreach (var action in actions) {
                if (container.childCount == MaximumActions) {
                    break;
                }

                if (action == null) {
                    continue;
                }

                var button = new Button(() => action.Run(finding)) { text = action.Label };
                button.AddToClassList(ActionClassName);
                container.Add(button);
            }

            return container.childCount == 0 ? null : container;
        }

        private string HintFor(ValidationFinding finding) {
            if (PreferredHint != null
                && finding.NamedHints.TryGetValue(PreferredHint, out var named)
                && !string.IsNullOrWhiteSpace(named)) {
                return named;
            }

            return finding.Hint;
        }

        private static Label TextLabel(string text, string className) {
            var label = new Label(text);
            label.AddToClassList(className);
            label.style.whiteSpace = WhiteSpace.Normal;
            return label;
        }

        private static string ClassFor(FindingSeverity severity) {
            switch (severity) {
                case FindingSeverity.Error:
                    return ErrorClassName;
                case FindingSeverity.Warning:
                    return WarningClassName;
                default:
                    return InformationClassName;
            }
        }
    }
}
