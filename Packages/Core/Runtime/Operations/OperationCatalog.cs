// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Collections.Generic;
using System.Reflection;
using Spellbound.Core.Logging;
using Spellbound.Core.Tooling;

namespace Spellbound.Core.Operations {
    public sealed class OperationCatalog {
        private const BindingFlags StaticOperations = BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly;
        private const BindingFlags InstanceOperations = BindingFlags.Public | BindingFlags.Instance;

        private readonly Func<IEnumerable<Assembly>> _assemblySource;
        private readonly List<OperationGroupDescription> _groups = new List<OperationGroupDescription>();
        private readonly Dictionary<uint, OperationDescription> _byIdentifier = new Dictionary<uint, OperationDescription>();
        private readonly Dictionary<object, List<OperationDescription>> _instances = new Dictionary<object, List<OperationDescription>>();
        private bool _loaded;

        public double LoadMilliseconds { get; private set; }

        public OperationCatalog() : this(null) { }

        public OperationCatalog(Func<IEnumerable<Assembly>> assemblySource) {
            _assemblySource = assemblySource;
        }

        public IReadOnlyList<OperationGroupDescription> Groups {
            get {
                EnsureLoaded();
                return _groups;
            }
        }

        public IReadOnlyList<Assembly> MarkedAssemblies() {
            return _assemblySource != null ? MarkedAssemblyScan.Assemblies(_assemblySource()) : MarkedAssemblyScan.Assemblies();
        }

        public OperationGroupDescription Group(string name) {
            EnsureLoaded();

            foreach (var group in _groups) {
                if (string.Equals(group.Name, name, StringComparison.OrdinalIgnoreCase)) {
                    return group;
                }
            }

            return null;
        }

        public OperationDescription Describe(string group, string name) {
            return Group(group)?.Find(name);
        }

        public OperationDescription Describe(uint identifier) {
            EnsureLoaded();
            return _byIdentifier.TryGetValue(identifier, out var description) ? description : null;
        }

        public IReadOnlyList<OperationDescription> Find(string prefix) {
            EnsureLoaded();
            var found = new List<OperationDescription>();
            var wanted = (prefix ?? string.Empty).Trim();

            foreach (var group in _groups) {
                foreach (var operation in group.Operations) {
                    if (operation.Line.StartsWith(wanted, StringComparison.OrdinalIgnoreCase)) {
                        found.Add(operation);
                    }
                }
            }

            return found;
        }

        public IReadOnlyList<OperationDescription> FindByName(string name) {
            EnsureLoaded();
            var found = new List<OperationDescription>();

            foreach (var group in _groups) {
                var operation = group.Find(name);

                if (operation != null) {
                    found.Add(operation);
                }
            }

            return found;
        }

        public int Register(object instance) {
            if (instance == null) {
                throw new ArgumentNullException(nameof(instance));
            }

            EnsureLoaded();

            if (_instances.ContainsKey(instance)) {
                return 0;
            }

            var type = instance.GetType();
            var groupAttribute = type.Assembly.GetCustomAttribute<OperationGroupAttribute>();

            if (groupAttribute == null) {
                Log.Error("Operations on " + type.FullName + " cannot be registered: its assembly carries no OperationGroup attribute.");
                return 0;
            }

            var group = Group(groupAttribute.Name) ?? AddGroup(groupAttribute, type.Assembly);
            var added = new List<OperationDescription>();

            foreach (var method in SortedMethods(type.GetMethods(InstanceOperations))) {
                var description = Describe(group.Name, method, instance);

                if (description != null && Add(group, description)) {
                    added.Add(description);
                }
            }

            group.Sort();
            _instances[instance] = added;
            return added.Count;
        }

        public int Unregister(object instance) {
            if (instance == null || !_instances.TryGetValue(instance, out var added)) {
                return 0;
            }

            foreach (var description in added) {
                Group(description.Group)?.Remove(description);
                _byIdentifier.Remove(description.Identifier);
            }

            _instances.Remove(instance);
            return added.Count;
        }

        public void Reload() {
            _loaded = false;
            _groups.Clear();
            _byIdentifier.Clear();
            _instances.Clear();
            EnsureLoaded();
        }

