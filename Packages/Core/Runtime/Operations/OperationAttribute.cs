// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using UnityEngine.Scripting;

namespace Spellbound.Core.Operations {
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public sealed class OperationAttribute : PreserveAttribute {
        public string Name { get; }
        public string Description { get; }
        public OperationEffect Effect { get; }
        public string Audience { get; set; } = OperationAudiences.Developer;
        public OperationAvailability Availability { get; set; } = OperationAvailability.Anytime;

        public OperationAttribute(string name, string description, OperationEffect effect) {
            Name = name;
            Description = description;
            Effect = effect;
        }
    }
}
