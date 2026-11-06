// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using NUnit.Framework;
using Spellbound.Core.Controls.Editor;
using Spellbound.Core.Tooling;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Spellbound.Core.Tests {
    public class TypePickerDrawerTests {
        private sealed class DrawerHost : ScriptableObject {
            [SerializeReference] [SerializeReferenceDropdown] public TypePickerClashShape shape;
            public int plain;
        }

        private sealed class TaggedDrawer : TypePickerDrawer {
            protected override VisualElement CreateRoot() => new VisualElement { name = "tagged-root" };

            protected override VisualElement CreateUnresolvedNotice(SerializedProperty property) =>
                new Label("unresolved " + property.name) { name = "tagged-notice" };

            protected override TypePickerField CreatePicker(string label, Type baseType) =>
                new TypePickerField(label, baseType) { name = "tagged-picker" };

            protected override VisualElement CreateFields() => new VisualElement { name = "tagged-fields" };

            protected override VisualElement CreateField(SerializedProperty child) =>
                new Label(child.name) { name = "tagged-field" };
        }

        private DrawerHost _host;
        private SerializedObject _serialized;

        [SetUp]
        public void SetUp() {
            _host = ScriptableObject.CreateInstance<DrawerHost>();
            _host.shape = new PickerSolid.Square { sides = 4 };
            _serialized = new SerializedObject(_host);
        }

        [TearDown]
        public void TearDown() {
            _serialized.Dispose();
            UnityEngine.Object.DestroyImmediate(_host);
        }

        [Test]
        public void TheDrawerBuildsAPickerOverTheDeclaredBaseTypeAndTheFieldsOfTheValue() {
            var root = new TypePickerDrawer().CreatePropertyGUI(_serialized.FindProperty(nameof(DrawerHost.shape)));
            Assert.IsTrue(root.ClassListContains(TypePickerDrawer.ClassName));
            Assert.IsTrue(root.styleSheets.Contains(ControlStyleSheet.Default));
            var picker = root.Q<TypePickerField>();
            Assert.AreEqual(typeof(TypePickerClashShape), picker.BaseType);
            Assert.AreEqual(typeof(PickerSolid.Square), picker.value);
            var fields = root.Q<VisualElement>(className: TypePickerDrawer.FieldsClassName);
            Assert.AreEqual(1, fields.childCount);
        }

        [Test]
        public void TypePickerDrawer_CustomImplementation() {
            var drawer = new TaggedDrawer();
            var root = drawer.CreatePropertyGUI(_serialized.FindProperty(nameof(DrawerHost.shape)));
            Assert.AreEqual("tagged-root", root.name);
            Assert.AreEqual("tagged-picker", root.Q<TypePickerField>().name);
            var fields = root.Q<VisualElement>(className: TypePickerDrawer.FieldsClassName);
            Assert.AreEqual("tagged-fields", fields.name);
            Assert.AreEqual("tagged-field", fields[0].name);
            Assert.AreEqual(nameof(PickerSolid.Square.sides), ((Label)fields[0]).text);
            var notice = drawer.CreatePropertyGUI(_serialized.FindProperty(nameof(DrawerHost.plain)));
            Assert.AreEqual("tagged-notice", notice[0].name);
        }
    }
}
