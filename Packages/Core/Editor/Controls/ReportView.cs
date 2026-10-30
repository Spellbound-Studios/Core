// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Collections.Generic;
using Spellbound.Core.Authoring;
using Spellbound.Core.Authoring.Editor;
using UnityEngine.UIElements;

namespace Spellbound.Core.Controls.Editor {
    [UxmlElement]
    public partial class ReportView : VisualElement {
        public const string ClassName = "sb-report";
        public const string StateClassPrefix = ClassName + "--";
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
        public const string DefaultEmptyText = "no findings";
        public const int DefaultMaximumActions = 2;

        private readonly List<VisualElement> _findingRows = new List<VisualElement>();
        private bool _compact;
        private string _state = HeaderStates.Neutral;
        private string _emptyText = DefaultEmptyText;

        public ReportView() {
            AddToClassList(ClassName);
            AddToClassList(StateClassNameFor(_state));
            ControlStyleSheet.Apply(this);
            Banner = CreateBanner();
            Banner.AddToClassList(BannerClassName);
            Banner.RegisterCallback<ClickEvent>(_ => Compact = false);
        }

        public Func<ValidationFinding, IReadOnlyList<FindingAction>> ActionsFor { get; set; }

        [UxmlAttribute]
        public string PreferredHint { get; set; }

        [UxmlAttribute]
        public int MaximumActions { get; set; } = DefaultMaximumActions;

        [UxmlAttribute]
        public string EmptyText {
            get => _emptyText;
            set {
                var text = string.IsNullOrWhiteSpace(value) ? DefaultEmptyText : value;

                if (string.Equals(_emptyText, text, StringComparison.Ordinal)) {
                    return;
                }

                _emptyText = text;

                if (Run != null) {
                    Rebind();
                }
            }
        }

        public ValidationRun Run { get; private set; }
        public HeaderBar Banner { get; }
        public string State => _state;
        public IReadOnlyList<VisualElement> FindingRows => _findingRows;

        [UxmlAttribute]
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

        public static string StateClassNameFor(string state) {
            return StateClassPrefix + HeaderStates.Normalize(state);
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
            var ordered = Run?.Report.Ordered();
            ShowState(Run == null ? HeaderStates.Neutral
                : ordered.Count == 0 ? HeaderStates.Passed
                : HeaderStates.For(ordered[0].Severity));

            if (Run == null) {
                return;
            }

            if (_compact) {
                BuildCompact(ordered);
            }
            else {
                BuildFull(ordered);
            }
        }

        protected virtual HeaderBar CreateBanner() {
            return new HeaderBar();
        }

        protected virtual Label CreateChecksLine(string text) {
            return new Label(text);
        }

        protected virtual Label CreateEmpty() {
            return new Label(EmptyText);
        }

        protected virtual VisualElement CreateRow(ValidationFinding finding) {
            return new VisualElement { tooltip = finding.ToText() };
        }

        protected virtual Label CreateText(string text, string className) {
            return new Label(text);
        }

        protected virtual VisualElement CreateActions(ValidationFinding finding) {
            return new VisualElement();
        }

        protected virtual Button CreateActionButton(ValidationFinding finding, FindingAction action) {
            return new Button(() => action.Run(finding)) { text = action.Label };
        }

        private void ShowState(string state) {
            RemoveFromClassList(StateClassNameFor(_state));
            _state = state;
            AddToClassList(StateClassNameFor(_state));
            Banner.State = _state;
        }

        private void BuildCompact(IReadOnlyList<ValidationFinding> ordered) {
            var title = Run.Summary();

            if (ordered.Count > 0) {
                var first = ordered[0];
                Banner.tooltip = first.ToText();
                title += ": " + first.Code + " " + first.Message;
            }
            else {
                Banner.tooltip = null;
            }

            Banner.Title = title;
            Add(Banner);
        }

        private void BuildFull(IReadOnlyList<ValidationFinding> ordered) {
            var checks = CreateChecksLine(Run.ChecksLine());
            checks.AddToClassList(ChecksClassName);
            Add(checks);

            if (ordered.Count == 0) {
                var empty = CreateEmpty();
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
            var row = CreateRow(finding);
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

            var container = CreateActions(finding);
            container.AddToClassList(ActionsClassName);

            foreach (var action in actions) {
                if (MaximumActions > 0 && container.childCount == MaximumActions) {
                    break;
                }

                if (action == null) {
                    continue;
                }

                var button = CreateActionButton(finding, action);
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

        private Label TextLabel(string text, string className) {
            var label = CreateText(text, className);
            label.AddToClassList(className);
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
