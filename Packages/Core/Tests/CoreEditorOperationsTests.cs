// Copyright (c) Spellbound Studios. All rights reserved.

using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Spellbound.Core.Operations;
using Spellbound.Core.Operations.Editor;
using Spellbound.Core.Tests.OperationFixture;
using UnityEngine;

namespace Spellbound.Core.Tests {
    public class CoreEditorOperationsTests {
        private static readonly Assembly Fixture = typeof(FixtureOperations).Assembly;
        private static readonly Assembly Core = typeof(OperationHost).Assembly;
        private static readonly Assembly CoreEditor = typeof(CoreEditorOperations).Assembly;

        private OperationHost _host;
        private OperationCaller _developer;

        [SetUp]
        public void SetUp() {
            _host = new OperationHost(new OperationCatalog(() => new[] { Fixture, Core, CoreEditor }), () => ScriptableObject.CreateInstance<OperationSettings>());
            _developer = OperationCaller.Development();
        }

        private string Text(string line) => _host.Run(line, _developer).Result.Text;

        [Test]
        public void TheEditorAssemblyJoinsTheCoreGroup() {
            Assert.IsNotNull(_host.Catalog.Describe("core", "context"));
            Assert.IsNotNull(_host.Catalog.Describe("core", "context-install"));
            Assert.IsNotNull(_host.Catalog.Describe("core", "ledger"));
            CollectionAssert.Contains(_host.Catalog.Group("core").Assemblies, "Spellbound.Core.Editor");
        }

        [Test]
        public void ContextListsTheInstalledPackagesAndTheGroupsWithoutOne() {
            var text = Text("core context");
            StringAssert.StartsWith("succeeded: ", text);
            StringAssert.Contains("com.spellboundstudios.core ", text);
            StringAssert.Contains(": group core, ", text);
            StringAssert.Contains("front door `CoreKit`", text);
            StringAssert.Contains("Run: core context --package <group>", text);
        }

        [Test]
        public void ContextForCorePrintsItsFileWithACurrentOperationsBlock() {
            var text = Text("core context --package core").Replace("\r\n", "\n");
            StringAssert.StartsWith("succeeded: ", text);
            StringAssert.Contains("AGENTS.md\n", text);
            StringAssert.Contains("\n## Purpose\n", text);
            StringAssert.Contains("\n## Ledger\n", text);
            Assert.IsTrue(GeneratedBlock.IsCurrent(text, AgentContextBlocks.OperationsBlock));
            StringAssert.Contains("- `core context [--package <text>]`", GeneratedBlock.Body(text, AgentContextBlocks.OperationsBlock));
            StringAssert.Contains("com.spellboundstudios.core ", GeneratedBlock.Body(text, AgentContextBlocks.OperationsBlock));
        }

        [Test]
        public void ContextForTheFixtureGroupPrintsThePackageThatHoldsIt() {
            var text = Text("core context --package fixture");
            StringAssert.StartsWith("succeeded: ", text);
            StringAssert.Contains("AGENTS.md\n", text);
            StringAssert.Contains("- `fixture echo --text <text>`", GeneratedBlock.Body(text, AgentContextBlocks.OperationsBlock));
            StringAssert.Contains("- `FixtureOtherKit` in `Spellbound.Fixture.Other`", GeneratedBlock.Body(text, AgentContextBlocks.DoorsBlock));
        }

        [Test]
        public void ContextForAnUnknownNameFails() {
            Assert.AreEqual("failed: no group or package 'nowhere'\nRun: core context", Text("core context --package nowhere"));
        }

        [Test]
        public void ContextIsOpenToAnyone() {
            Assert.IsTrue(_host.Run("core context", new OperationCaller("player")).Result.Succeeded);
        }

        [Test]
        public void LedgerListsCoreRows() {
            var text = Text("core ledger");
            StringAssert.StartsWith("succeeded: package", text);
            StringAssert.Contains("com.spellboundstudios.core", text);
            StringAssert.Contains("Builds on", text);
        }

        [Test]
        public void RefreshAddsTheDoorsBlockAfterTheOperationsBlock() {
            var group = _host.Catalog.Group("fixture");
            var doors = BridgeDoors.Find(new[] { typeof(Spellbound.Fixture.Other.FixtureOtherKit).Assembly });
            var text = CoreEditorOperations.Refresh("## Front door\n\n`FixtureKit`\n\n## Rules\n\nr\n", group, null, doors);
            Assert.IsTrue(GeneratedBlock.IsCurrent(text, AgentContextBlocks.OperationsBlock));
            Assert.IsTrue(GeneratedBlock.IsCurrent(text, AgentContextBlocks.DoorsBlock));
            Assert.Less(text.IndexOf("<!-- spellbound:operations"), text.IndexOf("<!-- spellbound:doors"));
            Assert.Less(text.IndexOf("<!-- /spellbound:doors"), text.IndexOf("## Rules"));
        }

        [Test]
        public void AboutListsTheInstalledPackagesThroughTheSeam() {
            var previous = InstalledPackages.Source;

            try {
                InstalledPackages.Source = () => new List<InstalledPackageDescription> { new InstalledPackageDescription("com.example.one", "One", "9.9.9", null) };
                StringAssert.Contains("\ncom.example.one 9.9.9\n", Text("core about"));
                InstalledPackages.Source = null;
                StringAssert.DoesNotContain("com.example.one", Text("core about"));
                CollectionAssert.IsEmpty(InstalledPackages.All());
            }
            finally {
                InstalledPackages.Source = previous;
            }
        }

        [Test]
        public void TheEditorSourceListsCore() {
            var core = InstalledPackages.Find("com.spellboundstudios.core");
            Assert.IsNotNull(core);
            Assert.IsNotNull(core.Path);
            Assert.AreEqual(core.Name, EditorInstalledPackages.ForAssembly(typeof(CoreKit).Assembly).Name);
        }
    }
}
