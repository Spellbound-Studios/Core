// Copyright (c) Spellbound Studios. All rights reserved.

using System.Reflection;
using NUnit.Framework;
using Spellbound.Core.Operations;
using Spellbound.Core.Tests.OperationFixture;
using Spellbound.Fixture.Other;

namespace Spellbound.Core.Tests {
    public class AgentContextBlocksTests {
        private static readonly Assembly Fixture = typeof(FixtureOperations).Assembly;

        private OperationCatalog _catalog;

        [SetUp]
        public void SetUp() {
            _catalog = new OperationCatalog(() => new[] { Fixture });
        }

        [Test]
        public void OperationsBlockListsEveryOperationWithItsArguments() {
            var block = AgentContextBlocks.Operations(_catalog.Group("fixture"), "com.example.fixture 1.2.3");
            StringAssert.StartsWith("com.example.fixture 1.2.3\nOperations of the group `fixture`, run as `<group> <operation> [arguments]`:", block);
            StringAssert.Contains("\n- `fixture add --left <integer> [--right <integer> = 1]`: Adds two integers. [reads, developer]\n  - `--left`: The left operand.\n  - `--right`: The right operand. Default: 1.", block);
            StringAssert.Contains("\n- `fixture wipe`: Deletes everything, or says what it would delete. [destroys, developer]", block);
            StringAssert.Contains("[reads, developer, while-playing]", block);
        }

        [Test]
        public void OperationsBlockIsTheSameTextOnEveryRender() {
            var group = _catalog.Group("fixture");
            Assert.AreEqual(AgentContextBlocks.Operations(group, null), AgentContextBlocks.Operations(group, null));
        }

        [Test]
        public void OperationsBlockHashChangesWhenAnOperationChanges() {
            var group = _catalog.Group("fixture");
            var body = AgentContextBlocks.Operations(group, "p 1");
            Assert.AreNotEqual(GeneratedBlock.Hash(body), GeneratedBlock.Hash(body.Replace("Adds two integers.", "Adds three integers.")));
            Assert.AreEqual(GeneratedBlock.Hash(body), GeneratedBlock.Hash(body.Replace("\n", "\r\n")));
        }

        [Test]
        public void DoorsBlockListsEachDoorWithItsMembers() {
            var doors = BridgeDoors.Find(new[] { typeof(FixtureOtherKit).Assembly });
            var block = AgentContextBlocks.Doors(doors);
            StringAssert.Contains("\n- `FixtureOtherKit` in `Spellbound.Fixture.Other`", block);
            StringAssert.Contains("\n  - `static List<string> Names(string prefix, int count = 3)`", block);
            Assert.AreEqual("Bridge doors in this project, the constructors of the types a bridge assembly adds:\n- none", AgentContextBlocks.Doors(null));
        }
    }
}
