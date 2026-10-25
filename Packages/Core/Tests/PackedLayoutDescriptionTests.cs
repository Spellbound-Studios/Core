// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using NUnit.Framework;

namespace Spellbound.Core.Tests {
    public class PackedLayoutDescriptionTests {
        private enum Kind : byte {
            None = 0,
            Some = 1
        }

        private struct Ordered {
            public int First;
            public float Second;
            public byte Third;
        }

        private struct Inner {
            public int Depth;
        }

        private struct Outer {
            public int Before;
            public Inner Nested;
            public int After;
        }

        private struct NamedAlpha {
            public int Alpha;
        }

        private struct NamedBeta {
            public int Beta;
        }

        private struct OrderAscending {
            public int One;
            public float Two;
        }

        private struct OrderDescending {
            public float Two;
            public int One;
        }

        private struct WithEnum {
            public Kind Category;
        }

        private sealed class Node {
            public Node Next;
            public int Value;
        }

        [Test]
        public void FieldOrderFollowsDeclarationOrder() {
            var lines = CoreKit.Packing.Describe(typeof(Ordered)).Split('\n');
            Assert.AreEqual(4, lines.Length);
            Assert.AreEqual("  First : System.Int32", lines[1]);
            Assert.AreEqual("  Second : System.Single", lines[2]);
            Assert.AreEqual("  Third : System.Byte", lines[3]);
        }

        [Test]
        public void NestedStructIsExpandedOneLevelDeeper() {
            var lines = CoreKit.Packing.Describe(typeof(Outer)).Split('\n');
            Assert.AreEqual(5, lines.Length);
            Assert.AreEqual("  Nested : " + typeof(Inner).FullName, lines[2]);
            Assert.AreEqual("    Depth : System.Int32", lines[3]);
            Assert.AreEqual("  After : System.Int32", lines[4]);
        }

        [Test]
        public void FieldNameChangesTheDescription() {
            var alpha = CoreKit.Packing.Describe(typeof(NamedAlpha)).Replace(nameof(NamedAlpha), string.Empty);
            var beta = CoreKit.Packing.Describe(typeof(NamedBeta)).Replace(nameof(NamedBeta), string.Empty);
            Assert.AreNotEqual(alpha, beta);
        }

        [Test]
        public void FieldOrderChangesTheDescription() {
            var ascending = CoreKit.Packing.Describe(typeof(OrderAscending))
                .Replace(nameof(OrderAscending), string.Empty);
            var descending = CoreKit.Packing.Describe(typeof(OrderDescending))
                .Replace(nameof(OrderDescending), string.Empty);
            Assert.AreNotEqual(ascending, descending);
        }

        [Test]
        public void SelfReferencingClassTerminatesAndNamesTheReference() {
            var lines = CoreKit.Packing.Describe(typeof(Node)).Split('\n');
            Assert.AreEqual(3, lines.Length);
            Assert.AreEqual("  Next : " + typeof(Node).FullName, lines[1]);
            Assert.AreEqual("  Value : System.Int32", lines[2]);
        }

        [Test]
        public void EnumFieldWritesItsUnderlyingType() {
            var lines = CoreKit.Packing.Describe(typeof(WithEnum)).Split('\n');
            Assert.AreEqual("  Category : " + typeof(Kind).FullName + "(System.Byte)", lines[1]);
        }

        [Test]
        public void DescribingTheSameTypeTwiceGivesIdenticalText() {
            Assert.AreEqual(
                CoreKit.Packing.Describe(typeof(Outer)),
                CoreKit.Packing.Describe(typeof(Outer)));
        }

        [Test]
        public void NullTypeThrows() {
            Assert.Throws<ArgumentNullException>(() => CoreKit.Packing.Describe(null));
        }
    }
}
