// Copyright (c) Spellbound Studios. All rights reserved.

using UnityEngine.UIElements;

namespace Spellbound.Core.Controls.Editor {
    [UxmlElement]
    public partial class SectionCard : VisualElement {
        public const string ClassName = "sb-section-card";
        public const string HeaderClassName = ClassName + "__header";
        public const string ContentClassName = ClassName + "__content";

        public SectionCard() : this(string.Empty) { }

        public SectionCard(string title) {
            AddToClassList(ClassName);
            ControlStyleSheet.Apply(this);
            Header = CreateHeader(title);
            Header.AddToClassList(HeaderClassName);
            hierarchy.Add(Header);
            Content = CreateContent();
            Content.AddToClassList(ContentClassName);
            hierarchy.Add(Content);
        }

        public HeaderBar Header { get; }
        public VisualElement Content { get; }

        public override VisualElement contentContainer => Content;

        [UxmlAttribute]
        public string Title {
            get => Header.Title;
            set => Header.Title = value;
        }

        protected virtual HeaderBar CreateHeader(string title) {
            return new HeaderBar(title);
        }

        protected virtual VisualElement CreateContent() {
            return new VisualElement();
        }
    }
}
