// Copyright (c) Spellbound Studios. All rights reserved.

using System;

namespace Spellbound.Core.Operations {
    [AttributeUsage(AttributeTargets.Parameter, Inherited = false, AllowMultiple = false)]
    public sealed class ArgumentAttribute : Attribute {
        public string Description { get; }

        public ArgumentAttribute(string description) {
            if (string.IsNullOrWhiteSpace(description)) {
                throw new ArgumentException("Argument description must not be null, empty or whitespace.", nameof(description));
            }

            Description = description;
        }
    }
}
