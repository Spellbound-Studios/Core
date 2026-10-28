// Copyright (c) Spellbound Studios. All rights reserved.

using UnityEngine.UIElements;

namespace Spellbound.Core.Window.Editor {
    public abstract class WindowPage {
        public string Title { get; internal set; } = string.Empty;

        public abstract VisualElement CreateContent();

        public virtual void Shown() { }

        public virtual void Hidden() { }
    }
}
