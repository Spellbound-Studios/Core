// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using Spellbound.Core.Authoring;

namespace Spellbound.Core.Authoring.Editor {
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class RegisteredDefinitionCheckAttribute : Attribute {
        public string Name { get; }
        public bool RunsWhileEditing { get; set; }

        public RegisteredDefinitionCheckAttribute(string name) {
            if (!ValidationFinding.IsCode(name)) {
                throw new ArgumentException("A check name is <package>.<kebab-name> in lower case; '" + name + "' is not.",
                    nameof(name));
            }

            Name = name;
        }
    }
}
