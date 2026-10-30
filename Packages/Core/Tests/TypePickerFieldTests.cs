// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Collections.Generic;
using NUnit.Framework;
using Spellbound.Core.Controls.Editor;
using UnityEngine.UIElements;

namespace Spellbound.Core.Tests {
    public class TypePickerFieldTests {
        private abstract class Shape { }

        private sealed class Circle : Shape { }

        private sealed class Square : Shape { }

        private abstract class Polygon : Shape { }

        private sealed class Generic<T> : Shape { }

        [Test]
        public void ThePickerListsTheConcreteTypesSortedByName() {
            var field = new TypePickerField("Shape", typeof(Shape));
            Assert.IsTrue(field.ClassListContains(TypePickerField.ClassName));
            Assert.AreEqual(typeof(Shape), field.BaseType);
            Assert.AreEqual(new[] { typeof(Circle), typeof(Square) }, field.Choices);
            var dropdown = field.Q<DropdownField>();
            Assert.AreEqual(new List<string> { TypePickerField.NoneLabel, "Circle", "Square" }, dropdown.choices);
            Assert.AreEqual(TypePickerField.NoneLabel, dropdown.value);
        }

        [Test]
        public void TwoTypesWithOneShortNameAreToldApartByNamespaceAndPickedByType() {
            var field = new TypePickerField("Shape", typeof(TypePickerClashShape));
            Assert.AreEqual(new[] { typeof(PickerPlane.Square), typeof(PickerSolid.Square) }, field.Choices);
            var labels = new List<string> {
                TypePickerField.NoneLabel, "Square (Spellbound.Core.Tests.PickerPlane)",
                "Square (Spellbound.Core.Tests.PickerSolid)"
            };
            Assert.AreEqual(labels, field.Labels);
            var dropdown = field.Q<DropdownField>();
            Assert.AreEqual(labels, dropdown.choices);
            field.value = typeof(PickerSolid.Square);
            Assert.AreEqual(2, dropdown.index);
            Assert.AreEqual(labels[2], dropdown.value);
            field.value = typeof(PickerPlane.Square);
            Assert.AreEqual(1, dropdown.index);
        }

        [Test]
        public void ChoosingTheSecondOfTwoSameNamedTypesPicksTheSecond() {
            var field = new TypePickerField("Shape", typeof(TypePickerClashShape));
            var received = new List<Type>();
            field.RegisterValueChangedCallback(change => received.Add(change.newValue));
            var window = ControlTestWindow.Open(field);

            try {
                field.Q<DropdownField>().value = field.Labels[2];
                Assert.AreEqual(new[] { typeof(PickerSolid.Square) }, received);
                Assert.AreEqual(typeof(PickerSolid.Square), field.value);
            }
            finally {
                window.Close();
            }
        }

        [Test]
        public void AParameterlessPickerOffersNothingUntilABaseTypeIsNamed() {
            var field = new TypePickerField();
            Assert.IsNull(field.BaseType);
            Assert.IsNull(field.BaseTypeName);
            CollectionAssert.IsEmpty(field.Choices);
            Assert.AreEqual(new List<string> { TypePickerField.NoneLabel }, field.Q<DropdownField>().choices);
            field.BaseTypeName = typeof(Shape).AssemblyQualifiedName;
            Assert.AreEqual(typeof(Shape), field.BaseType);
            Assert.AreEqual(new[] { typeof(Circle), typeof(Square) }, field.Choices);
            Assert.IsTrue(field.styleSheets.Contains(ControlStyleSheet.Default));
        }

        [Test]
        public void TypePickerField_CustomImplementation() {
            var field = new BracketedPicker("Shape", typeof(TypePickerClashShape));
            Assert.AreEqual("Square [Spellbound.Core.Tests.PickerPlane]", field.Labels[1]);
            Assert.AreEqual("Square [Spellbound.Core.Tests.PickerSolid]", field.Labels[2]);
        }

        private sealed class BracketedPicker : TypePickerField {
            public BracketedPicker(string label, Type baseType) : base(label, baseType) { }

            protected override string QualifiedLabel(Type type, string label) {
                return label + " [" + type.Namespace + "]";
            }
        }

        [Test]
        public void ABaseTypeIsRequired() {
            Assert.Throws<ArgumentNullException>(() => new TypePickerField("Shape", null));
        }

        [Test]
        public void ACustomDisplayNameRenamesTheChoices() {
            var field = new TypePickerField("Shape", typeof(Shape));
            field.DisplayName = type => "shape " + type.Name;
            field.RefreshChoices();
            Assert.AreEqual(new List<string> { TypePickerField.NoneLabel, "shape Circle", "shape Square" },
                field.Q<DropdownField>().choices);
        }

        [Test]
        public void ACustomFilterNarrowsTheChoices() {
            var field = new TypePickerField("Shape", typeof(Shape));
            field.Filter = type => type == typeof(Circle);
            field.RefreshChoices();
            Assert.AreEqual(new[] { typeof(Circle) }, field.Choices);
            Assert.AreEqual(new List<string> { TypePickerField.NoneLabel, "Circle" }, field.Q<DropdownField>().choices);
        }

        [Test]
        public void SettingTheValueMovesTheDropdown() {
            var field = new TypePickerField("Shape", typeof(Shape));
            var dropdown = field.Q<DropdownField>();
            field.value = typeof(Square);
            Assert.AreEqual("Square", dropdown.value);
            field.value = null;
            Assert.AreEqual(TypePickerField.NoneLabel, dropdown.value);
        }

        [Test]
        public void ChoosingInTheDropdownRaisesOneChangeWithTheType() {
            var field = new TypePickerField("Shape", typeof(Shape));
            var received = new List<Type>();
            field.RegisterValueChangedCallback(change => received.Add(change.newValue));
            var window = ControlTestWindow.Open(field);

            try {
                Assert.IsNotNull(field.panel, "the test window gave the field no panel");
                field.Q<DropdownField>().value = "Circle";
                Assert.AreEqual(new[] { typeof(Circle) }, received);
                Assert.AreEqual(typeof(Circle), field.value);
            }
            finally {
                window.Close();
            }
        }
    }

    [System.Serializable]
    internal abstract class TypePickerClashShape { }
}

namespace Spellbound.Core.Tests.PickerPlane {
    [System.Serializable]
    internal sealed class Square : TypePickerClashShape {
        public int sides;
    }
}

namespace Spellbound.Core.Tests.PickerSolid {
    [System.Serializable]
    internal sealed class Square : TypePickerClashShape {
        public int sides;
    }
}
