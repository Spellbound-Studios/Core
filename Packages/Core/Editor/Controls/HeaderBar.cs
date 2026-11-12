// Copyright (c) Spellbound Studios. All rights reserved.

using UnityEngine.UIElements;

namespace Spellbound.Core.Controls.Editor {
    [UxmlElement]
    public partial class HeaderBar : VisualElement {
        public const string ClassName = "sb-header";
        public const string TextBlockClassName = ClassName + "__text";
        public const string TitleClassName = ClassName + "__title";
        public const string SentenceClassName = ClassName + "__sentence";
        public const string ActionsClassName = ClassName + "__actions";

        private readonly Label _title;
        private readonly Label _sentence;
        private string _state = HeaderStates.Neutral;

        public HeaderBar() : this(string.Empty) { }

        public HeaderBar(string title, string state = null) {
            AddToClassList(ClassName);
            ControlStyleSheet.Apply(this);
            TextBlock = CreateTextBlock();
            TextBlock.AddToClassList(TextBlockClassName);
            hierarchy.Add(TextBlock);
            _title = CreateTitle(title);
            _title.AddToClassList(TitleClassName);
            TextBlock.Add(_title);
            _sentence = CreateSentence();
            _sentence.AddToClassList(SentenceClassName);
            TextBlock.Add(_sentence);
            Actions = CreateActions();
            Actions.AddToClassList(ActionsClassName);
            hierarchy.Add(Actions);
            Sentence = string.Empty;
            State = state;
        }

        public VisualElement TextBlock { get; }
        public VisualElement Actions { get; }

        public override VisualElement contentContainer => Actions;

        [UxmlAttribute]
        public string Title {
            get => _title.text;
            set => _title.text = value;
        }

        [UxmlAttribute]
        public string Sentence {
            get => _sentence.text;
            set {
                _sentence.text = value ?? string.Empty;
                _sentence.style.display = string.IsNullOrEmpty(_sentence.text) ? DisplayStyle.None : DisplayStyle.Flex;
            }
        }

        [UxmlAttribute]
        public string State {
            get => _state;
            set {
                var state = HeaderStates.Normalize(value);
                RemoveFromClassList(HeaderStates.ClassNameFor(_state));
                _state = state;
                AddToClassList(HeaderStates.ClassNameFor(_state));
            }
        }

        protected virtual VisualElement CreateTextBlock() {
            return new VisualElement();
        }

        protected virtual Label CreateTitle(string title) {
            return new Label(title);
        }

        protected virtual Label CreateSentence() {
            return new Label();
        }

        protected virtual VisualElement CreateActions() {
            return new VisualElement();
        }
    }
}
