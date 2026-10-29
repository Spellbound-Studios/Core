// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using NUnit.Framework;
using Spellbound.Core.Authoring;
using Spellbound.Core.Controls.Editor;

namespace Spellbound.Core.Tests {
    public class HeaderStatesTests {
        [Test]
        public void TheSixShippedStatesAreNamedAndEachHasItsClass() {
            CollectionAssert.AreEqual(
                new[] { "neutral", "passed", "information", "warning", "error", "stale" }, HeaderStates.Shipped);
            Assert.AreEqual("sb-header--passed", HeaderStates.ClassNameFor(HeaderStates.Passed));
            Assert.AreEqual("sb-header--stale", HeaderStates.ClassNameFor(HeaderStates.Stale));
        }

        [Test]
        public void ASeverityMapsToItsState() {
            Assert.AreEqual(HeaderStates.Error, HeaderStates.For(FindingSeverity.Error));
            Assert.AreEqual(HeaderStates.Warning, HeaderStates.For(FindingSeverity.Warning));
            Assert.AreEqual(HeaderStates.Information, HeaderStates.For(FindingSeverity.Information));
        }

        [Test]
        public void AnEmptyStateIsNeutralAnOpenIdIsLoweredAndAnythingButLettersDigitsAndDashesIsRefused() {
            Assert.AreEqual(HeaderStates.Neutral, HeaderStates.Normalize(null));
            Assert.AreEqual(HeaderStates.Neutral, HeaderStates.Normalize("  "));
            Assert.AreEqual("in-sync", HeaderStates.Normalize(" In-Sync "));
            Assert.AreEqual("sb-header--in-sync", HeaderStates.ClassNameFor("in-sync"));
            Assert.AreEqual("sb-header--error", HeaderStates.ClassNameFor("Error"));
            Assert.Throws<ArgumentException>(() => HeaderStates.Normalize("in sync"));
            Assert.Throws<ArgumentException>(() => HeaderStates.Normalize("in.sync"));
            Assert.Throws<ArgumentException>(() => HeaderStates.Normalize("in_sync"));
        }
    }
}
