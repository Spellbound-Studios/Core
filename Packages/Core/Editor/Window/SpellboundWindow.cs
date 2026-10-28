// Copyright (c) Spellbound Studios. All rights reserved.

using UnityEditor;
using UnityEngine;

namespace Spellbound.Core.Window.Editor {
    public sealed class SpellboundWindow : EditorWindow {
        public const string MenuPath = "Window/Spellbound";
        public const string WindowTitle = "Spellbound";

        [SerializeField] private string selectedPage;

        private WindowShell _shell;

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

        private void CreateGUI() {
            _shell = new WindowShell(WindowPages.Default, selectedPage);
            _shell.PageSelected += title => selectedPage = title;
            rootVisualElement.Add(_shell);
        }

        private void OnDisable() {
            _shell?.Close();
        }
    }
}
