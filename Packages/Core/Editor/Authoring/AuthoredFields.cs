// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;

namespace Spellbound.Core.Authoring.Editor {
    public static class AuthoredFields {
        public const string ManagedReferenceListType = "managed-reference list";

        internal const string ManagedReferenceType = "managed-reference";
        internal const string ListSuffix = " list";
        internal const string ElementMarker = "[]";

        private const int SerializationDepthLimit = 10;
        private const string ArrayName = "Array";
        private const string ElementPrefix = "data[";

        private const BindingFlags DeclaredInstanceFields =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

        private static readonly Regex ElementPath = new Regex(@"\.Array\.data\[\d+\]", RegexOptions.Compiled);
        private static readonly Regex StoredElement = new Regex(@"\[\d+\]", RegexOptions.Compiled);

        private static readonly Dictionary<Type, IReadOnlyList<AuthoredField>> StoredFieldsByType =
            new Dictionary<Type, IReadOnlyList<AuthoredField>>();

        private static readonly Dictionary<Type, StoredFieldSet> FieldSetsByType =
            new Dictionary<Type, StoredFieldSet>();

        private static readonly Dictionary<Type, IReadOnlyList<AuthoredField>> DeclaredFieldsByType =
            new Dictionary<Type, IReadOnlyList<AuthoredField>>();

        public static IReadOnlyList<AuthoredField> Describe(Type type) {
            if (type == null) {
                throw new ArgumentNullException(nameof(type));
            }

            if (!IsInstantiableAsset(type)) {
                return Array.Empty<AuthoredField>();
            }

            if (DeclaredFieldsByType.TryGetValue(type, out var cached)) {
                return cached;
            }

            var declared = new List<AuthoredField>();

            foreach (var field in DescribeStored(type)) {
                if (field.IsDeclared) {
                    declared.Add(field);
                }
            }

            var described = declared.AsReadOnly();
            DeclaredFieldsByType.Add(type, described);
            return described;
        }

        public static void ClearCache() {
            StoredFieldsByType.Clear();
            FieldSetsByType.Clear();
            DeclaredFieldsByType.Clear();
        }

        internal static string NormalizeStoredPath(string storedPath) {
            return string.IsNullOrEmpty(storedPath) ? string.Empty : StoredElement.Replace(storedPath, ElementMarker);
        }

        internal static StoredFieldSet FieldSetOf(Type type) {
            if (type == null) {
                throw new ArgumentNullException(nameof(type));
            }

            if (!FieldSetsByType.TryGetValue(type, out var fieldSet)) {
                fieldSet = new StoredFieldSet(DescribeStored(type));
                FieldSetsByType.Add(type, fieldSet);
            }

            return fieldSet;
        }

        internal static IReadOnlyList<AuthoredField> DescribeStored(Type type) {
            if (type == null) {
                throw new ArgumentNullException(nameof(type));
            }

            if (StoredFieldsByType.TryGetValue(type, out var cached)) {
                return cached;
            }

            IReadOnlyList<AuthoredField> fields;

            if (IsInstantiableAsset(type)) {
                fields = DescribeAsset(type);
            }
            else {
                fields = IsReferenceable(type) ? DescribeReference(type) : Array.Empty<AuthoredField>();
            }

            StoredFieldsByType.Add(type, fields);
            return fields;
        }

        private static bool IsInstantiableAsset(Type type) {
            return typeof(ScriptableObject).IsAssignableFrom(type) && !type.IsAbstract
                   && !type.ContainsGenericParameters;
        }

        private static bool IsReferenceable(Type type) {
            return type.IsClass && !type.IsAbstract && !type.ContainsGenericParameters
                   && !typeof(Object).IsAssignableFrom(type);
        }

        private static IReadOnlyList<AuthoredField> DescribeAsset(Type type) {
            var instance = ScriptableObject.CreateInstance(type);

            try {
                instance.hideFlags = HideFlags.HideAndDontSave;
                return Collect(instance, type, string.Empty, type.Name);
            }
            finally {
                Object.DestroyImmediate(instance);
            }
        }

        private static IReadOnlyList<AuthoredField> DescribeReference(Type type) {
            var host = ScriptableObject.CreateInstance<AuthoredReferenceHost>();

            try {
                host.hideFlags = HideFlags.HideAndDontSave;
                host.value = CreateReference(type);
                return Collect(host, typeof(AuthoredReferenceHost), AuthoredReferenceHost.ValueField, type.Name);
            }
            finally {
                Object.DestroyImmediate(host);
            }
        }

        private static object CreateReference(Type type) {
            var constructor = type.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null, Type.EmptyTypes, null);
            return constructor != null ? constructor.Invoke(null) : RuntimeHelpers.GetUninitializedObject(type);
        }

