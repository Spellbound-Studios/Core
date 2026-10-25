// Copyright 2026 Spellbound Studio Inc.

using System;
using System.Collections.Generic;
using Spellbound.Core.Tooling;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Spellbound.Core {
    /// <summary>
    /// UI Toolkit drawer for <see cref="SerializeReferenceDropdownAttribute"/>: a dropdown of every
    /// concrete type derived from the field's declared base, with the chosen instance's serialized
    /// fields rendered beneath it.
    /// </summary>
    [CustomPropertyDrawer(typeof(SerializeReferenceDropdownAttribute))]
    public sealed class SerializeReferenceDropdownDrawer : PropertyDrawer {
        private const string NoneChoice = "None";

        public override VisualElement CreatePropertyGUI(SerializedProperty property) {
            var root = new VisualElement();
            var typesByName = new Dictionary<string, Type>();
            var choices = new List<string> { NoneChoice };
            var baseType = ResolveBaseType(property);

            if (baseType != null) {
                foreach (var type in TypeCache.GetTypesDerivedFrom(baseType)) {
                    if (type.IsAbstract || type.IsGenericTypeDefinition)
                        continue;

                    typesByName[type.Name] = type;
                    choices.Add(type.Name);
                }
            }

            var dropdown = new DropdownField(property.displayName, choices, CurrentChoice(property));
            var fields = new VisualElement { style = { marginLeft = 14 } };
            root.Add(dropdown);
            root.Add(fields);

            dropdown.RegisterValueChangedCallback(changed => {
                property.serializedObject.Update();
                property.managedReferenceValue =
                        changed.newValue != NoneChoice && typesByName.TryGetValue(changed.newValue, out var picked)
                                ? Activator.CreateInstance(picked)
                                : null;
                property.serializedObject.ApplyModifiedProperties();
                RebuildFields(property, fields);
            });

            root.TrackPropertyValue(property, tracked => {
                var choice = CurrentChoice(tracked);

                if (dropdown.value != choice) {
                    dropdown.SetValueWithoutNotify(choice);
                    RebuildFields(tracked, fields);
                }
            });

            RebuildFields(property, fields);

            return root;
        }

        private static string CurrentChoice(SerializedProperty property) =>
                property.managedReferenceValue?.GetType().Name ?? NoneChoice;

        private static void RebuildFields(SerializedProperty property, VisualElement fields) {
            fields.Clear();

            if (property.managedReferenceValue == null)
                return;

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
            var typename = property.managedReferenceFieldTypename;

            if (string.IsNullOrEmpty(typename))
                return null;

            var parts = typename.Split(' ');

            return parts.Length == 2 ? Type.GetType($"{parts[1]}, {parts[0]}") : null;
        }
    }
}
