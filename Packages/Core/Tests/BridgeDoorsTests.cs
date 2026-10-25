// Copyright (c) Spellbound Studios. All rights reserved.

using NUnit.Framework;
using Spellbound.Core.Operations;
using Spellbound.Core.Tests.OperationFixture;
using Spellbound.Fixture.Other;

namespace Spellbound.Core.Tests {
    public class BridgeDoorsTests {
        [Test]
        public void ExpectedDoorNameFollowsTheAssemblyName() {
            Assert.AreEqual("GeoForgePlacesKit", BridgeDoors.ExpectedDoorName("Spellbound.GeoForge.Places"));
            Assert.AreEqual("FixtureOtherKit", BridgeDoors.ExpectedDoorName("Spellbound.Fixture.Other"));
            Assert.IsNull(BridgeDoors.ExpectedDoorName("Spellbound.Core"));
            Assert.IsNull(BridgeDoors.ExpectedDoorName("Unity.Pipeline.Attributes"));
        }

        [Test]
        public void FindListsTheFixtureDoorWithSortedSignatures() {
            var doors = BridgeDoors.Find(new[] { typeof(FixtureOtherKit).Assembly, typeof(FixtureOperations).Assembly, typeof(CoreKit).Assembly });
            Assert.AreEqual(1, doors.Count);
            var door = doors[0];
            Assert.AreEqual("Spellbound.Fixture.Other", door.AssemblyName);
            Assert.AreEqual("Fixture", door.PackageName);
            Assert.AreEqual("Other", door.OtherName);
            Assert.AreEqual("FixtureOtherKit", door.TypeName);
            CollectionAssert.AreEqual(new[] {
                "static List<string> Names(string prefix, int count = 3)",
                "static bool TryBuild(float size, out FixtureOtherPart part)",
                "static int Count { get; }"
            }, door.Members);
        }

        [Test]
        public void FindOverTheLoadedAssembliesIncludesTheFixture() {
            var found = false;

            foreach (var door in BridgeDoors.Find()) {
                found |= door.TypeName == "FixtureOtherKit";
            }

            Assert.IsTrue(found);
        }

        [Test]
        public void TypeNamesReadAsCSharp() {
            Assert.AreEqual("int?", BridgeDoors.TypeName(typeof(int?)));
            Assert.AreEqual("string[]", BridgeDoors.TypeName(typeof(string[])));
            Assert.AreEqual("Dictionary<string, float>", BridgeDoors.TypeName(typeof(System.Collections.Generic.Dictionary<string, float>)));
            Assert.AreEqual("void", BridgeDoors.TypeName(typeof(void)));
        }
    }
}
