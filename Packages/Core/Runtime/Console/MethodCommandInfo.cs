// Copyright 2026 Spellbound Studio Inc.

using System;
using System.Reflection;

namespace Spellbound.Core.Console {
    /// <summary>
    /// Utility command metadata.
    /// This allows us to track the declaring class and other things for later use while wrapping Reflection's MethodInfo.
    /// </summary>
    public class MethodCommandInfo {
        public MethodInfo Method { get; set; }
        public Type DeclaringClass { get; set; }
        public string Description { get; set; }
    }
}