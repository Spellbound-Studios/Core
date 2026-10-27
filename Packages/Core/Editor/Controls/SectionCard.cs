// Copyright (c) Spellbound Studios. All rights reserved.

using UnityEngine.UIElements;

namespace Spellbound.Core.Controls.Editor {
    public class SectionCard : VisualElement {
        public const string ClassName = "sb-section-card";
        public const string HeaderClassName = ClassName + "__header";
        public const string ContentClassName = ClassName + "__content";

        public SectionCard(string title) {
            AddToClassList(ClassName);
            Header = new HeaderBar(title);
            Header.AddToClassList(HeaderClassName);
            hierarchy.Add(Header);
            Content = new VisualElement();
            Content.AddToClassList(ContentClassName);
            hierarchy.Add(Content);
        }

        public HeaderBar Header { get; }
        public VisualElement Content { get; }

        public override VisualElement contentContainer => Content;

        public string Title {
            get => Header.Title;
            set => Header.Title = value;
        }
    }
}
