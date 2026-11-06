// Copyright (c) Spellbound Studios. All rights reserved.

using System.Linq;
using NUnit.Framework;
using Spellbound.Core.Operations;
using Spellbound.Core.Operations.Editor;
using Spellbound.Fixture.Other;

namespace Spellbound.Core.Tests {
    public class EditorAssembliesTests {
        [Test]
        public void AnAssemblyConstrainedOnTestsIsRecognisedByItsDefinitionText() {
            Assert.IsTrue(EditorAssemblies.IsConstrainedOnTests(
                "{\n  \"name\": \"Fixture\",\n  \"defineConstraints\": [\n    \"UNITY_INCLUDE_TESTS\"\n  ]\n}"));
            Assert.IsTrue(EditorAssemblies.IsConstrainedOnTests(
                "{ \"defineConstraints\": [ \"!UNITY_SERVER\", \"UNITY_INCLUDE_TESTS\" ] }"));
            Assert.IsTrue(EditorAssemblies.IsConstrainedOnTests(
                "{ \"defineConstraints\": [ \"UNITY_INCLUDE_TESTS || SPELLBOUND_FIXTURE\" ] }"));
            Assert.IsTrue(EditorAssemblies.IsConstrainedOnTests(
                "{ \"optionalUnityReferences\": [ \"TestAssemblies\" ], \"defineConstraints\": [] }"));
            Assert.IsFalse(EditorAssemblies.IsConstrainedOnTests("{ \"defineConstraints\": [] }"));
            Assert.IsFalse(EditorAssemblies.IsConstrainedOnTests("{ \"defineConstraints\": [ \"!UNITY_INCLUDE_TESTS\" ] }"));
            Assert.IsFalse(EditorAssemblies.IsConstrainedOnTests("{ \"defineConstraints\": [ \"UNITY_INCLUDE_TESTS_LATER\" ] }"));
            Assert.IsFalse(EditorAssemblies.IsConstrainedOnTests("{ \"name\": \"UNITY_INCLUDE_TESTS\" }"));
            Assert.IsFalse(EditorAssemblies.IsConstrainedOnTests(null));
        }

        [Test]
        public void TheFixtureDoorAssemblyIsATestAssemblyAndCoreIsNot() {
            Assert.IsTrue(EditorAssemblies.IsTestAssembly(typeof(FixtureOtherKit).Assembly.GetName().Name));
            Assert.IsFalse(EditorAssemblies.IsTestAssembly(typeof(CoreKit).Assembly.GetName().Name));
            Assert.IsFalse(EditorAssemblies.IsTestAssembly("Spellbound.Nowhere"));
            Assert.IsFalse(EditorAssemblies.IsTestAssembly(null));
        }

        [Test]
        public void TheDefaultDoorListingSkipsTestAssembliesAndAnExplicitListDoesNot() {
            Assert.IsNotNull(BridgeDoors.AssemblySource);
            CollectionAssert.DoesNotContain(EditorAssemblies.WithoutTests().Select(assembly => assembly.GetName().Name).ToList(),
                typeof(FixtureOtherKit).Assembly.GetName().Name);
            Assert.IsFalse(BridgeDoors.Find().Any(door => door.TypeName == nameof(FixtureOtherKit)));
            Assert.IsTrue(BridgeDoors.Find(new[] { typeof(FixtureOtherKit).Assembly }).Any(door => door.TypeName == nameof(FixtureOtherKit)));
        }

        [Test]
        public void AssemblySource_CustomImplementation() {
            var previous = BridgeDoors.AssemblySource;

            try {
                BridgeDoors.AssemblySource = () => new[] { typeof(FixtureOtherKit).Assembly };
                Assert.IsTrue(BridgeDoors.Find().Any(door => door.TypeName == nameof(FixtureOtherKit)));
                BridgeDoors.AssemblySource = null;
                Assert.IsTrue(BridgeDoors.Find().Any(door => door.TypeName == nameof(FixtureOtherKit)));
            }
            finally {
                BridgeDoors.AssemblySource = previous;
            }
        }
    }
}
