// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Reflection;
using NUnit.Framework;
using Spellbound.Core.Sections;

namespace Spellbound.Core.Tests {
    public class SectionAttributeTests {
        [Section("test.sample")]
        private struct Sample { }

        [Test]
        public void NameIsKept() {
            var attribute = typeof(Sample).GetCustomAttribute<SectionAttribute>();
            Assert.IsNotNull(attribute);
            Assert.AreEqual("test.sample", attribute.Name);
        }

        [Test]
        public void EmptyNameThrows() {
            Assert.Throws<ArgumentException>(() => new SectionAttribute(string.Empty));
        }

        [Test]
        public void WhitespaceNameThrows() {
            Assert.Throws<ArgumentException>(() => new SectionAttribute("   "));
        }

        [Test]
        public void NullNameThrows() {
            Assert.Throws<ArgumentException>(() => new SectionAttribute(null));
        }

        [Test]
        public void AttributeIsNotInheritedAndAllowsOnePerType() {
            var usage = typeof(SectionAttribute).GetCustomAttribute<AttributeUsageAttribute>();
            Assert.IsFalse(usage.Inherited);
            Assert.IsFalse(usage.AllowMultiple);
        }
    }
}