        private static IReadOnlyList<AuthoredField> Collect(Object instance, Type rootType, string rootPath,
            string owner) {
            var serialized = new SerializedObject(instance);
            ExpandEmptyArrays(serialized);

            var typesByPath = new Dictionary<string, Type> { [string.Empty] = rootType };
            var prefix = rootPath.Length == 0 ? string.Empty : rootPath + ".";
            var fields = new List<AuthoredField>();
            var iterator = serialized.GetIterator();
            var enterChildren = true;

            while (iterator.Next(enterChildren)) {
                enterChildren = true;
                var path = iterator.propertyPath;
                var name = iterator.name;
                var parentPath = path.Length == name.Length ? string.Empty
                    : path.Substring(0, path.Length - name.Length - 1);

                if (iterator.propertyType == SerializedPropertyType.ArraySize || name == ArrayName) {
                    continue;
                }

                if (name.StartsWith(ElementPrefix, StringComparison.Ordinal)) {
                    typesByPath[path] = ElementTypeOf(TypeAt(typesByPath, ArrayOwnerPath(parentPath)));
                    continue;
                }

                var field = FindField(TypeAt(typesByPath, parentPath), name);
                var isManagedReference = iterator.propertyType == SerializedPropertyType.ManagedReference
                                         || (field != null && field.IsDefined(typeof(SerializeReference), false));

                if (field != null) {
                    typesByPath[path] = iterator.propertyType == SerializedPropertyType.ManagedReference
                                        && iterator.managedReferenceValue != null
                        ? iterator.managedReferenceValue.GetType()
                        : field.FieldType;
                }

                if (path == rootPath) {
                    continue;
                }

                if (prefix.Length > 0 && !path.StartsWith(prefix, StringComparison.Ordinal)) {
                    enterChildren = false;
                    continue;
                }

                if (isManagedReference) {
                    enterChildren = false;
                }

                fields.Add(new AuthoredField(owner, ElementPath.Replace(path.Substring(prefix.Length), ElementMarker),
                    TypeText(iterator, isManagedReference), isManagedReference, FormerNamesOf(field), field != null));
            }

            fields.Sort((left, right) => string.CompareOrdinal(left.Path, right.Path));
            return fields;
        }

        private static void ExpandEmptyArrays(SerializedObject serialized) {
            for (var pass = 0; pass < SerializationDepthLimit; pass++) {
                serialized.Update();
                var grew = false;
                var iterator = serialized.GetIterator();

                while (iterator.Next(true)) {
                    if (iterator.isArray && iterator.propertyType != SerializedPropertyType.String
                                         && iterator.arraySize == 0) {
                        iterator.arraySize = 1;
                        grew = true;
                    }
                }

                if (!grew) {
                    return;
                }

                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static string TypeText(SerializedProperty property, bool isManagedReference) {
            var isList = property.isArray && property.propertyType != SerializedPropertyType.String;

            if (isManagedReference) {
                return isList ? ManagedReferenceListType : ManagedReferenceType;
            }

            return isList ? property.arrayElementType + ListSuffix : property.type;
        }

        private static string ArrayOwnerPath(string arrayPath) {
            var suffix = "." + ArrayName;
            return arrayPath.EndsWith(suffix, StringComparison.Ordinal)
                ? arrayPath.Substring(0, arrayPath.Length - suffix.Length)
                : arrayPath;
        }

        private static Type TypeAt(Dictionary<string, Type> typesByPath, string path) {
            return typesByPath.TryGetValue(path, out var type) ? type : null;
        }

        private static Type ElementTypeOf(Type listType) {
            if (listType == null) {
                return null;
            }

            if (listType.IsArray) {
                return listType.GetElementType();
            }

            if (listType.IsGenericType && typeof(IList).IsAssignableFrom(listType)) {
                return listType.GetGenericArguments()[0];
            }

            return null;
        }

        private static FieldInfo FindField(Type type, string name) {
            for (var current = type; current != null && current != typeof(object); current = current.BaseType) {
                var field = current.GetField(name, DeclaredInstanceFields);

                if (field != null) {
                    return field;
                }
            }

            return null;
        }

        private static IReadOnlyList<string> FormerNamesOf(FieldInfo field) {
            if (field == null) {
                return Array.Empty<string>();
            }

            var names = new List<string>();

            foreach (FormerlySerializedAsAttribute attribute in field.GetCustomAttributes(
                         typeof(FormerlySerializedAsAttribute), false)) {
                names.Add(attribute.oldName);
            }

            return names;
        }
    }
}
