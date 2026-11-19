// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using NUnit.Framework;
using Spellbound.Core.Hashing;
using Spellbound.Core.Observations;

namespace Spellbound.Core.Tests {
    public class ObservationKeyTests {
        [TestCase("character.containment.rescues")]
        [TestCase("core.clock.rate_changed")]
        [TestCase("instancing.visible")]
        [TestCase("a1.b2_c3")]
        public void AcceptedTextsParse(string text) {
            Assert.IsTrue(ObservationKey.TryParse(text, out var key, out var error), error);
            Assert.AreEqual(text, key.Name);
            Assert.AreEqual(text, key.ToString());
            Assert.IsFalse(key.IsNone);
        }

        [TestCase("")]
        [TestCase(null)]
        [TestCase("rescues")]
        [TestCase("Character.rescues")]
        [TestCase("character..rescues")]
        [TestCase("character.rescues.")]
        [TestCase("character.1st")]
        [TestCase("character.res-cues")]
        [TestCase("character.res cues")]
        public void RefusedTextsDoNotParse(string text) {
            Assert.IsFalse(ObservationKey.TryParse(text, out var key, out var error));
            Assert.IsTrue(key.IsNone);
            Assert.IsNotEmpty(error);
            Assert.Throws<ArgumentException>(() => ObservationKey.Parse(text));
        }

        [Test]
        public void ThePackageIsTheFirstSegment() {
            Assert.AreEqual("character", ObservationKey.Parse("character.containment.rescues").Package);
            Assert.AreEqual("core", ObservationKey.Parse("core.clock.held").Package);
        }

        [Test]
        public void TheHashIsTheStableHashOfTheName() {
            var key = ObservationKey.Parse("worldobjects.index.awaken_job");
            Assert.AreEqual(StableHash.Fnv1A32("worldobjects.index.awaken_job"), key.Hash);
            Assert.AreEqual(ObservationKey.Parse("worldobjects.index.awaken_job"), key);
            Assert.AreNotEqual(ObservationKey.Parse("worldobjects.index.rows"), key);
        }

        [Test]
        public void ADefaultKeyIsNone() {
            Assert.IsTrue(default(ObservationKey).IsNone);
            Assert.AreEqual(string.Empty, default(ObservationKey).ToString());
        }

        [Test]
        public void UnitsCompareByName() {
            Assert.AreEqual(ObservationUnit.Count, default(ObservationUnit));
            Assert.AreEqual("count", default(ObservationUnit).Name);
            Assert.AreEqual(ObservationUnit.Milliseconds, ObservationUnit.Named("ms"));
            Assert.AreNotEqual(ObservationUnit.Bytes, ObservationUnit.Seconds);
            Assert.AreEqual("voxels", ObservationUnit.Named(" voxels ").Name);
            Assert.Throws<ArgumentException>(() => ObservationUnit.Named(" "));
        }
    }
}
