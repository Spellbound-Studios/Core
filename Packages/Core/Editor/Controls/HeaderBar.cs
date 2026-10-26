// Copyright (c) Spellbound Studios. All rights reserved.

using Spellbound.Core.Authoring;
using UnityEngine.UIElements;

namespace Spellbound.Core.Controls.Editor {
    public class HeaderBar : VisualElement {
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
            style.flexDirection = FlexDirection.Row;
            style.justifyContent = Justify.SpaceBetween;
            style.alignItems = Align.Center;
            _title = new Label(title);
            _title.AddToClassList(TitleClassName);
            _title.style.flexShrink = 1;
            _title.style.whiteSpace = WhiteSpace.Normal;
            hierarchy.Add(_title);
            Actions = new VisualElement();
            Actions.AddToClassList(ActionsClassName);
            Actions.style.flexDirection = FlexDirection.Row;
            Actions.style.alignItems = Align.Center;
            hierarchy.Add(Actions);
            Severity = severity;
        }

        public VisualElement Actions { get; }

        public override VisualElement contentContainer => Actions;

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
