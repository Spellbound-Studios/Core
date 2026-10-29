// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Collections.Generic;
using Spellbound.Core.Controls.Editor;
using Spellbound.Core.Logging;
using UnityEngine.UIElements;

namespace Spellbound.Core.Window.Editor {
    public class WindowShell : VisualElement {
        public const string ClassName = "sb-window";
        public const string PagesClassName = ClassName + "__pages";
        public const string PageClassName = ClassName + "__page";
        public const string SelectedPageClassName = PageClassName + "--selected";
        public const string BodyClassName = ClassName + "__body";
        public const string HeaderClassName = ClassName + "__header";
        public const string PageControlsClassName = ClassName + "__page-controls";
        public const string ContentClassName = ClassName + "__content";
        public const string EmptyClassName = ClassName + "__empty";
        public const string FailureClassName = ClassName + "__failure";
        public const string EmptyText = "No pages are installed in this project.";

        private readonly WindowPages _pages;
        private readonly Dictionary<string, WindowPage> _created = new Dictionary<string, WindowPage>();
        private readonly Dictionary<string, Button> _buttons = new Dictionary<string, Button>();

        public WindowShell(WindowPages pages, string selectedTitle = null, StyleSheet styleSheet = null) {
            _pages = pages ?? WindowPages.Default;
            AddToClassList(ClassName);
            ControlStyleSheet.Apply(this);

            if (styleSheet != null) {
                styleSheets.Add(styleSheet);
            }

            PageList = CreatePageList();
            PageList.AddToClassList(PagesClassName);
            Add(PageList);
            Body = CreateBody();
            Body.AddToClassList(BodyClassName);
            Add(Body);
            Header = CreateHeader();
            Header.AddToClassList(HeaderClassName);
            Body.Add(Header);
            PageControls = CreatePageControls();
            PageControls.AddToClassList(PageControlsClassName);
            Header.Add(PageControls);
            Content = CreateContent();
            Content.AddToClassList(ContentClassName);
            Body.Add(Content);
            BuildPageList();

            if (!Select(selectedTitle) && _pages.Pages.Count > 0) {
                Select(_pages.Pages[0].Title);
            }
        }

        public event Action<string> PageSelected;

        public VisualElement PageList { get; }
        public VisualElement Body { get; }
        public HeaderBar Header { get; }
        public VisualElement PageControls { get; }
        public VisualElement Content { get; }
        public string SelectedTitle { get; private set; }
        public WindowPage CurrentPage { get; private set; }
        public IReadOnlyList<WindowPageDescription> Pages => _pages.Pages;

        public bool Select(string title) {
            var description = _pages.Find(title);

            if (description == null) {
                return false;
            }

            if (CurrentPage != null && string.Equals(SelectedTitle, description.Title, StringComparison.Ordinal)) {
                return true;
            }

            Notify(CurrentPage, false);
            Content.Clear();
            PageControls.Clear();
            SelectedTitle = description.Title;
            Header.Title = description.Title;
            CurrentPage = PageFor(description);
            MarkSelected();

            if (CurrentPage != null) {
                Content.Add(ContentOf(CurrentPage));
                var controls = HeaderControlsOf(CurrentPage);

                if (controls != null) {
                    PageControls.Add(controls);
                }

                Notify(CurrentPage, true);
            }

            PageSelected?.Invoke(SelectedTitle);
            return true;
        }

        public void Close() {
            Notify(CurrentPage, false);
        }

        protected virtual VisualElement CreatePageList() {
            return new VisualElement();
        }

        protected virtual VisualElement CreateBody() {
            return new VisualElement();
        }

        protected virtual HeaderBar CreateHeader() {
            return new HeaderBar();
        }

        protected virtual VisualElement CreateContent() {
            return new VisualElement();
        }

        protected virtual VisualElement CreatePageControls() {
            return new VisualElement();
        }

        protected virtual Button CreatePageButton(WindowPageDescription page) {
            var title = page.Title;
            return new Button(() => Select(title)) { text = title };
        }

        protected virtual Label CreateEmpty() {
            return new Label(EmptyText);
        }

        protected virtual Label CreateFailure(string text) {
            return new Label(text);
        }

        private void BuildPageList() {
            if (_pages.Pages.Count == 0) {
                var empty = CreateEmpty();
                empty.AddToClassList(EmptyClassName);
                Content.Add(empty);
                return;
            }

            foreach (var page in _pages.Pages) {
                var button = CreatePageButton(page);
                button.AddToClassList(PageClassName);
                _buttons[page.Title] = button;
                PageList.Add(button);
            }
        }

        private void MarkSelected() {
            foreach (var pair in _buttons) {
                pair.Value.EnableInClassList(SelectedPageClassName,
                    string.Equals(pair.Key, SelectedTitle, StringComparison.Ordinal));
            }
        }

        private WindowPage PageFor(WindowPageDescription description) {
            if (_created.TryGetValue(description.Title, out var existing)) {
                return existing;
            }

            try {
                var page = description.Create();
                _created[description.Title] = page;
                return page;
            }
            catch (Exception exception) {
                Log.Error("The window page " + description + " could not be created: " + exception.Message);
                Content.Add(Failure(description.Title + " could not be created: " + exception.Message));
                return null;
            }
        }

        private static void Notify(WindowPage page, bool shown) {
            if (page == null) {
                return;
            }

            try {
                if (shown) {
                    page.Shown();
                }
                else {
                    page.Hidden();
                }
            }
            catch (Exception exception) {
                Log.Error("The window page " + page.Title + " failed when " + (shown ? "shown" : "hidden") + ": "
                          + exception.Message);
            }
        }

        private VisualElement ContentOf(WindowPage page) {
            try {
                return page.CreateContent() ?? new VisualElement();
            }
            catch (Exception exception) {
                Log.Error("The window page " + page.Title + " could not draw: " + exception.Message);
                return Failure(page.Title + " could not draw: " + exception.Message);
            }
        }

        private VisualElement HeaderControlsOf(WindowPage page) {
            try {
                return page.CreateHeaderControls();
            }
            catch (Exception exception) {
                Log.Error("The window page " + page.Title + " could not draw its header controls: "
                          + exception.Message);
                return null;
            }
        }

        private Label Failure(string text) {
            var label = CreateFailure(text);
            label.AddToClassList(FailureClassName);
            return label;
        }
    }
}
