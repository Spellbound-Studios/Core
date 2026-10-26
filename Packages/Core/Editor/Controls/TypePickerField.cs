// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UIElements;

namespace Spellbound.Core.Controls.Editor {
    public class TypePickerField : BaseField<Type> {
        public const string ClassName = "sb-type-picker";
        public const string NoneLabel = "None";

        private readonly DropdownField _dropdown;

        public TypePickerField(string label, Type baseType) : this(label, baseType, new DropdownField()) { }

        private TypePickerField(string label, Type baseType, DropdownField dropdown) : base(label, dropdown) {
            BaseType = baseType ?? throw new ArgumentNullException(nameof(baseType));
            _dropdown = dropdown;
            AddToClassList(ClassName);
            DisplayName = type => type.Name;
            Filter = IsConstructible;
            _dropdown.RegisterValueChangedCallback(OnDropdownChanged);
            RefreshChoices();
        }

        public Type BaseType { get; }
        public Func<Type, string> DisplayName { get; set; }
        public Func<Type, bool> Filter { get; set; }
        public IReadOnlyList<Type> Choices { get; private set; } = Array.Empty<Type>();

        public void RefreshChoices() {
            var choices = new List<Type>();
            var filter = Filter ?? IsConstructible;

            foreach (var type in TypeCache.GetTypesDerivedFrom(BaseType)) {
                if (filter(type)) {
                    choices.Add(type);
                }
            }

            choices.Sort(CompareByDisplayName);
            Choices = choices;
            var labels = new List<string>(choices.Count + 1) { NoneLabel };

            foreach (var type in choices) {
                labels.Add(NameOf(type));
            }

            _dropdown.choices = labels;
            _dropdown.SetValueWithoutNotify(LabelFor(value));
        }

        public override void SetValueWithoutNotify(Type newValue) {
            base.SetValueWithoutNotify(newValue);
            _dropdown?.SetValueWithoutNotify(LabelFor(newValue));
        }

        private void OnDropdownChanged(ChangeEvent<string> change) {
            change.StopPropagation();
            var index = _dropdown.choices.IndexOf(change.newValue);
            var picked = index > 0 && index <= Choices.Count ? Choices[index - 1] : null;
            var previous = value;

            if (picked == previous) {
                return;
            }

            SetValueWithoutNotify(picked);

            using (var changed = ChangeEvent<Type>.GetPooled(previous, picked)) {
                changed.target = this;
                SendEvent(changed);
            }
        }

        private int CompareByDisplayName(Type left, Type right) {
            var byName = string.CompareOrdinal(NameOf(left), NameOf(right));
            return byName != 0 ? byName : string.CompareOrdinal(left.FullName, right.FullName);
        }

        private string LabelFor(Type type) {
            return type == null ? NoneLabel : NameOf(type);
        }

        private string NameOf(Type type) {
            return DisplayName?.Invoke(type) ?? type.Name;
        }

        private static bool IsConstructible(Type type) {
            if (type.IsAbstract || type.IsGenericTypeDefinition) {
                return false;
            }

            return type.IsValueType || type.GetConstructor(Type.EmptyTypes) != null;
        }
    }
}
