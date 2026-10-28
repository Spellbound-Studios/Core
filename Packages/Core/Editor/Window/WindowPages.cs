// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Collections.Generic;
using Spellbound.Core.Logging;
using UnityEditor;

namespace Spellbound.Core.Window.Editor {
    public sealed class WindowPages {
        private static WindowPages _default;

        private readonly Func<IEnumerable<Type>> _typeSource;
        private List<WindowPageDescription> _pages;

        public static WindowPages Default => _default ??= new WindowPages(TypeCacheTypes);

        public WindowPages(Func<IEnumerable<Type>> typeSource) {
            _typeSource = typeSource ?? TypeCacheTypes;
        }

        public WindowPages(IEnumerable<WindowPageDescription> pages) {
            _typeSource = TypeCacheTypes;
            _pages = Kept(pages);
        }

        public IReadOnlyList<WindowPageDescription> Pages {
            get {
                EnsureLoaded();
                return _pages;
            }
        }

        public static IEnumerable<Type> TypeCacheTypes() {
            foreach (var type in TypeCache.GetTypesWithAttribute<WindowPageAttribute>()) {
                if (type.IsVisible) {
                    yield return type;
                }
            }
        }

        public static void Reset() {
            _default = null;
        }

        public WindowPageDescription Find(string title) {
            if (string.IsNullOrWhiteSpace(title)) {
                return null;
            }

            foreach (var page in Pages) {
                if (string.Equals(page.Title, title.Trim(), StringComparison.OrdinalIgnoreCase)) {
                    return page;
                }
            }

            return null;
        }

        private void EnsureLoaded() {
            if (_pages != null) {
                return;
            }

            var found = new List<WindowPageDescription>();

            foreach (var type in _typeSource()) {
                try {
                    var description = WindowPageDescription.From(type);

                    if (description != null) {
                        found.Add(description);
                    }
                }
                catch (Exception exception) {
                    Log.Error("The window page " + type?.FullName + " was not registered: " + exception.Message);
                }
            }

            _pages = Kept(found);
        }

        private static List<WindowPageDescription> Kept(IEnumerable<WindowPageDescription> pages) {
            var found = new List<WindowPageDescription>();

            if (pages != null) {
                foreach (var page in pages) {
                    if (page != null) {
                        found.Add(page);
                    }
                }
            }

            found.Sort((left, right) => {
                var byOrder = left.Order.CompareTo(right.Order);

                if (byOrder != 0) {
                    return byOrder;
                }

                var byTitle = string.CompareOrdinal(left.Title, right.Title);
                return byTitle != 0 ? byTitle : string.CompareOrdinal(left.PageType.FullName, right.PageType.FullName);
            });

            var kept = new List<WindowPageDescription>();

            foreach (var page in found) {
                var duplicate = FindIn(kept, page.Title);

                if (duplicate != null) {
                    Log.Error("Two window pages are titled '" + page.Title + "': " + duplicate.PageType.FullName
                              + " and " + page.PageType.FullName + "; the first is kept.");
                    continue;
                }

                kept.Add(page);
            }

            return kept;
        }

        private static WindowPageDescription FindIn(List<WindowPageDescription> pages, string title) {
            foreach (var page in pages) {
                if (string.Equals(page.Title, title, StringComparison.OrdinalIgnoreCase)) {
                    return page;
                }
            }

            return null;
        }
    }
}
