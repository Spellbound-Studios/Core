// Copyright (c) Spellbound Studios. All rights reserved.

using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Spellbound.Core.Window.Editor {
    public class SpellboundWindow : EditorWindow {
        public const string MenuPath = "Window/Spellbound";
        public const string WindowTitle = "Spellbound";

        [SerializeField] private string selectedPage;

        private WindowShell _shell;

        public static StyleSheet ConsumerStyleSheet { get; set; }

        public WindowShell Shell => _shell;
        public WindowPage CurrentPage => _shell?.CurrentPage;

        [MenuItem(MenuPath)]
        public static void ShowWindow() {
            Open(null);
        }

        public static SpellboundWindow Open(string pageTitle) {
            var window = GetWindow<SpellboundWindow>();
            window.titleContent = new GUIContent(WindowTitle);

            if (!string.IsNullOrWhiteSpace(pageTitle)) {
                window.Select(pageTitle);
            }

            window.Show();
            return window;
        }

        public bool Select(string pageTitle) {
            selectedPage = pageTitle;
            return _shell == null || _shell.Select(pageTitle);
        }

        protected virtual WindowShell CreateShell(WindowPages pages, string selectedTitle) {
            return new WindowShell(pages, selectedTitle, ConsumerStyleSheet);
        }

        private void CreateGUI() {
            _shell = CreateShell(WindowPages.Default, selectedPage);
            _shell.PageSelected += title => selectedPage = title;
            rootVisualElement.Add(_shell);
        }

        private void OnDisable() {
            _shell?.Close();
        }
    }
}
