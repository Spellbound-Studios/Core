// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Collections.Generic;
using System.Reflection;
using Spellbound.Core.Logging;

namespace Spellbound.Core.Operations {
    public sealed class ArgumentParserRegistry {
        private readonly Dictionary<Type, IArgumentParserAdapter> _exact = new Dictionary<Type, IArgumentParserAdapter>();
        private readonly List<IArgumentTypeParser> _families = new List<IArgumentTypeParser>();
        private readonly Func<IEnumerable<Assembly>> _markedAssemblies;
        private bool _scanned;

        public ArgumentParserRegistry() : this(null) { }

        public ArgumentParserRegistry(Func<IEnumerable<Assembly>> markedAssemblies) {
            _markedAssemblies = markedAssemblies;
            BuiltInArgumentParsers.Register(this);
        }

        public void Register<T>(IArgumentParser<T> parser) {
            if (parser == null) {
                throw new ArgumentNullException(nameof(parser));
            }

            _exact[typeof(T)] = new ArgumentParserAdapter<T>(parser);
        }

        public void Register(IArgumentTypeParser parser) {
            if (parser == null) {
                throw new ArgumentNullException(nameof(parser));
            }

            _families.Insert(0, parser);
        }

        public bool CanParse(Type type) {
            EnsureScanned();
            var target = Nullable.GetUnderlyingType(type) ?? type;

            if (_exact.ContainsKey(target)) {
                return true;
            }

            foreach (var family in _families) {
                if (family.CanParse(target)) {
                    return true;
                }
            }

            return false;
        }

        public string Expected(Type type) {
            EnsureScanned();
            var target = Nullable.GetUnderlyingType(type) ?? type;

            if (_exact.TryGetValue(target, out var exact)) {
                return exact.Expected;
            }

            foreach (var family in _families) {
                if (family.CanParse(target)) {
                    return family.Expected(target);
                }
            }

            return ArgumentText.KebabCase(target.Name);
        }

        public IReadOnlyList<string> LegalValues(Type type) {
            EnsureScanned();
            var target = Nullable.GetUnderlyingType(type) ?? type;

            if (_exact.ContainsKey(target)) {
                return target == typeof(bool) ? new[] { "true", "false" } : Array.Empty<string>();
            }

            foreach (var family in _families) {
                if (family.CanParse(target)) {
                    return family.LegalValues(target);
                }
            }

            return Array.Empty<string>();
        }

        public bool TryParse(string text, Type type, out object value) {
            EnsureScanned();
            value = null;

            if (text == null) {
                return false;
            }

            var target = Nullable.GetUnderlyingType(type) ?? type;

            if (_exact.TryGetValue(target, out var exact)) {
                return exact.TryParse(text, out value);
            }

            foreach (var family in _families) {
                if (family.CanParse(target)) {
                    return family.TryParse(text, target, out value);
                }
            }

            return false;
        }

        internal void EnsureScanned() {
            if (_scanned) {
                return;
            }

            _scanned = true;
            var assemblies = _markedAssemblies != null ? _markedAssemblies() : MarkedAssemblyScan.Assemblies();

            foreach (var type in MarkedAssemblyScan.TypesWith<ArgumentParserAttribute>(assemblies)) {
                Adopt(type);
            }
        }

        private void Adopt(Type type) {
            if (type.IsAbstract || type.GetConstructor(Type.EmptyTypes) == null) {
                Log.Error("Argument parser " + type.FullName + " needs a public parameterless constructor; it was skipped.");
                return;
            }

            object instance;

            try {
                instance = Activator.CreateInstance(type);
            }
            catch (Exception exception) {
                Log.Error("Argument parser " + type.FullName + " could not be created: " + exception.Message);
                return;
            }

            var adopted = false;

            if (instance is IArgumentTypeParser family) {
                Register(family);
                adopted = true;
            }

            foreach (var contract in type.GetInterfaces()) {
                if (!contract.IsGenericType || contract.GetGenericTypeDefinition() != typeof(IArgumentParser<>)) {
                    continue;
                }

                var argumentType = contract.GetGenericArguments()[0];
                var adapterType = typeof(ArgumentParserAdapter<>).MakeGenericType(argumentType);
                _exact[argumentType] = (IArgumentParserAdapter)Activator.CreateInstance(adapterType, instance);
                adopted = true;
            }

            if (!adopted) {
                Log.Error("Argument parser " + type.FullName + " implements neither IArgumentParser<T> nor IArgumentTypeParser; it was skipped.");
            }
        }
    }
}
