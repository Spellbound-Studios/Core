// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Collections.Generic;
using Spellbound.Core.Controls.Editor;
using Spellbound.Core.Modules;
using Spellbound.Core.Objects;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Spellbound.Core {
    [CustomEditor(typeof(ObjectPreset))]
    public sealed class ObjectPresetEditor : UnityEditor.Editor {
        public const string ClassName = "sb-object-preset";
        public const string HeadingClassName = ClassName + "__heading";
        public const string SurfacesClassName = ClassName + "__surfaces";
        public const string AddSurfaceClassName = ClassName + "__add-surface";
        public const string SurfaceClassName = ClassName + "__surface";
        public const string RemoveSurfaceClassName = ClassName + "__remove-surface";
        public const string ModulesClassName = ClassName + "__modules";
        public const string AddModuleClassName = ClassName + "__add-module";
        public const string ModuleClassName = ClassName + "__module";
        public const string ModuleHeaderClassName = ClassName + "__module-header";
        public const string ModuleTitleClassName = ClassName + "__module-title";
        public const string RemoveModuleClassName = ClassName + "__remove-module";
        public const string ModuleEmptyClassName = ClassName + "__module-empty";

        private const string FieldSurfaceModules = "surfaceModules";
        private const string FieldPresetModules = "presetModules";
        private const string FieldSurfaceName = "surfaceName";

        public override VisualElement CreateInspectorGUI() {
            var root = new VisualElement();
            root.AddToClassList(ClassName);
            EditorLook.Dress(root);
            BuildDefaultFields(root);
            var heading = new Label("Surface Modules");
            heading.AddToClassList(HeadingClassName);
            root.Add(heading);
            var surfacesProp = serializedObject.FindProperty(FieldSurfaceModules);

            if (surfacesProp == null) {
                root.Add(new HelpBox(
                    $"ObjectPreset has no '{FieldSurfaceModules}' field: editor field name has drifted.",
                    HelpBoxMessageType.Error));

                return root;
            }

            var surfacesContainer = new VisualElement();
            surfacesContainer.AddToClassList(SurfacesClassName);
            root.Add(surfacesContainer);

            void RenderAllSurfaces() {
                serializedObject.Update();
                surfacesContainer.Clear();

                for (var i = 0; i < surfacesProp.arraySize; i++)
                    surfacesContainer.Add(BuildSurfaceCard(surfacesProp, i, RenderAllSurfaces));
            }

            RenderAllSurfaces();

            var addSurfaceBtn = new Button(() => {
                surfacesProp.InsertArrayElementAtIndex(surfacesProp.arraySize);
                var newSurface = surfacesProp.GetArrayElementAtIndex(surfacesProp.arraySize - 1);
                newSurface.FindPropertyRelative(FieldSurfaceName).stringValue = "New Surface";
                newSurface.FindPropertyRelative(FieldPresetModules).ClearArray();
                serializedObject.ApplyModifiedProperties();
                PersistStructuralChange();
                RenderAllSurfaces();
            }) {
                text = "Add Surface"
            };

            addSurfaceBtn.AddToClassList(AddSurfaceClassName);
            root.Add(addSurfaceBtn);

            return root;
        }

        private void BuildDefaultFields(VisualElement root) {
            var iterator = serializedObject.GetIterator();

            if (!iterator.NextVisible(true))
                return;

            do {
                if (iterator.name == "m_Script")
                    continue;

                if (iterator.name == FieldSurfaceModules)
                    continue;

                var field = new PropertyField(iterator.Copy());
                field.Bind(serializedObject);
                root.Add(field);
            } while (iterator.NextVisible(false));
        }

        private VisualElement BuildSurfaceCard(SerializedProperty surfacesProp, int index, Action onChanged) {
            var capturedIndex = index;
            var surfaceProp = surfacesProp.GetArrayElementAtIndex(index);
            var nameProp = surfaceProp.FindPropertyRelative(FieldSurfaceName);
            var modulesProp = surfaceProp.FindPropertyRelative(FieldPresetModules);

            var card = new SectionCard(nameProp.stringValue);
            card.AddToClassList(SurfaceClassName);
            card.TrackPropertyValue(nameProp, renamed => card.Title = renamed.stringValue);

            var nameField = new PropertyField(nameProp, $"Surface {index}");
            nameField.Bind(serializedObject);
            card.Add(nameField);

            var removeSurfaceBtn = new Button(() => {
                surfacesProp.DeleteArrayElementAtIndex(capturedIndex);
                serializedObject.ApplyModifiedProperties();
                PersistStructuralChange();
                onChanged();
            }) {
                text = "Remove Surface"
            };

            removeSurfaceBtn.AddToClassList(RemoveSurfaceClassName);
            card.Header.Add(removeSurfaceBtn);

            var modulesContainer = new VisualElement();
            modulesContainer.AddToClassList(ModulesClassName);
            card.Add(modulesContainer);

            void RenderAllModules() {
                serializedObject.Update();
                modulesContainer.Clear();

                for (var j = 0; j < modulesProp.arraySize; j++)
                    modulesContainer.Add(BuildModuleRow(modulesProp, j, RenderAllModules));
            }

            RenderAllModules();

            var addModuleBtn = new Button(() => ShowModuleAddMenu(modulesProp, RenderAllModules)) {
                text = "Add Module"
            };

            addModuleBtn.AddToClassList(AddModuleClassName);
            card.Add(addModuleBtn);

            return card;
        }

        private VisualElement BuildModuleRow(SerializedProperty modulesProp, int index, Action onChanged) {
            var capturedIndex = index;
            var moduleProp = modulesProp.GetArrayElementAtIndex(index);
            var row = new VisualElement();
            row.AddToClassList(ModuleClassName);
            var header = new VisualElement();
            header.AddToClassList(ModuleHeaderClassName);
            var typeLabel = new Label(GetNiceTypeName(moduleProp));
            typeLabel.AddToClassList(ModuleTitleClassName);
            header.Add(typeLabel);

            var removeBtn = new Button(() => {
                modulesProp.DeleteArrayElementAtIndex(capturedIndex);
                serializedObject.ApplyModifiedProperties();
                PersistStructuralChange();
                onChanged();
            }) {
                text = "Remove"
            };

            removeBtn.AddToClassList(RemoveModuleClassName);
            header.Add(removeBtn);
            row.Add(header);

            if (moduleProp.managedReferenceValue == null) {
                var emptyLabel = new Label("(null managed reference)");
                emptyLabel.AddToClassList(ModuleEmptyClassName);
                row.Add(emptyLabel);

                return row;
            }

            var moduleField = new PropertyField(moduleProp, string.Empty);
            moduleField.Bind(serializedObject);
            row.Add(moduleField);

            return row;
        }

        private void ShowModuleAddMenu(SerializedProperty modulesProp, Action onChanged) {
            var existing = new HashSet<Type>();

            for (var i = 0; i < modulesProp.arraySize; i++) {
                var el = modulesProp.GetArrayElementAtIndex(i).managedReferenceValue;

                if (el != null)
                    existing.Add(el.GetType());
            }

            var menu = new GenericMenu();
            var anyAvailable = false;

            foreach (var type in TypeCache.GetTypesDerivedFrom<PresetModule>()) {
                if (type.IsAbstract)
                    continue;

                anyAvailable = true;
                var capturedType = type;
                var label = new GUIContent(type.Name);

                if (existing.Contains(type)) {
                    menu.AddDisabledItem(label);

                    continue;
                }

                menu.AddItem(label, false, () => {
                    modulesProp.arraySize++;
                    var element = modulesProp.GetArrayElementAtIndex(modulesProp.arraySize - 1);
                    element.managedReferenceValue = Activator.CreateInstance(capturedType);
                    serializedObject.ApplyModifiedProperties();
                    PersistStructuralChange();
                    onChanged();
                });
            }

            if (!anyAvailable)
                menu.AddDisabledItem(new GUIContent("No PresetModule types found"));

            menu.ShowAsContext();
        }

        private void PersistStructuralChange() {
            EditorUtility.SetDirty(target);
            AssetDatabase.SaveAssetIfDirty(target);
        }

        private static string GetNiceTypeName(SerializedProperty prop) {
            if (prop.managedReferenceValue == null)
                return "Missing";

            var full = prop.managedReferenceFullTypename;
            var lastSpace = full.LastIndexOf(' ');

            if (lastSpace >= 0)
                full = full[(lastSpace + 1)..];

            var lastDot = full.LastIndexOf('.');

            return lastDot >= 0
                    ? full[(lastDot + 1)..]
                    : full;
        }
    }
}
