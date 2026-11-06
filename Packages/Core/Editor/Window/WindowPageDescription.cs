// Copyright (c) Spellbound Studios. All rights reserved.

using System;

namespace Spellbound.Core.Window.Editor {
    public sealed class WindowPageDescription {
        public Type PageType { get; }
        public string Title { get; }
        public int Order { get; }

        public WindowPageDescription(Type pageType, string title, int order) {
            if (pageType == null || !typeof(WindowPage).IsAssignableFrom(pageType) || pageType.IsAbstract) {
                throw new ArgumentException("A window page is a concrete class deriving from WindowPage.",
                    nameof(pageType));
            }

            if (pageType.GetConstructor(Type.EmptyTypes) == null) {
                throw new ArgumentException(pageType.FullName + " needs a public constructor with no parameters.",
                    nameof(pageType));
            }

            if (string.IsNullOrWhiteSpace(title)) {
                throw new ArgumentException("A window page needs a title.", nameof(title));
            }

            PageType = pageType;
            Title = title.Trim();
            Order = order;
        }

        public static WindowPageDescription From(Type pageType) {
            if (pageType == null) {
                return null;
            }

            var attribute =
                (WindowPageAttribute)Attribute.GetCustomAttribute(pageType, typeof(WindowPageAttribute), false);
            return attribute == null ? null : new WindowPageDescription(pageType, attribute.Title, attribute.Order);
        }

        public WindowPage Create() {
            var page = (WindowPage)Activator.CreateInstance(PageType);
            page.Title = Title;
            return page;
        }

        public override string ToString() {
            return Title + " (" + PageType.FullName + ")";
        }
    }
}
