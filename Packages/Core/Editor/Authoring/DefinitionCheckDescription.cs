// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Collections.Generic;
using Spellbound.Core.Authoring;
using UnityEngine;

namespace Spellbound.Core.Authoring.Editor {
    public sealed class DefinitionCheckDescription {
        private readonly IDefinitionCheckAdapter _adapter;

        public string Name { get; }
        public Type DefinitionType { get; }
        public Type CheckType { get; }
        public string Package { get; }
        public bool RunsWhileEditing { get; }
        public string Label => Name + " (" + Package + ")";

        public DefinitionCheckDescription(string name, bool runsWhileEditing, object check, Type definitionType) {
            if (!ValidationFinding.IsCode(name)) {
                throw new ArgumentException("A check name is <package>.<kebab-name> in lower case; '" + name + "' is not.",
                    nameof(name));
            }

            if (check == null) {
                throw new ArgumentNullException(nameof(check));
            }

            if (definitionType == null || !typeof(ScriptableObject).IsAssignableFrom(definitionType)) {
                throw new ArgumentException("A check runs over a ScriptableObject type.", nameof(definitionType));
            }

            var contract = typeof(IDefinitionCheck<>).MakeGenericType(definitionType);

            if (!contract.IsInstanceOfType(check)) {
                throw new ArgumentException(
                    check.GetType().FullName + " does not implement IDefinitionCheck<" + definitionType.Name + ">.", nameof(check));
            }

            Name = name;
            RunsWhileEditing = runsWhileEditing;
            DefinitionType = definitionType;
            CheckType = check.GetType();
            Package = PackageLabel.Of(CheckType);
            var adapterType = typeof(DefinitionCheckAdapter<>).MakeGenericType(definitionType);
            _adapter = (IDefinitionCheckAdapter)Activator.CreateInstance(adapterType, check);
        }

        public static IReadOnlyList<DefinitionCheckDescription> From(Type checkType) {
            var found = new List<DefinitionCheckDescription>();

            if (checkType == null || checkType.IsAbstract || checkType.IsInterface
                || checkType.ContainsGenericParameters) {
                return found;
            }

            var attribute = (RegisteredDefinitionCheckAttribute)Attribute.GetCustomAttribute(checkType,
                typeof(RegisteredDefinitionCheckAttribute), false);

            if (attribute == null) {
                return found;
            }

            var definitionTypes = DefinitionTypesOf(checkType);

            if (definitionTypes.Count == 0) {
                throw new ArgumentException(
                    checkType.FullName + " carries RegisteredDefinitionCheck but implements no IDefinitionCheck<T>.",
                    nameof(checkType));
            }

            var constructor = checkType.GetConstructor(Type.EmptyTypes);

            if (constructor == null) {
                throw new ArgumentException(checkType.FullName + " needs a public constructor with no parameters.",
                    nameof(checkType));
            }

            var instance = constructor.Invoke(null);

            foreach (var definitionType in definitionTypes) {
                found.Add(
                    new DefinitionCheckDescription(attribute.Name, attribute.RunsWhileEditing, instance, definitionType));
            }

            return found;
        }

        public static IReadOnlyList<Type> DefinitionTypesOf(Type checkType) {
            var types = new List<Type>();

            foreach (var implemented in checkType.GetInterfaces()) {
                if (implemented.IsGenericType && implemented.GetGenericTypeDefinition() == typeof(IDefinitionCheck<>)) {
                    types.Add(implemented.GetGenericArguments()[0]);
                }
            }

            types.Sort((left, right) => string.CompareOrdinal(left.FullName, right.FullName));
            return types;
        }

        public bool AppliesTo(Type assetType) {
            return assetType != null && DefinitionType.IsAssignableFrom(assetType);
        }

        public void Run(ScriptableObject definition, ValidationReport report) {
            _adapter.Check(definition, report);
        }

        public IReadOnlyList<string> PreviewLines(ScriptableObject definition) {
            return _adapter.PreviewLines(definition) ?? Array.Empty<string>();
        }

        public override string ToString() {
            return Label;
        }

        private interface IDefinitionCheckAdapter {
            void Check(ScriptableObject definition, ValidationReport report);
            IReadOnlyList<string> PreviewLines(ScriptableObject definition);
        }

        private sealed class DefinitionCheckAdapter<T> : IDefinitionCheckAdapter where T : ScriptableObject {
            private readonly IDefinitionCheck<T> _check;

            public DefinitionCheckAdapter(object check) {
                _check = (IDefinitionCheck<T>)check;
            }

            public void Check(ScriptableObject definition, ValidationReport report) {
                _check.Check((T)definition, report);
            }

            public IReadOnlyList<string> PreviewLines(ScriptableObject definition) {
                return _check.PreviewLines((T)definition);
            }
        }
    }
}
