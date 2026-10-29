// Copyright (c) Spellbound Studios. All rights reserved.

using Spellbound.Core.Authoring;
using UnityEngine.UIElements;

namespace Spellbound.Core.Controls.Editor {
    [UxmlElement]
    public partial class HeaderBar : VisualElement {
        public const string ClassName = "sb-header";
        public const string TitleClassName = ClassName + "__title";
        public const string ActionsClassName = ClassName + "__actions";
        public const string ErrorClassName = ClassName + "--error";
        public const string WarningClassName = ClassName + "--warning";
        public const string InformationClassName = ClassName + "--information";

        private readonly Label _title;
        private FindingSeverity? _severity;

        public HeaderBar() : this(string.Empty) { }

        public HeaderBar(string title, FindingSeverity? severity = null) {
            AddToClassList(ClassName);
            ControlStyleSheet.Apply(this);
            _title = CreateTitle(title);
            _title.AddToClassList(TitleClassName);
            hierarchy.Add(_title);
            Actions = CreateActions();
            Actions.AddToClassList(ActionsClassName);
            hierarchy.Add(Actions);
            Severity = severity;
        }

        public VisualElement Actions { get; }

        public override VisualElement contentContainer => Actions;

        [UxmlAttribute]
        public string Title {
            get => _title.text;
            set => _title.text = value;
        }

        public FindingSeverity? Severity {
            get => _severity;
            set {
                _severity = value;
                RemoveFromClassList(ErrorClassName);
                RemoveFromClassList(WarningClassName);
                RemoveFromClassList(InformationClassName);

                if (value.HasValue) {
                    AddToClassList(ClassFor(value.Value));
                }
            }
        }

        protected virtual Label CreateTitle(string title) {
            return new Label(title);
        }

        protected virtual VisualElement CreateActions() {
            return new VisualElement();
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
