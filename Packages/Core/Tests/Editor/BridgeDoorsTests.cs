// Copyright (c) Spellbound Studios. All rights reserved.

using NUnit.Framework;
using Spellbound.Core.Operations;
using Spellbound.Core.Tests.OperationFixture;
using Spellbound.Core.Tests.BridgeDoorFixture;

namespace Spellbound.Core.Tests {
    public class BridgeDoorsTests {
        [Test]
        public void ExpectedDoorNameFollowsTheAssemblyName() {
            Assert.AreEqual("GeoForgePlacesKit", BridgeDoors.ExpectedDoorName("Spellbound.GeoForge.Places"));
            Assert.AreEqual("CoreTestsKit", BridgeDoors.ExpectedDoorName("Spellbound.Core.Tests.BridgeDoorFixture"));
            Assert.IsNull(BridgeDoors.ExpectedDoorName("Spellbound.Core"));
            Assert.IsNull(BridgeDoors.ExpectedDoorName("Unity.Pipeline.Attributes"));
        }

        [Test]
        public void FindListsTheFixtureDoorWithSortedSignatures() {
            var doors = BridgeDoors.Find(new[] { typeof(CoreTestsKit).Assembly, typeof(FixtureOperations).Assembly, typeof(CoreKit).Assembly });
            Assert.AreEqual(1, doors.Count);
            var door = doors[0];
            Assert.AreEqual("Spellbound.Core.Tests.BridgeDoorFixture", door.AssemblyName);
            Assert.AreEqual("Core", door.PackageName);
            Assert.AreEqual("Tests", door.OtherName);
            Assert.AreEqual("CoreTestsKit", door.TypeName);
            CollectionAssert.AreEqual(new[] {
                "static List<string> Names(string prefix, int count = 3)",
                "static bool TryBuild(float size, out CoreTestsPart part)",
                "static int Count { get; }"
            }, door.Members);
        }

        [Test]
        public void FindOverTheLoadedAssembliesSkipsTheTestFixtureAndAnExplicitListKeepsIt() {
            var listed = false;

            foreach (var door in BridgeDoors.Find()) {
                listed |= door.TypeName == "CoreTestsKit";
            }

            Assert.IsFalse(listed);
            var explicitDoors = BridgeDoors.Find(new[] { typeof(CoreTestsKit).Assembly });
            Assert.AreEqual(1, explicitDoors.Count);
            Assert.AreEqual("CoreTestsKit", explicitDoors[0].TypeName);
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
