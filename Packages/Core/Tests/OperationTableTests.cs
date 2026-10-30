// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using NUnit.Framework;
using Spellbound.Core.Operations;

namespace Spellbound.Core.Tests {
    public class OperationTableTests {
        [Test]
        public void TextAlignsColumnsAndKeepsTheLastColumnUnpadded() {
            var table = new OperationTable("name", "count", "note").AddRow("apples", 3, "ripe").AddRow("kiwi", 1200, null);
            Assert.AreEqual("name    count  note\napples  3      ripe\nkiwi    1200   ", table.ToText());
        }

        [Test]
        public void NumbersUseTheInvariantCulture() {
            var table = new OperationTable("value").AddRow(1.5);
            Assert.AreEqual("value\n1.5", table.ToText());
        }

        [Test]
        public void ARowNeedsOneCellPerColumn() {
            var table = new OperationTable("a", "b");
            Assert.Throws<ArgumentException>(() => table.AddRow("only one"));
        }

        [Test]
        public void ATableNeedsAColumn() {
            Assert.Throws<ArgumentException>(() => new OperationTable());
        }
    }
}
