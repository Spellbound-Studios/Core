// Copyright 2026 Spellbound Studio Inc.

using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Spellbound.Core.Tooling {
    /// <summary>
    /// This class adds the [Immutable] tag to something that is exposed in the inspector, but you don't want modified.
    /// It will gray out the field and make it unselectable.
    /// </summary>
    public class ImmutableAttribute : PropertyAttribute { }

#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(ImmutableAttribute))]
    public class ImmutableDrawer : PropertyDrawer {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            var old = GUI.enabled;

            if (old)
                GUI.enabled = false;

            EditorGUI.PropertyField(position, property, label);

            if (old)
                GUI.enabled = true;
        }
    }
#endif
}