// Copyright (c) Spellbound Studios. All rights reserved.

using NUnit.Framework;
using Spellbound.Core.Window.Editor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace Spellbound.Core.Tests {
    public class SpellboundWindowTests {
        private sealed class TaggedShell : WindowShell {
            public TaggedShell(WindowPages pages, string selectedTitle, StyleSheet styleSheet)
                : base(pages, selectedTitle, styleSheet) {
                name = "tagged-shell";
            }
        }

        private sealed class TaggedWindow : SpellboundWindow {
            protected override WindowShell CreateShell(WindowPages pages, string selectedTitle) {
                return new TaggedShell(pages, selectedTitle, ConsumerStyleSheet);
            }
        }

        private StyleSheet _sheet;

        [SetUp]
        public void SetUp() {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) {
                Assert.Ignore("A window needs a graphics device; run this in an editor with one.");
            }

            _sheet = ScriptableObject.CreateInstance<StyleSheet>();
            SpellboundWindow.ConsumerStyleSheet = _sheet;
        }

        [TearDown]
        public void TearDown() {
            SpellboundWindow.ConsumerStyleSheet = null;

            if (_sheet != null) {
                Object.DestroyImmediate(_sheet);
            }
        }

        [Test]
        public void SpellboundWindow_CustomImplementation() {
            var window = ScriptableObject.CreateInstance<TaggedWindow>();

            try {
                window.Show();
                Assert.IsInstanceOf<TaggedShell>(window.Shell);
                Assert.AreEqual("tagged-shell", window.Shell.name);
                Assert.IsTrue(window.Shell.styleSheets.Contains(_sheet));
            }
            finally {
                window.Close();
            }
        }

        [Test]
        public void TheStockWindowAppliesTheConsumerStyleSheet() {
            var window = ScriptableObject.CreateInstance<SpellboundWindow>();

            try {
                window.Show();
                Assert.IsTrue(window.Shell.styleSheets.Contains(_sheet));
            }
            finally {
                window.Close();
            }
        }
    }
}
