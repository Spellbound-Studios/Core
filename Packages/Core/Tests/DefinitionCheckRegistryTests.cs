// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Collections.Generic;
using NUnit.Framework;
using Spellbound.Core.Authoring;
using Spellbound.Core.Authoring.Editor;
using Spellbound.Core.Logging;
using Spellbound.Core.Tests.AuthoringFixture;
using UnityEditor;
using UnityEngine;

namespace Spellbound.Core.Tests {
    public class DefinitionCheckRegistryTests {
        public const string FixtureCheckTypeName = "Spellbound.Core.Tests.AuthoringFixture.Check.FixtureThresholdCheck";
        public const string FixtureCheckName = "fixturecheck.threshold-high";

        [Test]
        public void TypeCacheFindsAnAttributedClassThatImplementsAClosedGenericInterface() {
            Type found = null;

            foreach (var type in TypeCache.GetTypesWithAttribute<RegisteredDefinitionCheckAttribute>()) {
                if (type.FullName == FixtureCheckTypeName) {
                    found = type;
                }
            }

            Assert.IsNotNull(found, "TypeCache did not return " + FixtureCheckTypeName);
            var definitionTypes = DefinitionCheckDescription.DefinitionTypesOf(found);
            Assert.AreEqual(1, definitionTypes.Count);
            Assert.AreEqual(typeof(FixtureDefinition), definitionTypes[0]);
        }

        [Test]
        public void TheTestAssemblyDoesNotReferenceTheCheckAssembly() {
            foreach (var referenced in typeof(DefinitionCheckRegistryTests).Assembly.GetReferencedAssemblies()) {
                Assert.AreNotEqual("Spellbound.Core.Tests.AuthoringFixture.Check", referenced.Name);
            }
        }

        [Test]
        public void TheDefaultRegistryReadsTheDefinitionTypeFromTheInterface() {
            var check = DefinitionCheckRegistry.Default.Find(FixtureCheckName);
            Assert.IsNotNull(check);
            Assert.AreEqual(typeof(FixtureDefinition), check.DefinitionType);
            Assert.AreEqual(FixtureCheckTypeName, check.CheckType.FullName);
            Assert.IsTrue(check.RunsWhileEditing);
            Assert.AreEqual("Core", check.Package);
            Assert.AreEqual(FixtureCheckName + " (Core)", check.Label);
        }

        [Test]
        public void TheMarkedAssemblyScanIsTheFallbackAndFindsTheSameClass() {
            var fallback = new DefinitionCheckRegistry(DefinitionCheckRegistry.MarkedAssemblyTypes);
            var check = fallback.Find(FixtureCheckName);
            Assert.IsNotNull(check);
            Assert.AreEqual(typeof(FixtureDefinition), check.DefinitionType);
        }

        [Test]
        public void ForAnswersEveryCheckWhoseTypeTheAssetIsAssignableTo() {
            var registry = new DefinitionCheckRegistry(new[] {
                new DefinitionCheckDescription("test.over-everything", false, new OverEverything(), typeof(ScriptableObject)),
                new DefinitionCheckDescription("test.over-fixture", true, new OverFixture(), typeof(FixtureDefinition)),
                new DefinitionCheckDescription("test.over-settings", false, new OverSettings(), typeof(Spellbound.Core.Operations.OperationSettings))
            });
            var applying = registry.For(typeof(FixtureDefinition));
            Assert.AreEqual(2, applying.Count);
            Assert.AreEqual("test.over-everything", applying[0].Name);
            Assert.AreEqual("test.over-fixture", applying[1].Name);
            Assert.AreEqual(2, registry.For(typeof(Spellbound.Core.Operations.OperationSettings)).Count);
            Assert.AreEqual(0, registry.For(typeof(string)).Count);
        }

        [Test]
        public void ADuplicateNameOverOneTypeKeepsTheFirstAndLogsAnError() {
            using (Log.SuspendSinks()) {
                var registry = new DefinitionCheckRegistry(new[] {
                    new DefinitionCheckDescription("test.twice", false, new OverFixture(), typeof(FixtureDefinition)),
                    new DefinitionCheckDescription("test.twice", false, new OverFixtureAgain(), typeof(FixtureDefinition))
                });
                Assert.AreEqual(1, registry.Checks.Count);
                Assert.AreEqual(typeof(OverFixture), registry.Checks[0].CheckType);
            }
        }

        [Test]
        public void ADescriptionRefusesAClassThatDoesNotImplementTheInterfaceForTheType() {
            Assert.Throws<ArgumentException>(() => new DefinitionCheckDescription("test.wrong", false, new OverFixture(), typeof(ScriptableObject)));
            Assert.Throws<ArgumentException>(() => new DefinitionCheckDescription("NotACode", false, new OverFixture(), typeof(FixtureDefinition)));
        }

        [Test]
        public void TheAttributeRefusesANameThatIsNotACode() {
            Assert.Throws<ArgumentException>(() => new RegisteredDefinitionCheckAttribute("NotACode"));
            Assert.IsFalse(new RegisteredDefinitionCheckAttribute("test.fine").RunsWhileEditing);
        }

        [Test]
        public void PreviewLinesDefaultToNone() {
            var description = new DefinitionCheckDescription("test.over-fixture", false, new OverFixture(), typeof(FixtureDefinition));
            var asset = ScriptableObject.CreateInstance<FixtureDefinition>();

            try {
                Assert.AreEqual(0, description.PreviewLines(asset).Count);
                var report = new ValidationReport("memory");
                description.Run(asset, report);
                Assert.AreEqual("test.over-fixture", report.Findings[0].Code);
            }
            finally {
                UnityEngine.Object.DestroyImmediate(asset);
            }
        }

        private sealed class OverEverything : IDefinitionCheck<ScriptableObject> {
            public void Check(ScriptableObject definition, ValidationReport report) { }
        }

        private sealed class OverFixture : IDefinitionCheck<FixtureDefinition> {
            public void Check(FixtureDefinition definition, ValidationReport report) {
                report.Information("test.over-fixture", "seen");
            }
        }

        private sealed class OverFixtureAgain : IDefinitionCheck<FixtureDefinition> {
            public void Check(FixtureDefinition definition, ValidationReport report) { }
        }

        private sealed class OverSettings : IDefinitionCheck<Spellbound.Core.Operations.OperationSettings> {
            public void Check(Spellbound.Core.Operations.OperationSettings definition, ValidationReport report) { }
        }
    }
}
