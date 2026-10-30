// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Collections.Generic;
using System.Reflection;

namespace Spellbound.Core.Operations {
    public sealed class ArgumentDescription {
        public string Name { get; }
        public Type Type { get; }
        public string Summary { get; }
        public bool HasDefault { get; }
        public object Default { get; }
        public IReadOnlyList<string> LegalValues { get; }

        internal ParameterInfo Parameter { get; }

        internal ArgumentDescription(ParameterInfo parameter, string name, string summary, IReadOnlyList<string> legalValues) {
            Parameter = parameter;
            Name = name;
            Type = parameter.ParameterType;
            Summary = summary;
            HasDefault = parameter.HasDefaultValue;
            Default = parameter.HasDefaultValue ? parameter.DefaultValue : null;
            LegalValues = legalValues ?? Array.Empty<string>();
        }

        public string DefaultText => HasDefault ? ArgumentText.Describe(Default) : null;
    }
}
