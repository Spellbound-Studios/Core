// Copyright (c) Spellbound Studios. All rights reserved.

using System;

namespace Spellbound.Core.Window.Editor {
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class WindowPageAttribute : Attribute {
        public string Title { get; }
        public int Order { get; }

        public WindowPageAttribute(string title, int order = 0) {
            if (string.IsNullOrWhiteSpace(title)) {
                throw new ArgumentException("A window page needs a title.", nameof(title));
            }

            Title = title.Trim();
            Order = order;
        }
    }
}
