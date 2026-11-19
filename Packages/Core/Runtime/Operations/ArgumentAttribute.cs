// Copyright (c) Spellbound Studios. All rights reserved.

using System;

namespace Spellbound.Core.Operations {
    [AttributeUsage(AttributeTargets.Parameter, Inherited = false, AllowMultiple = false)]
    public sealed class ArgumentAttribute : Attribute {
        public string Description { get; }
        public bool Target { get; set; }

        public ArgumentAttribute(string description) {
            Description = description;
        }
    }
}
