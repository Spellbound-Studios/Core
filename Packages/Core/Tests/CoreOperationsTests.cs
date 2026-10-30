// Copyright (c) Spellbound Studios. All rights reserved.

using System.Reflection;
using NUnit.Framework;
using Spellbound.Core.Observations;
using Spellbound.Core.Operations;
using Spellbound.Core.Tests.OperationFixture;
using UnityEngine;

namespace Spellbound.Core.Tests {
    public class CoreOperationsTests {
        private static readonly Assembly Fixture = typeof(FixtureOperations).Assembly;
        private static readonly Assembly Core = typeof(OperationHost).Assembly;

        private OperationHost _host;
        private OperationCaller _developer;

        [SetUp]
        public void SetUp() {
            _host = new OperationHost(new OperationCatalog(() => new[] { Fixture, Core }), () => ScriptableObject.CreateInstance<OperationSettings>());
            _developer = OperationCaller.Development();
        }

        private string Text(string line) => _host.Run(line, _developer, ObservationContext.Discard).Result.Text;

        [Test]
        public void HelpListsTheGroups() {
            var text = Text("core help");
            StringAssert.StartsWith("succeeded: 2 groups, ", text);
            StringAssert.Contains("\ncore: Gives every package one vocabulary", text);
            StringAssert.Contains("\nfixture: Operations that exist so the catalog and the host can be tested. (", text);
            StringAssert.Contains("Run: core help --group <name>, or core help --all", text);
        }

        [Test]
        public void HelpForAGroupListsItsOperationsWithArguments() {
            var text = Text("core help --group fixture");
            StringAssert.StartsWith("succeeded: fixture: Operations that exist", text);
            StringAssert.Contains("\n  fixture add --left <integer> [--right <integer> = 1]\n    Adds two integers. [reads, developer]\n    --left: The left operand.\n    --right: The right operand.", text);
            StringAssert.Contains("\n  fixture tier --tier <one of Common, Rare, Epic>", text);
            StringAssert.Contains("\n  core help [--group <text>] [--all <true or false> = false]", Text("core help --group core"));
            StringAssert.Contains("[reads, developer, while-playing]", text);
            StringAssert.Contains("[destroys, developer]", text);
        }

        [Test]
        public void HelpForAnUnknownGroupFails() {
            Assert.AreEqual("failed: no group 'nowhere'\nRun: core help", Text("core help --group nowhere"));
        }

        [Test]
        public void HelpAllListsEveryOperation() {
            var text = Text("core help --all");
            StringAssert.Contains("\n  core about", text);
            StringAssert.Contains("\n  fixture echo --text <text>", text);
        }

        [Test]
        public void HelpIsOpenToAnyone() {
            Assert.IsTrue(_host.Run("core help", new OperationCaller("player"), ObservationContext.Discard).Result.Succeeded);
        }

        [Test]
        public void AboutNamesUnityThePipelineAndTheGroups() {
            var text = Text("core about");
            StringAssert.StartsWith("succeeded: Unity " + Application.unityVersion + ", editor, not playing\nrender pipeline: ", text);
            StringAssert.Contains("\ncore: 6 operations in Spellbound.Core", text);
            StringAssert.Contains("\nfixture: ", text);
        }

        [Test]
        public void HistoryListsTheLinesRun() {
            Text("fixture echo one");
            Text("fixture add x");
            var text = Text("core history");
            StringAssert.StartsWith("succeeded: frame", text);
            StringAssert.Contains("development  succeeded  fixture echo one", text);
            StringAssert.Contains("development  invalid    fixture add x", text);
            var paged = Text("core history --offset 1 --limit 1");
            StringAssert.Contains("fixture add x", paged);
            StringAssert.DoesNotContain("fixture echo one", paged);
        }

        [Test]
        public void StatsListsCallsAndFailures() {
            Text("fixture echo one");
            Text("fixture boom");
            var text = Text("core stats");
            StringAssert.StartsWith("succeeded: operation", text);
            StringAssert.Contains("fixture boom  1      1", text);
            StringAssert.Contains("fixture echo  1      0", text);
        }

        [Test]
        public void TheFrontDoorIsOneHostThatRunsCoreHelp() {
            var run = CoreKit.Operations.Run("core help", OperationCaller.Development(), ObservationContext.Discard);
            Assert.IsTrue(run.IsDone);
            Assert.IsTrue(run.Result.Succeeded);
            Assert.AreSame(CoreKit.Operations, CoreKit.Operations);
        }
    }
}
