// Copyright (c) Spellbound Studios. All rights reserved.

using NUnit.Framework;
using Spellbound.Core.Authoring;
using Spellbound.Core.Controls.Editor;
using UnityEngine.UIElements;

namespace Spellbound.Core.Tests {
    public class HeaderBarTests {
        [Test]
        public void TheBarTheTitleAndTheActionsCarryTheirClasses() {
            var header = new HeaderBar("Surface");
            Assert.IsTrue(header.ClassListContains(HeaderBar.ClassName));
            Assert.IsTrue(header.Q<Label>().ClassListContains(HeaderBar.TitleClassName));
            Assert.IsTrue(header.Actions.ClassListContains(HeaderBar.ActionsClassName));
        }

        [Test]
        public void TheTitleIsSetAndRead() {
            var header = new HeaderBar();
            Assert.AreEqual(string.Empty, header.Title);
            header.Title = "Validation";
            Assert.AreEqual("Validation", header.Title);
            Assert.AreEqual("Validation", header.Q<Label>(className: HeaderBar.TitleClassName).text);
        }

        [Test]
        public void ASeverityCarriesExactlyOneModifierClassAndNullCarriesNone() {
            var header = new HeaderBar("Report", FindingSeverity.Error);
            Assert.AreEqual(FindingSeverity.Error, header.Severity);
            AssertModifiers(header, true, false, false);
            header.Severity = FindingSeverity.Warning;
            AssertModifiers(header, false, true, false);
            header.Severity = FindingSeverity.Information;
            AssertModifiers(header, false, false, true);
            header.Severity = null;
            Assert.IsNull(header.Severity);
            AssertModifiers(header, false, false, false);
        }

        [Test]
        public void AddLandsInTheActionsSlot() {
            var header = new HeaderBar("Surface");
            var button = new Button { text = "Remove" };
            header.Add(button);
            Assert.AreSame(header.Actions, button.hierarchy.parent);
            Assert.AreEqual(1, header.Actions.childCount);
            Assert.AreEqual(2, header.hierarchy.childCount);
        }

        [Test]
        public void HeaderBar_CustomImplementation() {
            var header = new TaggedHeaderBar("Tagged");
            Assert.AreEqual("tagged-title", header.Q<Label>(className: HeaderBar.TitleClassName).name);
            Assert.AreEqual("tagged-actions", header.Actions.name);
            Assert.AreEqual("Tagged", header.Title);
            Assert.IsTrue(header.styleSheets.Contains(ControlStyleSheet.Default));
        }

        private sealed class TaggedHeaderBar : HeaderBar {
            public TaggedHeaderBar(string title) : base(title) { }

            protected override Label CreateTitle(string title) => new Label(title) { name = "tagged-title" };

            protected override VisualElement CreateActions() => new VisualElement { name = "tagged-actions" };
        }

        private static void AssertModifiers(HeaderBar header, bool error, bool warning, bool information) {
            Assert.AreEqual(error, header.ClassListContains(HeaderBar.ErrorClassName));
            Assert.AreEqual(warning, header.ClassListContains(HeaderBar.WarningClassName));
            Assert.AreEqual(information, header.ClassListContains(HeaderBar.InformationClassName));
        }
    }
}
