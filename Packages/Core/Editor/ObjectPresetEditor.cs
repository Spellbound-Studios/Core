// Copyright 2026 Spellbound Studio Inc.

using System;
using System.Collections.Generic;
using Spellbound.Core.Modules;
using Spellbound.Core.Objects;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Spellbound.Core {
    /// <summary>
    /// UI Toolkit inspector for <see cref="ObjectPreset"/>. Renders the default fields, then a custom block of
    /// surface modules where each surface is a card containing a header (name + remove button) and a list of
    /// <see cref="PresetModule"/>s. Module rows route to whatever <c>CustomPropertyDrawer</c> is registered for
    /// the concrete module type — that's how DamageableModule's bespoke layout shows up.
    /// </summary>
    [CustomEditor(typeof(ObjectPreset))]
    public sealed class ObjectPresetEditor : Editor {
        private const string FieldSurfaceModules = "surfaceModules";
        private const string FieldPresetModules = "presetModules";
        private const string FieldSurfaceName = "surfaceName";

        public override VisualElement CreateInspectorGUI() {
            var root = new VisualElement();
            root.style.marginTop = 4;

            BuildDefaultFields(root);
            root.Add(Spacer(8));

            var header = new Label("Surface Modules") {
                style = {
                    unityFontStyleAndWeight = FontStyle.Bold,
                    fontSize = 14,
                    marginBottom = 4
                }
            };

            root.Add(header);

            var surfacesProp = serializedObject.FindProperty(FieldSurfaceModules);

            if (surfacesProp == null) {
                root.Add(new HelpBox(
                    $"ObjectPreset has no '{FieldSurfaceModules}' field — editor field name has drifted.",
                    HelpBoxMessageType.Error));

                return root;
            }

            var surfacesContainer = new VisualElement();
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
                text = "Add Surface",
                style = { marginTop = 6 }
            };

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

            var surfaceAccent = new Color(0.4f, 0.6f, 0.9f);

            var card = new VisualElement {
                style = {
                    borderLeftWidth = 4,
                    borderLeftColor = surfaceAccent,
                    borderTopWidth = 1,
                    borderRightWidth = 1,
                    borderBottomWidth = 1,
                    borderTopColor = new Color(surfaceAccent.r, surfaceAccent.g, surfaceAccent.b, 0.3f),
                    borderRightColor = new Color(surfaceAccent.r, surfaceAccent.g, surfaceAccent.b, 0.3f),
                    borderBottomColor = new Color(surfaceAccent.r, surfaceAccent.g, surfaceAccent.b, 0.3f),
                    backgroundColor = new Color(0f, 0f, 0f, 0.04f),
                    paddingTop = 8,
                    paddingBottom = 8,
                    paddingLeft = 10,
                    paddingRight = 10,
                    marginBottom = 12
                }
            };

            var header = new VisualElement {
                style = {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    marginBottom = 4
                }
            };

            var nameField = new PropertyField(nameProp, $"Surface {index}");
            nameField.style.flexGrow = 1;
            nameField.Bind(serializedObject);
            header.Add(nameField);

            var removeSurfaceBtn = new Button(() => {
                surfacesProp.DeleteArrayElementAtIndex(capturedIndex);
                serializedObject.ApplyModifiedProperties();
                PersistStructuralChange();
                onChanged();
            }) {
                text = "Remove Surface",
                style = {
                    marginLeft = 6
                }
            };

            header.Add(removeSurfaceBtn);
            card.Add(header);

            var modulesContainer = new VisualElement {
                style = {
                    marginLeft = 4,
                    marginTop = 2
                }
            };

            card.Add(modulesContainer);

            void RenderAllModules() {
                serializedObject.Update();
                modulesContainer.Clear();

                for (var j = 0; j < modulesProp.arraySize; j++)
                    modulesContainer.Add(BuildModuleRow(modulesProp, j, RenderAllModules));
            }

            RenderAllModules();

            var addModuleBtn = new Button(() => ShowModuleAddMenu(modulesProp, RenderAllModules)) {
                text = "Add Module",
                style = { marginTop = 4 }
            };

            card.Add(addModuleBtn);

            return card;
        }

        private VisualElement BuildModuleRow(SerializedProperty modulesProp, int index, Action onChanged) {
            var capturedIndex = index;
            var moduleProp = modulesProp.GetArrayElementAtIndex(index);

            var moduleAccent = new Color(0.3f, 0.7f, 0.4f);

            var row = new VisualElement {
                style = {
                    marginTop = 8,
                    marginBottom = 4,
                    paddingTop = 8,
                    paddingBottom = 10,
                    paddingLeft = 10,
                    paddingRight = 10,
                    borderLeftWidth = 4,
                    borderLeftColor = moduleAccent,
                    borderTopWidth = 1,
                    borderRightWidth = 1,
                    borderBottomWidth = 1,
                    borderTopColor = new Color(moduleAccent.r, moduleAccent.g, moduleAccent.b, 0.25f),
                    borderRightColor = new Color(moduleAccent.r, moduleAccent.g, moduleAccent.b, 0.25f),
                    borderBottomColor = new Color(moduleAccent.r, moduleAccent.g, moduleAccent.b, 0.25f),
                    backgroundColor = new Color(0f, 0f, 0f, 0.12f)
                }
            };

            var header = new VisualElement {
                style = {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    marginBottom = 6,
                    paddingBottom = 6,
                    borderBottomWidth = 1,
                    borderBottomColor = new Color(1f, 1f, 1f, 0.08f)
                }
            };

            var typeLabel = new Label(GetNiceTypeName(moduleProp)) {
                style = {
                    flexGrow = 1,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    fontSize = 14,
                    color = new Color(0.86f, 0.96f, 0.86f)
                }
            };

            header.Add(typeLabel);

            var removeBtn = new Button(() => {
                modulesProp.DeleteArrayElementAtIndex(capturedIndex);
                serializedObject.ApplyModifiedProperties();
                PersistStructuralChange();
                onChanged();
            }) {
                text = "Remove",
                style = { marginLeft = 4 }
            };

            header.Add(removeBtn);
            row.Add(header);

            if (moduleProp.managedReferenceValue == null) {
                var emptyLabel = new Label("(null managed reference)") {
                    style = {
                        color = new Color(0.85f, 0.5f, 0.3f),
                        unityFontStyleAndWeight = FontStyle.Italic
                    }
                };

                row.Add(emptyLabel);

                return row;
            }

            // PropertyField for the managed-reference element. If a CustomPropertyDrawer is registered for the
            // concrete module type, its CreatePropertyGUI runs here. Otherwise Unity's default UI Toolkit
            // rendering shows the fields.
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

        private static VisualElement Spacer(float height) {
            var v = new VisualElement();
            v.style.height = height;

            return v;
        }
    }
}
