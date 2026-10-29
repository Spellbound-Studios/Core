// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using Spellbound.Core.Tooling;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Spellbound.Core.Controls.Editor {
    [CustomPropertyDrawer(typeof(SerializeReferenceDropdownAttribute))]
    public class TypePickerDrawer : PropertyDrawer {
        public const string ClassName = "sb-type-picker-drawer";
        public const string FieldsClassName = ClassName + "__fields";

        public override VisualElement CreatePropertyGUI(SerializedProperty property) {
            var root = CreateRoot();
            root.AddToClassList(ClassName);
            ControlStyleSheet.Apply(root);
            var baseType = ResolveBaseType(property);

            if (baseType == null) {
                root.Add(CreateUnresolvedNotice(property));
                return root;
            }

            var picker = CreatePicker(property.displayName, baseType);
            picker.SetValueWithoutNotify(property.managedReferenceValue?.GetType());
            var fields = CreateFields();
            fields.AddToClassList(FieldsClassName);
            root.Add(picker);
            root.Add(fields);

            picker.RegisterValueChangedCallback(changed => {
                property.serializedObject.Update();
                property.managedReferenceValue = changed.newValue != null
                    ? Activator.CreateInstance(changed.newValue)
                    : null;
                property.serializedObject.ApplyModifiedProperties();
                RebuildFields(property, fields);
            });

            root.TrackPropertyValue(property, tracked => {
                var current = tracked.managedReferenceValue?.GetType();

                if (picker.value != current) {
                    picker.SetValueWithoutNotify(current);
                    RebuildFields(tracked, fields);
                }
            });

            RebuildFields(property, fields);
            return root;
        }

        protected virtual VisualElement CreateRoot() {
            return new VisualElement();
        }

        protected virtual VisualElement CreateUnresolvedNotice(SerializedProperty property) {
            return new HelpBox(property.displayName + ": the declared type of this reference does not resolve.",
                HelpBoxMessageType.Warning);
        }

        protected virtual TypePickerField CreatePicker(string label, Type baseType) {
            return new TypePickerField(label, baseType);
        }

        protected virtual VisualElement CreateFields() {
            return new VisualElement();
        }

        protected virtual VisualElement CreateField(SerializedProperty child) {
            return new PropertyField(child);
        }

        private void RebuildFields(SerializedProperty property, VisualElement fields) {
            fields.Clear();

            if (property.managedReferenceValue == null) {
                return;
            }

            var end = property.GetEndProperty();
            var child = property.Copy();
            var enterChildren = true;

            while (child.NextVisible(enterChildren) && !SerializedProperty.EqualContents(child, end)) {
                enterChildren = false;
                fields.Add(CreateField(child.Copy()));
            }

            fields.Bind(property.serializedObject);
        }

        private static Type ResolveBaseType(SerializedProperty property) {
            if (property.propertyType != SerializedPropertyType.ManagedReference) {
                return null;
            }

            var typeName = property.managedReferenceFieldTypename;

            if (string.IsNullOrEmpty(typeName)) {
                return null;
            }

            var parts = typeName.Split(' ');
            return parts.Length == 2 ? Type.GetType(parts[1] + ", " + parts[0]) : null;
        }
    }
}
