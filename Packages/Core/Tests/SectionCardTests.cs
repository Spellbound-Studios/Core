// Copyright (c) Spellbound Studios. All rights reserved.

using NUnit.Framework;
using Spellbound.Core.Controls.Editor;
using UnityEngine.UIElements;

namespace Spellbound.Core.Tests {
    public class SectionCardTests {
        [Test]
        public void TheCardTheHeaderAndTheContentCarryTheirClasses() {
            var card = new SectionCard("General");
            Assert.IsTrue(card.ClassListContains(SectionCard.ClassName));
            Assert.IsTrue(card.Header.ClassListContains(SectionCard.HeaderClassName));
            Assert.IsTrue(card.Header.ClassListContains(HeaderBar.ClassName));
            Assert.IsTrue(card.Content.ClassListContains(SectionCard.ContentClassName));
        }

        [Test]
        public void TheCardCarriesTheControlStyleSheetAndAParameterlessConstructor() {
            var card = new SectionCard();
            Assert.AreEqual(string.Empty, card.Title);
            Assert.IsNotNull(ControlStyleSheet.Default);
            Assert.IsTrue(card.styleSheets.Contains(ControlStyleSheet.Default));
            Assert.IsTrue(card.Header.styleSheets.Contains(ControlStyleSheet.Default));
        }

        [Test]
        public void SectionCard_CustomImplementation() {
            var card = new NarrowCard("Custom");
            Assert.AreEqual("narrow-header", card.Header.name);
            Assert.AreEqual("narrow-content", card.Content.name);
            Assert.AreEqual("Custom", card.Title);
        }

        private sealed class NarrowCard : SectionCard {
            public NarrowCard(string title) : base(title) { }

            protected override HeaderBar CreateHeader(string title) {
                return new HeaderBar(title) { name = "narrow-header" };
            }

            protected override VisualElement CreateContent() {
                return new VisualElement { name = "narrow-content" };
            }
        }

        [Test]
        public void AddLandsInTheContentSlot() {
            var card = new SectionCard("General");
            var field = new TextField();
            card.Add(field);
            Assert.AreSame(card.Content, field.hierarchy.parent);
            Assert.AreEqual(1, card.Content.childCount);
            Assert.AreEqual(2, card.hierarchy.childCount);
        }

        [Test]
        public void TheHeaderIsAHeaderBarWithTheTitle() {
            var card = new SectionCard("General");
            Assert.IsInstanceOf<HeaderBar>(card.hierarchy[0]);
            Assert.AreEqual("General", card.Header.Title);
            card.Title = "Advanced";
            Assert.AreEqual("Advanced", card.Header.Title);
            Assert.AreEqual("Advanced", card.Title);
        }
    }
}
