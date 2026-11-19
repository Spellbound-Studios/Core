// Copyright (c) Spellbound Studios. All rights reserved.

using System.Collections.Generic;
using NUnit.Framework;
using Spellbound.Core.Authoring.Editor;
using Spellbound.Core.Tests.AuthoringFixture;

namespace Spellbound.Core.Tests {
    public class AuthoredFieldsTests {
        [Test]
        public void TheFixtureDefinitionListsItsFieldsSortedByName() {
            Assert.AreEqual(new[] {
                "FixtureDefinition.threshold : int",
                "FixtureDefinition.throwOnValidate : bool",
                "FixtureDefinition.title : string"
            }, Lines(typeof(FixtureDefinition)));
        }

        [Test]
        public void APrivateSerializedFieldIsListed() {
            CollectionAssert.Contains(Lines(typeof(FixtureLayeredDefinition)),
                "FixtureLayeredDefinition.caption : string");
        }

        [Test]
        public void AManagedReferenceListIsOneLine() {
            Assert.AreEqual(new[] {
                "FixtureLayeredDefinition.caption : string",
                "FixtureLayeredDefinition.layers : " + AuthoredFields.ManagedReferenceListType
            }, Lines(typeof(FixtureLayeredDefinition)));
            Assert.IsTrue(AuthoredFields.Describe(typeof(FixtureLayeredDefinition))[1].IsManagedReference);
        }

        [Test]
        public void AFieldHiddenFromTheInspectorIsStillListed() {
            Assert.AreEqual(new[] {
                "FixtureHiddenFieldDefinition.hidden : int",
                "FixtureHiddenFieldDefinition.shown : string"
            }, Lines(typeof(FixtureHiddenFieldDefinition)));
        }

        [Test]
        public void ARenamedFieldCarriesItsFormerName() {
            var fields = AuthoredFields.Describe(typeof(FixtureRenamedDefinition));
            Assert.AreEqual(1, fields.Count);
            Assert.AreEqual("FixtureRenamedDefinition.label : string", fields[0].Line);
            Assert.AreEqual(new[] { "oldName" }, fields[0].FormerNames);
        }

        [Test]
        public void ASecondDescribeReusesTheCacheUntilItIsCleared() {
            AuthoredFields.ClearCache();
            var first = AuthoredFields.Describe(typeof(FixtureDefinition));
            Assert.AreSame(first, AuthoredFields.Describe(typeof(FixtureDefinition)));
            AuthoredFields.ClearCache();
            Assert.AreNotSame(first, AuthoredFields.Describe(typeof(FixtureDefinition)));
        }

        [Test]
        public void ATypeThatIsNotAScriptableObjectListsNothing() {
            Assert.IsEmpty(AuthoredFields.Describe(typeof(FixtureLayer)));
            Assert.IsEmpty(AuthoredFields.Describe(typeof(string)));
        }

        private static List<string> Lines(System.Type type) {
            var lines = new List<string>();

            foreach (var field in AuthoredFields.Describe(type)) {
                lines.Add(field.Line);
            }

            return lines;
        }
    }
}
