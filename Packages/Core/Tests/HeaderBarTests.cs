// Copyright (c) Spellbound Studios. All rights reserved.

using NUnit.Framework;
using Spellbound.Core.Controls.Editor;
using UnityEngine.UIElements;

namespace Spellbound.Core.Tests {
    public class HeaderBarTests {
        [Test]
        public void TheBarTheTextBlockTheTitleTheSentenceAndTheActionsCarryTheirClasses() {
            var header = new HeaderBar("Surface");
            Assert.IsTrue(header.ClassListContains(HeaderBar.ClassName));
            Assert.IsTrue(header.TextBlock.ClassListContains(HeaderBar.TextBlockClassName));
            Assert.IsTrue(header.Q<Label>().ClassListContains(HeaderBar.TitleClassName));
            Assert.IsNotNull(header.Q<Label>(className: HeaderBar.SentenceClassName));
            Assert.IsTrue(header.Actions.ClassListContains(HeaderBar.ActionsClassName));
            Assert.AreSame(header.TextBlock, header.Q<Label>(className: HeaderBar.TitleClassName).hierarchy.parent);
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
        public void TheSentenceShowsWhenSetAndHidesWhenEmpty() {
            var header = new HeaderBar("Check");
            var sentence = header.Q<Label>(className: HeaderBar.SentenceClassName);
            Assert.AreEqual(string.Empty, header.Sentence);
            Assert.AreEqual(DisplayStyle.None, sentence.style.display.value);
            header.Sentence = "Core: 3 must fix.";
            Assert.AreEqual("Core: 3 must fix.", sentence.text);
            Assert.AreEqual(DisplayStyle.Flex, sentence.style.display.value);
            header.Sentence = null;
            Assert.AreEqual(string.Empty, header.Sentence);
            Assert.AreEqual(DisplayStyle.None, sentence.style.display.value);
        }

        [Test]
        public void AStateCarriesExactlyOneStateClassAndStartsNeutral() {
            var header = new HeaderBar("Report");
            Assert.AreEqual(HeaderStates.Neutral, header.State);
            AssertOnlyState(header, HeaderStates.Neutral);
            header.State = HeaderStates.Error;
            AssertOnlyState(header, HeaderStates.Error);
            header.State = HeaderStates.Passed;
            AssertOnlyState(header, HeaderStates.Passed);
            header.State = null;
            Assert.AreEqual(HeaderStates.Neutral, header.State);
            AssertOnlyState(header, HeaderStates.Neutral);
        }

        [Test]
        public void TheConstructorTakesAStateAndAConsumerInventsItsOwn() {
            var header = new HeaderBar("Table", HeaderStates.Warning);
            AssertOnlyState(header, HeaderStates.Warning);
            header.State = "in-sync";
            Assert.AreEqual("in-sync", header.State);
            Assert.IsTrue(header.ClassListContains("sb-header--in-sync"));
            Assert.IsFalse(header.ClassListContains(HeaderStates.ClassNameFor(HeaderStates.Warning)));
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
            Assert.AreEqual("tagged-text", header.TextBlock.name);
            Assert.AreEqual("tagged-title", header.Q<Label>(className: HeaderBar.TitleClassName).name);
            Assert.AreEqual("tagged-sentence", header.Q<Label>(className: HeaderBar.SentenceClassName).name);
            Assert.AreEqual("tagged-actions", header.Actions.name);
            Assert.AreEqual("Tagged", header.Title);
            Assert.IsTrue(header.styleSheets.Contains(ControlStyleSheet.Default));
        }

        private sealed class TaggedHeaderBar : HeaderBar {
            public TaggedHeaderBar(string title) : base(title) { }

            protected override VisualElement CreateTextBlock() => new VisualElement { name = "tagged-text" };

            protected override Label CreateTitle(string title) => new Label(title) { name = "tagged-title" };

            protected override Label CreateSentence() => new Label { name = "tagged-sentence" };

            protected override VisualElement CreateActions() => new VisualElement { name = "tagged-actions" };
        }

        private static void AssertOnlyState(HeaderBar header, string state) {
            Assert.AreEqual(state, header.State);

            foreach (var shipped in HeaderStates.Shipped) {
                Assert.AreEqual(shipped == state, header.ClassListContains(HeaderStates.ClassNameFor(shipped)),
                    shipped);
            }
        }
    }
}
