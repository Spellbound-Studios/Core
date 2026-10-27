// Copyright (c) Spellbound Studios. All rights reserved.

using NUnit.Framework;
using Spellbound.Core.Controls.Editor;
using UnityEngine.UIElements;

namespace Spellbound.Core.Tests {
    public class SectionCardTests {
        [Test]
        public void TheCardTheHeaderAndTheContentCarryTheirClasses() {
            var card = new SectionCard("Hull");
            Assert.IsTrue(card.ClassListContains(SectionCard.ClassName));
            Assert.IsTrue(card.Header.ClassListContains(SectionCard.HeaderClassName));
            Assert.IsTrue(card.Header.ClassListContains(HeaderBar.ClassName));
            Assert.IsTrue(card.Content.ClassListContains(SectionCard.ContentClassName));
        }

        [Test]
        public void AddLandsInTheContentSlot() {
            var card = new SectionCard("Hull");
            var field = new TextField();
            card.Add(field);
            Assert.AreSame(card.Content, field.hierarchy.parent);
            Assert.AreEqual(1, card.Content.childCount);
            Assert.AreEqual(2, card.hierarchy.childCount);
        }

        [Test]
        public void TheHeaderIsAHeaderBarWithTheTitle() {
            var card = new SectionCard("Hull");
            Assert.IsInstanceOf<HeaderBar>(card.hierarchy[0]);
            Assert.AreEqual("Hull", card.Header.Title);
            card.Title = "Deck";
            Assert.AreEqual("Deck", card.Header.Title);
            Assert.AreEqual("Deck", card.Title);
        }
    }
}
