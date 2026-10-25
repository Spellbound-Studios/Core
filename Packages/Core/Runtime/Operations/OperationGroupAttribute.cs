// Copyright (c) Spellbound Studios. All rights reserved.

using System;

namespace Spellbound.Core.Operations {
    [AttributeUsage(AttributeTargets.Assembly, Inherited = false, AllowMultiple = false)]
    public sealed class OperationGroupAttribute : Attribute {
        public string Name { get; }
        public string Job { get; }

        public OperationGroupAttribute(string name) : this(name, null) { }

        public OperationGroupAttribute(string name, string job) {
            if (string.IsNullOrWhiteSpace(name)) {
                throw new ArgumentException("Operation group name must not be null, empty or whitespace.", nameof(name));
            }

            Name = name;
            Job = job;
        }
    }
}
