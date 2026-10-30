// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Spellbound.Core.Logging;
using Spellbound.Core.Window.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Spellbound.Core.Tests {
    public class WindowPagesTests {
        [WindowPage("Attributed Fixture", 7)]
        internal sealed class AttributedPage : WindowPage {
            public override VisualElement CreateContent() {
                return new Label(Title);
            }
        }

        [WindowPage("Needs An Argument")]
        internal sealed class ArgumentPage : WindowPage {
            public ArgumentPage(int depth) {
                Depth = depth;
            }

            public int Depth { get; }

            public override VisualElement CreateContent() {
                return new Label(Title + " " + Depth);
            }
        }

        private sealed class PlainPage : WindowPage {
            public override VisualElement CreateContent() {
                return new VisualElement();
            }
        }

        private sealed class OtherPage : WindowPage {
            public override VisualElement CreateContent() {
                return new VisualElement();
            }
        }

        private sealed class GreetingPage : WindowPage {
            public const string ContentName = "greeting";
            public const string Greeting = "Hello from a page the package never saw.";

            public override VisualElement CreateContent() {
                return new Label(Greeting) { name = ContentName };
            }
        }

        private RecordingLogSink _recording;
        private Log.SinkSuspension _suspension;

        [SetUp]
        public void SetUp() {
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
        public void PagesFromADescriptionListAreOrderedByOrderThenTitle() {
            var pages = new WindowPages(new[] {
                Describe(typeof(PlainPage), "Zeta", 0), Describe(typeof(PlainPage), "Alpha", 1),
                Describe(typeof(PlainPage), "Beta", 0), null
            });

            CollectionAssert.AreEqual(new[] { "Beta", "Zeta", "Alpha" },
                pages.Pages.Select(page => page.Title).ToArray());
            Assert.AreEqual(0, _recording.Count);
        }

        [Test]
        public void ASecondPageWithTheSameTitleIsDroppedWithOneError() {
            var console = new UnityConsoleSink();
            Log.AddSink(console, null, LogLevel.Error);

            try {
                LogAssert.Expect(LogType.Error, new Regex("Two window pages are titled 'Same'"));
                var pages = new WindowPages(new[] {
                    Describe(typeof(OtherPage), "Same", 5), Describe(typeof(PlainPage), "Same", 0)
                });

                Assert.AreEqual(1, pages.Pages.Count);
                Assert.AreEqual(typeof(PlainPage), pages.Pages[0].PageType);
                Assert.AreEqual(1, _recording.CountOf(LogLevel.Error));
                Assert.IsTrue(_recording.Contains(LogLevel.Error, "the first is kept"));
            }
            finally {
                Log.RemoveSink(console);
            }
        }

        [Test]
        public void WindowPage_CustomImplementation() {
            var pages = new WindowPages(new[] { Describe(typeof(GreetingPage), "Greeting", 0) });
            var description = pages.Find("greeting");
            Assert.IsNotNull(description);
            var page = description.Create();
            Assert.IsInstanceOf<GreetingPage>(page);
            Assert.AreEqual("Greeting", page.Title);
            var shell = new WindowShell(pages);
            Assert.IsInstanceOf<GreetingPage>(shell.CurrentPage);
            Assert.AreEqual(GreetingPage.Greeting, shell.Content.Q<Label>(GreetingPage.ContentName).text);
        }

        [Test]
        public void FromReadsTheTitleAndOrderOfAnAttributedType() {
            var description = WindowPageDescription.From(typeof(AttributedPage));
            Assert.IsNotNull(description);
            Assert.AreEqual(typeof(AttributedPage), description.PageType);
            Assert.AreEqual("Attributed Fixture", description.Title);
            Assert.AreEqual(7, description.Order);
        }

        [Test]
        public void FromReturnsNullForAnUnattributedType() {
            Assert.IsNull(WindowPageDescription.From(typeof(PlainPage)));
            Assert.IsNull(WindowPageDescription.From(null));
        }

        [Test]
        public void TheWindowsTypeSourceNeverListsAnInternalPage() {
            CollectionAssert.Contains(TypeCache.GetTypesWithAttribute<WindowPageAttribute>(), typeof(AttributedPage));
            var listed = WindowPages.TypeCacheTypes().ToList();
            CollectionAssert.DoesNotContain(listed, typeof(AttributedPage));
            CollectionAssert.DoesNotContain(listed, typeof(ArgumentPage));
        }

        [Test]
        public void ATypeSourceKeepsAttributedPagesAndLogsOneItCannotDescribe() {
            var types = new[] { typeof(PlainPage), typeof(AttributedPage), typeof(ArgumentPage) };
            var pages = new WindowPages(() => types);
            CollectionAssert.AreEqual(new[] { typeof(AttributedPage) },
                pages.Pages.Select(page => page.PageType).ToArray());
            Assert.AreEqual(1, _recording.CountOf(LogLevel.Error));
            Assert.IsTrue(_recording.Contains(LogLevel.Error, typeof(ArgumentPage).FullName));
        }

        [Test]
        public void ResetGivesANewDefault() {
            var before = WindowPages.Default;
            Assert.AreSame(before, WindowPages.Default);
            WindowPages.Reset();
            Assert.AreNotSame(before, WindowPages.Default);
        }

        [Test]
        public void FindMatchesATrimmedTitleInAnyCase() {
            var pages = new WindowPages(new[] { Describe(typeof(PlainPage), "Packages", 0) });
            Assert.AreSame(pages.Pages[0], pages.Find(" packages "));
            Assert.IsNull(pages.Find("Missing"));
            Assert.IsNull(pages.Find(null));
            Assert.IsNull(pages.Find(" "));
        }

        [Test]
        public void ADescriptionNeedsAConcretePageWithAParameterlessConstructorAndATitle() {
            Assert.Throws<ArgumentException>(() => Describe(null, "Nothing", 0));
            Assert.Throws<ArgumentException>(() => Describe(typeof(string), "Text", 0));
            Assert.Throws<ArgumentException>(() => Describe(typeof(WindowPage), "Abstract", 0));
            Assert.Throws<ArgumentException>(() => Describe(typeof(ArgumentPage), "Argument", 0));
            Assert.Throws<ArgumentException>(() => Describe(typeof(PlainPage), " ", 0));
            Assert.AreEqual("Trimmed", Describe(typeof(PlainPage), "  Trimmed  ", 0).Title);
        }

        [Test]
        public void TheAttributeNeedsATitleAndTrimsIt() {
            Assert.Throws<ArgumentException>(() => new WindowPageAttribute(" "));
            var attribute = new WindowPageAttribute("  Packages  ", 3);
            Assert.AreEqual("Packages", attribute.Title);
            Assert.AreEqual(3, attribute.Order);
            Assert.AreEqual(0, new WindowPageAttribute("Default").Order);
        }

        private static WindowPageDescription Describe(Type pageType, string title, int order) {
            return new WindowPageDescription(pageType, title, order);
        }
    }
}
