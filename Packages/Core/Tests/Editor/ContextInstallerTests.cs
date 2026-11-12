// Copyright (c) Spellbound Studios. All rights reserved.

using System.IO;
using NUnit.Framework;
using Spellbound.Core.Operations;
using Spellbound.Core.Operations.Editor;

namespace Spellbound.Core.Tests {
    public class ContextInstallerTests {
        private string _root;

        [SetUp]
        public void SetUp() {
            _root = Path.Combine(Path.GetTempPath(), "spellbound-context-" + Path.GetRandomFileName());
            Directory.CreateDirectory(_root);
        }

        [TearDown]
        public void TearDown() {
            if (Directory.Exists(_root)) {
                Directory.Delete(_root, true);
            }
        }

        [Test]
        public void SkillWritesTheSkillFileUnderTheClaudeSkillsFolder() {
            var path = ContextInstaller.Install(ContextInstallTarget.Skill, _root, out var created);
            Assert.IsTrue(created);
            Assert.AreEqual(Path.Combine(_root, ".claude", "skills", "spellbound", "SKILL.md"), path);
            var text = File.ReadAllText(path);
            StringAssert.StartsWith("---\nname: spellbound\n", text);
            StringAssert.Contains("unity command --query spellbound", text);
            StringAssert.DoesNotContain("spellbound_run", text);
        }

        [Test]
        public void AgentsCreatesTheFileWithOneBlock() {
            var path = ContextInstaller.Install(ContextInstallTarget.Agents, _root, out var created);
            Assert.IsTrue(created);
            Assert.AreEqual(Path.Combine(_root, "AGENTS.md"), path);
            Assert.IsTrue(GeneratedBlock.IsCurrent(File.ReadAllText(path), ContextInstaller.BlockName));
        }

        [Test]
        public void ARerunReplacesOnlyTheBlockAndKeepsTheNotesAroundIt() {
            var path = Path.Combine(_root, "CLAUDE.md");
            File.WriteAllText(path, "# My notes\n\nkeep this\n\n" + GeneratedBlock.Render(ContextInstaller.BlockName, "stale") + "\n\nand this\n");
            ContextInstaller.Install(ContextInstallTarget.Claude, _root, out var created);
            Assert.IsFalse(created);
            var text = File.ReadAllText(path);
            StringAssert.StartsWith("# My notes\n\nkeep this\n\n<!-- spellbound:context ", text);
            StringAssert.EndsWith("<!-- /spellbound:context -->\n\nand this\n", text);
            StringAssert.DoesNotContain("stale", text);
            Assert.IsTrue(GeneratedBlock.IsCurrent(text, ContextInstaller.BlockName));
        }

        [Test]
        public void TheStubNamesNoPipelineSpelling() {
            StringAssert.DoesNotContain("spellbound_run", AgentContextStub.Body);
            StringAssert.DoesNotContain("spellbound run", AgentContextStub.Body);
        }
    }
}
