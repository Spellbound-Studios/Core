// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using Spellbound.Core.Authoring.Editor;
using Spellbound.Core.Observations;
using Spellbound.Core.Operations;
using Spellbound.Core.Operations.Editor;
using Spellbound.Core.Tests.AuthoringFixture;
using UnityEditor;
using UnityEngine;

namespace Spellbound.Core.Tests {
    public class WritesOperationsDryRunTests {
        private const string Previewed = "previewed: ";
        private const string AssetsFolder = "Assets";
        private const string ClaudeFolder = ".claude";

        private static readonly Assembly Core = typeof(OperationHost).Assembly;
        private static readonly Assembly CoreEditor = typeof(AuthoringOperations).Assembly;

        private AuthoringTestFolder _folder;
        private OperationSettings _settings;
        private OperationHost _host;
        private OperationCaller _approver;

        [SetUp]
        public void SetUp() {
            _folder = new AuthoringTestFolder();
            _settings = ScriptableObject.CreateInstance<OperationSettings>();
            _settings.draftsFolder = _folder.Path + "/Drafts";
            _host = new OperationHost(new OperationCatalog(() => new[] { Core, CoreEditor }), () => _settings);
            _approver = new OperationCaller("window", OperationAudiences.Developer, OperationAudiences.Approver);
        }

        [TearDown]
        public void TearDown() {
            _folder.Dispose();
            UnityEngine.Object.DestroyImmediate(_settings);
        }

        [Test]
        public void EveryWritesOperationOfCoreIsCoveredHere() {
            var covered = new[] { "approve", "context-install", "revise" };
            var declared = new List<string>();

            foreach (var operation in _host.Catalog.Group("core").Operations) {
                if (operation.Effect == OperationEffect.Writes) {
                    declared.Add(operation.Name);
                }
            }

            declared.Sort(StringComparer.Ordinal);
            CollectionAssert.AreEqual(covered, declared);
        }

        [Test]
        public void ReviseUnderDryRunWritesNothing() {
            var content = _folder.Subfolder("Content");
            _folder.Create("Granite", "granite", 1, content);
            AssertWritesNothing("core revise asset:" + content + "/Granite.asset --dry-run", null);
        }

        [Test]
        public void ApproveOfADraftUnderDryRunWritesNothing() {
            var content = _folder.Subfolder("Content");
            var draft = DefinitionDrafts.Create<FixtureDefinition>("Pebble", definition => definition.title = "pebble",
                _settings);
            AssertWritesNothing("core approve asset:" + draft.Path + " --to " + content + " --dry-run", _approver);
        }

        [Test]
        public void ApproveOfARevisionUnderDryRunWritesNothing() {
            var content = _folder.Subfolder("Content");
            _folder.Create("Granite", "granite", 1, content);
            StringAssert.StartsWith("succeeded: ", Run("core revise asset:" + content + "/Granite.asset", null));
            AssertWritesNothing("core approve asset:Granite-revision --dry-run", _approver);
        }

        [TestCase("skill")]
        [TestCase("agents")]
        [TestCase("claude")]
        public void ContextInstallUnderDryRunWritesNothing(string target) {
            var originals = ReadContextFiles();
            var claudeFolder = Path.Combine(ContextInstaller.ProjectRoot(), ClaudeFolder);
            var claudeFolderExisted = Directory.Exists(claudeFolder);

            try {
                AssertWritesNothing("core context-install --target " + target + " --dry-run", null);
            }
            finally {
                RestoreContextFiles(originals);

                if (!claudeFolderExisted && Directory.Exists(claudeFolder)) {
                    Directory.Delete(claudeFolder, true);
                }
            }
        }

        private void AssertWritesNothing(string line, OperationCaller caller) {
            var before = Snapshot();
            var text = Run(line, caller);
            var after = Snapshot();
            CollectionAssert.IsEmpty(before.Differences(after), text);
            StringAssert.StartsWith(Previewed, text);
        }

        private static FileSystemSnapshot Snapshot() {
            var diskPaths = new List<string>(ContextPaths()) { FileUtil.GetPhysicalPath(AssetsFolder) };
            return FileSystemSnapshot.Take(diskPaths, new[] { AssetsFolder });
        }

        private string Run(string line, OperationCaller caller) {
            return _host.Run(line, caller ?? OperationCaller.Development(), ObservationContext.Discard).Result.Text;
        }

        private static string[] ContextPaths() {
            var root = ContextInstaller.ProjectRoot();
            return new[] {
                Path.Combine(root, ContextInstaller.AgentsFileName),
                Path.Combine(root, ContextInstaller.ClaudeFileName),
                Path.Combine(root, ClaudeFolder)
            };
        }

        private static Dictionary<string, byte[]> ReadContextFiles() {
            var originals = new Dictionary<string, byte[]>();
            var root = ContextInstaller.ProjectRoot();

            foreach (ContextInstallTarget target in Enum.GetValues(typeof(ContextInstallTarget))) {
                var path = ContextInstaller.PathFor(target, root);
                originals[path] = File.Exists(path) ? File.ReadAllBytes(path) : null;
            }

            return originals;
        }

        private static void RestoreContextFiles(Dictionary<string, byte[]> originals) {
            foreach (var entry in originals) {
                if (entry.Value != null) {
                    File.WriteAllBytes(entry.Key, entry.Value);
                }
                else if (File.Exists(entry.Key)) {
                    File.Delete(entry.Key);
                }
            }
        }
    }
}
