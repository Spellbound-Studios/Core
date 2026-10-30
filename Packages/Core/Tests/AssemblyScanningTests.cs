// Copyright (c) Spellbound Studios. All rights reserved.

using System.Linq;
using NUnit.Framework;
using Spellbound.Core.Tooling;

namespace Spellbound.Core.Tests {
    public class AssemblyScanningTests {
        [Test]
        public void TheEngineAndTheFrameworkAreSkippedByExactNameOrByFamily() {
            Assert.IsTrue(AssemblyScanning.IsSkipped("mscorlib"));
            Assert.IsTrue(AssemblyScanning.IsSkipped("netstandard"));
            Assert.IsTrue(AssemblyScanning.IsSkipped("System"));
            Assert.IsTrue(AssemblyScanning.IsSkipped("System.Core"));
            Assert.IsTrue(AssemblyScanning.IsSkipped("Mono.Security"));
            Assert.IsTrue(AssemblyScanning.IsSkipped("nunit.framework"));
            Assert.IsTrue(AssemblyScanning.IsSkipped("UnityEngine"));
            Assert.IsTrue(AssemblyScanning.IsSkipped("UnityEngine.CoreModule"));
            Assert.IsTrue(AssemblyScanning.IsSkipped("UnityEditor.CoreModule"));
            Assert.IsTrue(AssemblyScanning.IsSkipped("Unity.Collections"));
            Assert.IsTrue(AssemblyScanning.IsSkipped(null));
            Assert.IsTrue(AssemblyScanning.IsSkipped(string.Empty));
        }

        [Test]
        public void ANearMissIsScanned() {
            Assert.IsFalse(AssemblyScanning.IsSkipped("SystemsKit"));
            Assert.IsFalse(AssemblyScanning.IsSkipped("Systems.Combat"));
            Assert.IsFalse(AssemblyScanning.IsSkipped("UnityEngineering"));
            Assert.IsFalse(AssemblyScanning.IsSkipped("Monopoly"));
            Assert.IsFalse(AssemblyScanning.IsSkipped("nunitools"));
            Assert.IsFalse(AssemblyScanning.IsSkipped("UnityCorp.Game"));
            Assert.IsFalse(AssemblyScanning.IsSkipped("Spellbound.Core"));
        }

        [Test]
        public void TheListingCallKeepsTheLineAndDropsTheEngine() {
            var names = AssemblyScanning.ScannableAssemblies().Select(assembly => assembly.GetName().Name).ToList();
            CollectionAssert.Contains(names, "Spellbound.Core");
            CollectionAssert.Contains(names, "Spellbound.Core.Editor");
            Assert.IsFalse(names.Any(name => name == "UnityEngine.CoreModule" || name == "mscorlib" || name == "System"));
        }
    }
}
