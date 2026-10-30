// Copyright (c) Spellbound Studios. All rights reserved.

using System.Collections.Generic;

namespace Spellbound.Core.Operations {
    public sealed class OperationGroupDescription {
        private readonly List<OperationDescription> _operations = new List<OperationDescription>();
        private readonly List<string> _assemblies = new List<string>();

        public string Name { get; }
        public string Job { get; internal set; }
        public IReadOnlyList<OperationDescription> Operations => _operations;
        public IReadOnlyList<string> Assemblies => _assemblies;

        internal OperationGroupDescription(string name, string job) {
            Name = name;
            Job = job;
        }

        internal void AddAssembly(string assemblyName) {
            if (!_assemblies.Contains(assemblyName)) {
                _assemblies.Add(assemblyName);
            }
        }

        internal bool TryAdd(OperationDescription operation) {
            foreach (var existing in _operations) {
                if (string.Equals(existing.Name, operation.Name, System.StringComparison.OrdinalIgnoreCase)) {
                    return false;
                }
            }

            _operations.Add(operation);
            return true;
        }

        internal bool Remove(OperationDescription operation) {
            return _operations.Remove(operation);
        }

        internal OperationDescription Find(string name) {
            foreach (var operation in _operations) {
                if (string.Equals(operation.Name, name, System.StringComparison.OrdinalIgnoreCase)) {
                    return operation;
                }
            }

            return null;
        }

        internal void Sort() {
            _operations.Sort((left, right) => string.CompareOrdinal(left.Name, right.Name));
        }
    }
}
