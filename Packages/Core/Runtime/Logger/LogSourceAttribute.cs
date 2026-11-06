// Copyright 2026 Spellbound Studio Inc.

using System;

namespace Spellbound.Core.Logging {
    [AttributeUsage(AttributeTargets.Assembly)]
    public class LogSourceAttribute : Attribute {
        public string Name { get; }

        public LogSourceAttribute(string name) {
            Name = name;
        }
    }
}