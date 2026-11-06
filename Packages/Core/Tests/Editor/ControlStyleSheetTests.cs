// Copyright (c) Spellbound Studios. All rights reserved.

using NUnit.Framework;
using Spellbound.Core.Controls.Editor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Spellbound.Core.Tests {
    public class ControlStyleSheetTests {
        [TearDown]
        public void TearDown() {
            ControlStyleSheet.Source = ControlStyleSheet.LoadShipped;
        }

        [Test]
        public void TheShippedStyleSheetIsFoundAndCached() {
            Assert.IsNotNull(ControlStyleSheet.Default);
            Assert.AreSame(ControlStyleSheet.Default, ControlStyleSheet.Default);
            Assert.AreSame(ControlStyleSheet.Default, ControlStyleSheet.LoadShipped());
        }

        [Test]
        public void ApplyAddsTheSheetOnce() {
            var element = new VisualElement();
            ControlStyleSheet.Apply(element);
            ControlStyleSheet.Apply(element);
            Assert.AreEqual(1, element.styleSheets.count);
            Assert.IsTrue(element.styleSheets.Contains(ControlStyleSheet.Default));
        }

        [Test]
        public void Source_CustomImplementation() {
            var custom = ScriptableObject.CreateInstance<StyleSheet>();

            try {
                ControlStyleSheet.Source = () => custom;
                Assert.AreSame(custom, ControlStyleSheet.Default);
                var header = new HeaderBar("Custom");
                Assert.IsTrue(header.styleSheets.Contains(custom));
                var shell = new WindowShellProbe();
                Assert.IsTrue(shell.styleSheets.Contains(custom));
            }
            finally {
                Object.DestroyImmediate(custom);
            }
        }

        private sealed class WindowShellProbe : Window.Editor.WindowShell {
            public WindowShellProbe()
                : base(new Window.Editor.WindowPages(System.Array.Empty<Window.Editor.WindowPageDescription>())) { }
        }
    }
}
