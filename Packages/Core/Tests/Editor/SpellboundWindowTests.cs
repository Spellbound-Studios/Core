// Copyright (c) Spellbound Studios. All rights reserved.

using NUnit.Framework;
using Spellbound.Core.Controls.Editor;
using Spellbound.Core.Window.Editor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Spellbound.Core.Tests {
    public class SpellboundWindowTests {
        private sealed class TaggedShell : WindowShell {
            public TaggedShell(WindowPages pages, string selectedTitle) : base(pages, selectedTitle) {
                name = "tagged-shell";
            }
        }

        private sealed class TaggedWindow : SpellboundWindow {
            protected override WindowShell CreateShell(WindowPages pages, string selectedTitle) {
                return new TaggedShell(pages, selectedTitle);
            }
        }

        [SetUp]
        public void SetUp() {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) {
                Assert.Ignore("A window needs a graphics device; run this in an editor with one.");
            }
        }

        [Test]
        public void SpellboundWindow_CustomImplementation() {
            var window = ScriptableObject.CreateInstance<TaggedWindow>();

            try {
                window.Show();
                Assert.IsInstanceOf<TaggedShell>(window.Shell);
                Assert.AreEqual("tagged-shell", window.Shell.name);
            }
            finally {
                window.Close();
            }
        }

        [Test]
        public void TheStockWindowDressesItsRootWithTheLook() {
            var window = ScriptableObject.CreateInstance<SpellboundWindow>();

            try {
                window.Show();
                Assert.IsTrue(EditorLook.IsDressed(window.rootVisualElement));
                Assert.IsTrue(window.rootVisualElement.ClassListContains(EditorLook.ThemeClassName));
                Assert.IsTrue(window.rootVisualElement.styleSheets.Contains(ControlStyleSheet.Default));
                Assert.AreSame(window.rootVisualElement, window.Shell.hierarchy.parent);
            }
            finally {
                window.Close();
            }
        }
    }
}
