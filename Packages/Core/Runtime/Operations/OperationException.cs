// Copyright (c) Spellbound Studios. All rights reserved.

using System;

namespace Spellbound.Core.Operations {
    public sealed class OperationException : Exception {
        public string Hint { get; }

        public OperationException(string message) : this(message, null) { }

        public OperationException(string message, string hint) : base(message) {
            Hint = hint;
        }
    }
}
