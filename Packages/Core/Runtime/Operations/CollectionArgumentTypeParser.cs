// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Collections;
using System.Collections.Generic;

namespace Spellbound.Core.Operations {
    internal sealed class CollectionArgumentTypeParser : IArgumentTypeParser {
        private readonly ArgumentParserRegistry _registry;

        public CollectionArgumentTypeParser(ArgumentParserRegistry registry) {
            _registry = registry;
        }

        public bool CanParse(Type type) {
            return ElementType(type) != null && _registry.CanParse(ElementType(type));
        }

        public string Expected(Type type) {
            return "comma separated list of " + _registry.Expected(ElementType(type));
        }

        public IReadOnlyList<string> LegalValues(Type type) {
            return _registry.LegalValues(ElementType(type));
        }

        public bool TryParse(string text, Type type, out object value) {
            value = null;
            var elementType = ElementType(type);
            var pieces = text.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType));

            foreach (var piece in pieces) {
                if (!_registry.TryParse(piece.Trim(), elementType, out var element)) {
                    return false;
                }

                list.Add(element);
            }

            if (type.IsArray) {
                var array = Array.CreateInstance(elementType, list.Count);
                list.CopyTo(array, 0);
                value = array;
                return true;
            }

            value = list;
            return true;
        }

        private static Type ElementType(Type type) {
            if (type.IsArray && type.GetArrayRank() == 1) {
                return type.GetElementType();
            }

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>)) {
                return type.GetGenericArguments()[0];
            }

            return null;
        }
    }
}
