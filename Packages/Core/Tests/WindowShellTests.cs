// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Spellbound.Core.Logging;
using Spellbound.Core.Window.Editor;
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
