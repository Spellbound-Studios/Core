// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Spellbound.Core.Controls.Editor;
using Spellbound.Core.Logging;
using Spellbound.Core.Window.Editor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Spellbound.Core.Tests {
    public class WindowShellTests {
        private const string ContentName = "page-content";

        private sealed class CountingPage : WindowPage {
            public static int Created;

            public CountingPage() {
                Created++;
            }

            public int ShownCount { get; private set; }
            public int HiddenCount { get; private set; }

            public override VisualElement CreateContent() {
                return new Label(Title) { name = ContentName };
            }

            public override void Shown() {
                ShownCount++;
            }

            public override void Hidden() {
                HiddenCount++;
            }
        }

        private sealed class DrawingFailurePage : WindowPage {
            public override VisualElement CreateContent() {
                throw new InvalidOperationException("no content today");
            }
        }

        private sealed class SwitchFailurePage : WindowPage {
            public override VisualElement CreateContent() {
                return new Label(Title) { name = ContentName };
            }

            public override void Shown() {
                throw new InvalidOperationException("cannot be shown");
            }

            public override void Hidden() {
                throw new InvalidOperationException("cannot be hidden");
            }
        }

        private sealed class CreationFailurePage : WindowPage {
            public CreationFailurePage() {
                throw new InvalidOperationException("no page today");
            }

            public override VisualElement CreateContent() {
                return new VisualElement();
            }
        }

        private RecordingLogSink _recording;
        private Log.SinkSuspension _suspension;

        [SetUp]
        public void SetUp() {
            CountingPage.Created = 0;
            _suspension = Log.SuspendSinks();
            _recording = new RecordingLogSink(LogLevel.Verbose);
            Log.AddSink(_recording, null, LogLevel.Verbose);
        }

        [TearDown]
        public void TearDown() {
            Log.RemoveSink(_recording);
            _suspension.Dispose();
        }

        [Test]
        public void EachPageGetsOneButtonInOrderWithThePageClass() {
            var shell = new WindowShell(ThreePages());
            Assert.IsTrue(shell.ClassListContains(WindowShell.ClassName));
            var buttons = shell.PageList.Query<Button>().ToList();
            CollectionAssert.AreEqual(new[] { "Alpha", "Beta", "Gamma" }, buttons.Select(button => button.text));
            Assert.AreEqual(3, shell.PageList.childCount);

            foreach (var button in buttons) {
                Assert.IsTrue(button.ClassListContains(WindowShell.PageClassName), button.text);
            }
        }

        [Test]
        public void TheFirstPageIsSelectedByDefault() {
            var shell = new WindowShell(ThreePages());
            Assert.AreEqual("Alpha", shell.SelectedTitle);
            Assert.AreEqual("Alpha", shell.Header.Title);
            Assert.AreEqual("Alpha", shell.CurrentPage.Title);
            Assert.AreEqual("Alpha", shell.Content.Q<Label>(ContentName).text);
            CollectionAssert.AreEqual(new[] { "Alpha" }, SelectedButtons(shell));
        }

        [Test]
        public void ANamedPageIsSelectedFirstAndAnUnknownNameFallsBackToTheFirstPage() {
            Assert.AreEqual("Gamma", new WindowShell(ThreePages(), "gamma").SelectedTitle);
            Assert.AreEqual("Alpha", new WindowShell(ThreePages(), "Missing").SelectedTitle);
        }

        [Test]
        public void SelectChangesTheHeaderTitleTheContentAndTheMarkedButton() {
            var shell = new WindowShell(ThreePages());
            Assert.IsTrue(shell.Select("Beta"));
            Assert.AreEqual("Beta", shell.SelectedTitle);
            Assert.AreEqual("Beta", shell.Header.Title);
            Assert.AreEqual("Beta", shell.Content.Q<Label>(ContentName).text);
            Assert.AreEqual(1, shell.Content.childCount);
            CollectionAssert.AreEqual(new[] { "Beta" }, SelectedButtons(shell));
        }

        [Test]
        public void SelectRaisesPageSelectedOnce() {
            var shell = new WindowShell(ThreePages());
            var selected = new List<string>();
            shell.PageSelected += selected.Add;
            shell.Select("Beta");
            shell.Select("Beta");
            CollectionAssert.AreEqual(new[] { "Beta" }, selected);
        }

        [Test]
        public void APageIsCreatedOnceAndHearsShownAndHidden() {
            var shell = new WindowShell(ThreePages());
            var alpha = (CountingPage)shell.CurrentPage;
            Assert.AreEqual(1, CountingPage.Created);
            Assert.AreEqual(1, alpha.ShownCount);
            Assert.AreEqual(0, alpha.HiddenCount);
            shell.Select("Beta");
            var beta = (CountingPage)shell.CurrentPage;
            Assert.AreEqual(2, CountingPage.Created);
            Assert.AreEqual(1, alpha.HiddenCount);
            Assert.AreEqual(1, beta.ShownCount);
            shell.Select("Alpha");
            Assert.AreSame(alpha, shell.CurrentPage);
            Assert.AreEqual(2, CountingPage.Created);
            Assert.AreEqual(2, alpha.ShownCount);
            Assert.AreEqual(1, beta.HiddenCount);
            shell.Close();
            Assert.AreEqual(2, alpha.HiddenCount);
        }

        [Test]
        public void AnUnknownTitleIsRefusedAndChangesNothing() {
            var shell = new WindowShell(ThreePages());
            var selected = new List<string>();
            shell.PageSelected += selected.Add;
            Assert.IsFalse(shell.Select("Missing"));
            Assert.IsFalse(shell.Select(null));
            Assert.AreEqual("Alpha", shell.SelectedTitle);
            CollectionAssert.IsEmpty(selected);
        }

        [Test]
        public void NoPagesShowsTheEmptyState() {
            var shell = new WindowShell(new WindowPages(Array.Empty<WindowPageDescription>()));
            var empty = shell.Content.Q<Label>(className: WindowShell.EmptyClassName);
            Assert.IsNotNull(empty);
            Assert.AreEqual(WindowShell.EmptyText, empty.text);
            Assert.AreEqual(0, shell.PageList.childCount);
            Assert.IsNull(shell.SelectedTitle);
            Assert.IsNull(shell.CurrentPage);
        }

        [Test]
        public void APageWhoseContentThrowsShowsTheFailure() {
            var shell = new WindowShell(new WindowPages(new[] {
                new WindowPageDescription(typeof(DrawingFailurePage), "Broken", 0)
            }));

            var failure = shell.Content.Q<Label>(className: WindowShell.FailureClassName);
            Assert.IsNotNull(failure);
            Assert.AreEqual("Broken could not draw: no content today", failure.text);
            Assert.AreEqual("Broken", shell.SelectedTitle);
            Assert.AreEqual(1, _recording.CountOf(LogLevel.Error));
        }

        [Test]
        public void APageThatCannotBeCreatedShowsTheFailure() {
            var shell = new WindowShell(new WindowPages(new[] {
                new WindowPageDescription(typeof(CreationFailurePage), "Unbuilt", 0)
            }));

            var failure = shell.Content.Q<Label>(className: WindowShell.FailureClassName);
            Assert.IsNotNull(failure);
            StringAssert.StartsWith("Unbuilt could not be created: ", failure.text);
            Assert.IsNull(shell.CurrentPage);
            Assert.AreEqual(1, _recording.CountOf(LogLevel.Error));
        }

        [Test]
        public void APageThatThrowsWhenShownOrHiddenCanStillBeLeftAndIsRemembered() {
            var shell = new WindowShell(new WindowPages(new[] {
                new WindowPageDescription(typeof(SwitchFailurePage), "Stubborn", 0),
                new WindowPageDescription(typeof(CountingPage), "Calm", 1)
            }));

            var selected = new List<string>();
            shell.PageSelected += selected.Add;
            Assert.AreEqual("Stubborn", shell.SelectedTitle);
            Assert.IsTrue(shell.Select("Calm"));
            Assert.AreEqual("Calm", shell.SelectedTitle);
            Assert.AreEqual("Calm", shell.Content.Q<Label>(ContentName).text);
            Assert.IsTrue(shell.Select("Stubborn"));
            CollectionAssert.AreEqual(new[] { "Calm", "Stubborn" }, selected);
            shell.Close();
            Assert.AreEqual(4, _recording.CountOf(LogLevel.Error));
        }

        [Test]
        public void APageDrawsItsControlsInTheHeaderSlotUntilAnotherPageIsShown() {
            var shell = new WindowShell(new WindowPages(new[] {
                new WindowPageDescription(typeof(ControlledPage), "Controlled", 0),
                new WindowPageDescription(typeof(CountingPage), "Plain", 1)
            }));

            Assert.AreSame(shell.Header.Actions, shell.PageControls.hierarchy.parent);
            Assert.IsTrue(shell.PageControls.ClassListContains(WindowShell.PageControlsClassName));
            var own = new Button { name = "own-control" };
            shell.Header.Add(own);
            Assert.IsNotNull(shell.PageControls.Q<Button>(ControlledPage.ControlName));
            shell.Select("Plain");
            Assert.AreEqual(0, shell.PageControls.childCount);
            Assert.AreSame(shell.Header.Actions, own.hierarchy.parent);
            shell.Select("Controlled");
            Assert.IsNotNull(shell.PageControls.Q<Button>(ControlledPage.ControlName));
        }

        [Test]
        public void TheShellCarriesTheLayoutSheetAndAnEmptyFooter() {
            var shell = new WindowShell(ThreePages());
            Assert.IsTrue(shell.styleSheets.Contains(ControlStyleSheet.Default));
            Assert.IsTrue(shell.Footer.ClassListContains(WindowShell.FooterClassName));
            Assert.AreSame(shell.Body, shell.Footer.hierarchy.parent);
            Assert.AreEqual(string.Empty, shell.Footer.text);
            Assert.AreEqual(DisplayStyle.None, shell.Footer.style.display.value);
            Assert.AreEqual(HeaderStates.Neutral, shell.Header.State);
            Assert.AreEqual(string.Empty, shell.Header.Sentence);
        }

        [Test]
        public void APageSetsTheHeaderSentenceTheStateAndTheFooterWhileItIsShown() {
            var shell = new WindowShell(new WindowPages(new[] {
                new WindowPageDescription(typeof(SurfacePage), "Surface", 0),
                new WindowPageDescription(typeof(CountingPage), "Plain", 1)
            }));

            var page = (SurfacePage)shell.CurrentPage;
            Assert.AreEqual("three rules to confirm", shell.Header.Sentence);
            Assert.AreEqual(HeaderStates.Warning, shell.Header.State);
            Assert.AreEqual("compiled: current", shell.Footer.text);
            Assert.AreEqual(DisplayStyle.Flex, shell.Footer.style.display.value);
            page.HeaderSentence = "all passed";
            page.HeaderState = HeaderStates.Passed;
            page.FooterText = string.Empty;
            Assert.AreEqual("all passed", shell.Header.Sentence);
            Assert.AreEqual(HeaderStates.Passed, shell.Header.State);
            Assert.AreEqual(DisplayStyle.None, shell.Footer.style.display.value);
            Assert.AreEqual("Surface", shell.Header.Title);
        }

        [Test]
        public void AnotherPageClearsTheSurfaceAHiddenPageDoesNotReachItAndAReturnRestoresIt() {
            var shell = new WindowShell(new WindowPages(new[] {
                new WindowPageDescription(typeof(SurfacePage), "Surface", 0),
                new WindowPageDescription(typeof(CountingPage), "Plain", 1)
            }));

            var page = (SurfacePage)shell.CurrentPage;
            shell.Select("Plain");
            Assert.AreEqual(string.Empty, shell.Header.Sentence);
            Assert.AreEqual(HeaderStates.Neutral, shell.Header.State);
            Assert.AreEqual(DisplayStyle.None, shell.Footer.style.display.value);
            page.HeaderSentence = "changed while hidden";
            page.HeaderState = HeaderStates.Error;
            page.FooterText = "stale";
            Assert.AreEqual(string.Empty, shell.Header.Sentence);
            Assert.AreEqual(HeaderStates.Neutral, shell.Header.State);
            shell.Select("Surface");
            Assert.AreEqual("changed while hidden", shell.Header.Sentence);
            Assert.AreEqual(HeaderStates.Error, shell.Header.State);
            Assert.AreEqual("stale", shell.Footer.text);
            Assert.AreEqual(DisplayStyle.Flex, shell.Footer.style.display.value);
        }

        [Test]
        public void ASurfaceSetWhileThePageDrawsReachesTheHeaderAndTheFooter() {
            var shell = new WindowShell(new WindowPages(new[] {
                new WindowPageDescription(typeof(DrawingSurfacePage), "Drawn", 0),
                new WindowPageDescription(typeof(CountingPage), "Plain", 1)
            }));

            Assert.AreEqual("set while drawing", shell.Header.Sentence);
            Assert.AreEqual(HeaderStates.Passed, shell.Header.State);
            Assert.AreEqual("set by the controls", shell.Footer.text);
            Assert.AreEqual(DisplayStyle.Flex, shell.Footer.style.display.value);
            shell.Select("Plain");
            Assert.AreEqual(string.Empty, shell.Header.Sentence);
            shell.Select("Drawn");
            Assert.AreEqual("set while drawing", shell.Header.Sentence);
            Assert.AreEqual("set by the controls", shell.Footer.text);
            var page = (DrawingSurfacePage)shell.CurrentPage;
            page.HeaderSentence = "once more";
            Assert.AreEqual("once more", shell.Header.Sentence);
            Assert.AreEqual(2, page.Draws);
        }

        [Test]
        public void APageNormalisesItsStateAndItsTextsAndRaisesSurfaceChanged() {
            var page = new SurfacePage();
            var changes = 0;
            page.SurfaceChanged += _ => changes++;
            page.HeaderState = null;
            page.HeaderSentence = null;
            page.FooterText = null;
            Assert.AreEqual(HeaderStates.Neutral, page.HeaderState);
            Assert.AreEqual(string.Empty, page.HeaderSentence);
            Assert.AreEqual(string.Empty, page.FooterText);
            Assert.AreEqual(3, changes);
            Assert.Throws<ArgumentException>(() => page.HeaderState = "not one word");
        }

        [Test]
        public void WindowShell_CustomImplementation() {
            var shell = new TaggedShell(ThreePages());
            Assert.AreEqual("tagged-pages", shell.PageList.name);
            Assert.AreEqual("tagged-body", shell.Body.name);
            Assert.AreEqual("tagged-header", shell.Header.name);
            Assert.AreEqual("tagged-content", shell.Content.name);
            Assert.AreEqual("tagged-page-controls", shell.PageControls.name);
            Assert.AreEqual("tagged-footer", shell.Footer.name);
            Assert.AreEqual("tagged-button", shell.PageList.Q<Button>().name);
            var empty = new TaggedShell(new WindowPages(Array.Empty<WindowPageDescription>()));
            Assert.AreEqual("tagged-empty", empty.Content.Q<Label>(className: WindowShell.EmptyClassName).name);
            var broken = new TaggedShell(new WindowPages(new[] {
                new WindowPageDescription(typeof(DrawingFailurePage), "Broken", 0)
            }));
            Assert.AreEqual("tagged-failure", broken.Content.Q<Label>(className: WindowShell.FailureClassName).name);
            Assert.AreEqual(1, _recording.CountOf(LogLevel.Error));
        }

        private sealed class SurfacePage : WindowPage {
            public SurfacePage() {
                HeaderSentence = "three rules to confirm";
                HeaderState = HeaderStates.Warning;
                FooterText = "compiled: current";
            }

            public override VisualElement CreateContent() {
                return new Label(Title) { name = ContentName };
            }
        }

        private sealed class DrawingSurfacePage : WindowPage {
            public int Draws { get; private set; }

            public override VisualElement CreateContent() {
                Draws++;
                HeaderSentence = "set while drawing";
                HeaderState = HeaderStates.Passed;
                return new Label(Title) { name = ContentName };
            }

            public override VisualElement CreateHeaderControls() {
                FooterText = "set by the controls";
                return new Button { text = "Refresh" };
            }
        }

        private sealed class ControlledPage : WindowPage {
            public const string ControlName = "page-control";

            public override VisualElement CreateContent() {
                return new Label(Title) { name = ContentName };
            }

            public override VisualElement CreateHeaderControls() {
                return new Button { name = ControlName, text = "Refresh" };
            }
        }

        private sealed class TaggedShell : WindowShell {
            public TaggedShell(WindowPages pages) : base(pages) { }

            protected override VisualElement CreatePageList() => new VisualElement { name = "tagged-pages" };

            protected override VisualElement CreateBody() => new VisualElement { name = "tagged-body" };

            protected override HeaderBar CreateHeader() => new HeaderBar { name = "tagged-header" };

            protected override VisualElement CreateContent() => new VisualElement { name = "tagged-content" };

            protected override VisualElement CreatePageControls() =>
                new VisualElement { name = "tagged-page-controls" };

            protected override Label CreateFooter() => new Label { name = "tagged-footer" };

            protected override Button CreatePageButton(WindowPageDescription page) {
                var button = base.CreatePageButton(page);
                button.name = "tagged-button";
                return button;
            }

            protected override Label CreateEmpty() => new Label(EmptyText) { name = "tagged-empty" };

            protected override Label CreateFailure(string text) => new Label(text) { name = "tagged-failure" };
        }

        private static WindowPages ThreePages() {
            return new WindowPages(new[] {
                new WindowPageDescription(typeof(CountingPage), "Gamma", 2),
                new WindowPageDescription(typeof(CountingPage), "Alpha", 0),
                new WindowPageDescription(typeof(CountingPage), "Beta", 1)
            });
        }

        private static string[] SelectedButtons(WindowShell shell) {
            return shell.PageList.Query<Button>(className: WindowShell.SelectedPageClassName).ToList()
                .Select(button => button.text)
                .ToArray();
        }
    }
}
