// Copyright (c) Spellbound Studios. All rights reserved.

using System;

namespace Spellbound.Core.Sections {
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
    public sealed class SectionAttribute : Attribute {
        public string Name { get; }

        public SectionAttribute(string name) {
            if (string.IsNullOrWhiteSpace(name)) {
                throw new ArgumentException("Section name must not be null, empty or whitespace.", nameof(name));
            }

            Name = name;
        }
    }
}
