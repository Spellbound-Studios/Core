// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using Spellbound.Core.Tooling;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Spellbound.Core.Controls.Editor {
    [CustomPropertyDrawer(typeof(SerializeReferenceDropdownAttribute))]
    public sealed class TypePickerDrawer : PropertyDrawer {
        public override VisualElement CreatePropertyGUI(SerializedProperty property) {
            var root = new VisualElement();
            var baseType = ResolveBaseType(property);

            if (baseType == null) {
                root.Add(new HelpBox(property.displayName + ": the declared type of this reference does not resolve.",
                    HelpBoxMessageType.Warning));
                return root;
            }

            var picker = new TypePickerField(property.displayName, baseType);
            picker.SetValueWithoutNotify(property.managedReferenceValue?.GetType());
            var fields = new VisualElement { style = { marginLeft = 14 } };
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

        private static void RebuildFields(SerializedProperty property, VisualElement fields) {
            fields.Clear();

            if (property.managedReferenceValue == null) {
                return;
            }

            var end = property.GetEndProperty();
            var child = property.Copy();
            var enterChildren = true;

            while (child.NextVisible(enterChildren) && !SerializedProperty.EqualContents(child, end)) {
                enterChildren = false;
                fields.Add(new PropertyField(child.Copy()));
            }

            fields.Bind(property.serializedObject);
        }

        private static Type ResolveBaseType(SerializedProperty property) {
            var typeName = property.managedReferenceFieldTypename;

            if (string.IsNullOrEmpty(typeName)) {
                return null;
            }

            var parts = typeName.Split(' ');
            return parts.Length == 2 ? Type.GetType(parts[1] + ", " + parts[0]) : null;
        }
    }
}