        private void EnsureLoaded() {
            if (_loaded) {
                return;
            }

            _loaded = true;
            var watch = System.Diagnostics.Stopwatch.StartNew();

            foreach (var assembly in MarkedAssemblies()) {
                var attribute = assembly.GetCustomAttribute<OperationGroupAttribute>();
                var group = Group(attribute.Name);

                if (group == null) {
                    group = AddGroup(attribute, assembly);
                }
                else if (!string.IsNullOrWhiteSpace(attribute.Job) && !string.IsNullOrWhiteSpace(group.Job)
                         && !string.Equals(attribute.Job, group.Job, StringComparison.Ordinal)) {
                    Log.Error("Two operation groups are named '" + attribute.Name + "': " + string.Join(", ", group.Assemblies)
                              + " is used and " + assembly.GetName().Name + " is skipped.");
                    continue;
                }
                else {
                    group.AddAssembly(assembly.GetName().Name);

                    if (string.IsNullOrWhiteSpace(group.Job)) {
                        group.Job = attribute.Job;
                    }
                }

                foreach (var type in SortedTypes(assembly)) {
                    if (!type.IsClass || !(type.IsPublic || type.IsNestedPublic)) {
                        continue;
                    }

                    foreach (var method in SortedMethods(type.GetMethods(StaticOperations))) {
                        var description = Describe(group.Name, method, null);

                        if (description != null) {
                            Add(group, description);
                        }
                    }
                }
            }

            foreach (var group in _groups) {
                group.Sort();
            }

            _groups.Sort((left, right) => string.CompareOrdinal(left.Name, right.Name));
            LoadMilliseconds = watch.Elapsed.TotalMilliseconds;
        }

        private OperationGroupDescription AddGroup(OperationGroupAttribute attribute, Assembly assembly) {
            var group = new OperationGroupDescription(attribute.Name, attribute.Job);
            group.AddAssembly(assembly.GetName().Name);
            _groups.Add(group);
            return group;
        }

        private bool Add(OperationGroupDescription group, OperationDescription description) {
            if (!group.TryAdd(description)) {
                var existing = group.Find(description.Name);
                Log.Error("Operation '" + description.Line + "' is declared twice: " + existing.AssemblyName + "."
                          + existing.Method.DeclaringType?.Name + " is used and " + description.AssemblyName + "."
                          + description.Method.DeclaringType?.Name + " is skipped.");
                return false;
            }

            _byIdentifier[description.Identifier] = description;
            return true;
        }

        private static OperationDescription Describe(string group, MethodInfo method, object instance) {
            var attribute = method.GetCustomAttribute<OperationAttribute>(false);

            if (attribute == null) {
                return null;
            }

            if (method.IsGenericMethodDefinition) {
                Log.Error("Operation '" + group + " " + attribute.Name + "' on " + method.DeclaringType?.FullName
                          + " is generic and was skipped.");
                return null;
            }

            var parameters = method.GetParameters();
            var arguments = new List<ArgumentDescription>(parameters.Length);
            var contextIndex = -1;

            foreach (var parameter in parameters) {
                if (parameter.ParameterType == typeof(OperationContext)) {
                    if (contextIndex >= 0) {
                        Log.Error("Operation '" + group + " " + attribute.Name + "' declares two OperationContext parameters and was skipped.");
                        return null;
                    }

                    contextIndex = parameter.Position;
                    continue;
                }

                if (parameter.IsOut || parameter.ParameterType.IsByRef) {
                    Log.Error("Operation '" + group + " " + attribute.Name + "' has a by-reference parameter '" + parameter.Name
                              + "' and was skipped.");
                    return null;
                }

                var summary = parameter.GetCustomAttribute<ArgumentAttribute>(false)?.Description ?? string.Empty;
                var underlying = Nullable.GetUnderlyingType(parameter.ParameterType) ?? parameter.ParameterType;
                var legal = underlying.IsEnum ? Enum.GetNames(underlying) : underlying == typeof(bool) ? new[] { "true", "false" } : null;
                arguments.Add(new ArgumentDescription(parameter, ArgumentText.KebabCase(parameter.Name), summary, legal));
            }

            return new OperationDescription(group, attribute, method, instance, arguments, contextIndex);
        }

        private static List<Type> SortedTypes(Assembly assembly) {
            var types = new List<Type>(AssemblyScanning.LoadableTypes(assembly));
            types.Sort((left, right) => string.CompareOrdinal(left.FullName, right.FullName));
            return types;
        }

        private static MethodInfo[] SortedMethods(MethodInfo[] methods) {
            Array.Sort(methods, (left, right) => left.MetadataToken.CompareTo(right.MetadataToken));
            return methods;
        }
    }
}
