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

        /// <summary>
        /// The module type required for preset commands.
        /// Null for utility commands.
        /// </summary>
        public Type RequiredModuleType { get; set; }

        /// <summary>
        /// True if this is a utility command (no module requirement).
        /// </summary>
        public bool IsUtilityCommand => RequiredModuleType == null;

        /// <summary>
        /// True if this is a preset command (requires a specific module).
        /// </summary>
        public bool IsPresetCommand => RequiredModuleType != null;
    }
}