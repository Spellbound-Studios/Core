// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UIElements;

namespace Spellbound.Core.Controls.Editor {
    [UxmlElement]
    public partial class TypePickerField : BaseField<Type> {
        public const string ClassName = "sb-type-picker";
        public const string NoneLabel = "None";

        private readonly DropdownField _dropdown;
        private List<string> _labels = new List<string> { NoneLabel };
        private Type _baseType;

        public TypePickerField() : this(string.Empty, null, new DropdownField()) { }

        public TypePickerField(string label, Type baseType)
            : this(label, baseType ?? throw new ArgumentNullException(nameof(baseType)), new DropdownField()) { }

        private TypePickerField(string label, Type baseType, DropdownField dropdown) : base(label, dropdown) {
            _dropdown = dropdown;
            AddToClassList(ClassName);
            ControlStyleSheet.Apply(this);
            DisplayName = type => type.Name;
            Filter = IsConstructible;
            _dropdown.RegisterValueChangedCallback(OnDropdownChanged);
            BaseType = baseType;
        }

        public Type BaseType {
            get => _baseType;
            set {
                _baseType = value;
                RefreshChoices();
            }
        }

        [UxmlAttribute("base-type")]
        public string BaseTypeName {
            get => _baseType?.AssemblyQualifiedName;
            set => BaseType = string.IsNullOrWhiteSpace(value) ? null : Type.GetType(value, false);
        }

        public Func<Type, string> DisplayName { get; set; }
        public Func<Type, bool> Filter { get; set; }
        public IReadOnlyList<Type> Choices { get; private set; } = Array.Empty<Type>();
        public IReadOnlyList<string> Labels => _labels;

        public void RefreshChoices() {
            var choices = new List<Type>();
            var filter = Filter ?? IsConstructible;

            if (_baseType != null) {
                foreach (var type in TypeCache.GetTypesDerivedFrom(_baseType)) {
                    if (filter(type)) {
                        choices.Add(type);
                    }
                }
            }

            choices.Sort(CompareByDisplayName);
            Choices = choices;
            _labels = UniqueLabels(choices);
            _dropdown.choices = new List<string>(_labels);
            _dropdown.SetValueWithoutNotify(LabelFor(value));
        }

        public override void SetValueWithoutNotify(Type newValue) {
            base.SetValueWithoutNotify(newValue);
            _dropdown?.SetValueWithoutNotify(LabelFor(newValue));
        }

        protected virtual string QualifiedLabel(Type type, string label) {
            return string.IsNullOrEmpty(type.Namespace) ? type.FullName : label + " (" + type.Namespace + ")";
        }

        private void OnDropdownChanged(ChangeEvent<string> change) {
            change.StopPropagation();
            var index = _dropdown.index;
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

        private List<string> UniqueLabels(List<Type> choices) {
            var labels = new List<string>(choices.Count + 1) { NoneLabel };
            var counts = CountOf(choices, NameOf);

            foreach (var type in choices) {
                var name = NameOf(type);
                labels.Add(counts[name] > 1 ? QualifiedLabel(type, name) : name);
            }

            var qualifiedCounts = CountOf(labels, label => label);

            for (var index = 1; index < labels.Count; index++) {
                if (qualifiedCounts[labels[index]] > 1) {
                    labels[index] = choices[index - 1].FullName;
                }
            }

            return labels;
        }

        private static Dictionary<string, int> CountOf<T>(IReadOnlyList<T> items, Func<T, string> keyOf) {
            var counts = new Dictionary<string, int>(StringComparer.Ordinal);

            foreach (var item in items) {
                var key = keyOf(item);
                counts[key] = counts.TryGetValue(key, out var count) ? count + 1 : 1;
            }

            return counts;
        }

        private int CompareByDisplayName(Type left, Type right) {
            var byName = string.CompareOrdinal(NameOf(left), NameOf(right));
            return byName != 0 ? byName : string.CompareOrdinal(left.FullName, right.FullName);
        }

        private string LabelFor(Type type) {
            if (type == null) {
                return NoneLabel;
            }

            for (var index = 0; index < Choices.Count; index++) {
                if (Choices[index] == type) {
                    return _labels[index + 1];
                }
            }

            return NameOf(type);
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
