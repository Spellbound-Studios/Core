// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Collections.Generic;

namespace Spellbound.Core.Operations {
    public sealed class OperationCaller {
        private readonly HashSet<string> _audiences;

        public string Name { get; }
        public IReadOnlyCollection<string> Audiences => _audiences;

        public OperationCaller(string name, params string[] audiences) {
            if (string.IsNullOrWhiteSpace(name)) {
                throw new ArgumentException("Caller name must not be null, empty or whitespace.", nameof(name));
            }

            Name = name;
            _audiences = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (audiences == null) {
                return;
            }

            foreach (var audience in audiences) {
                if (!string.IsNullOrWhiteSpace(audience)) {
                    _audiences.Add(audience);
                }
            }
        }

        public bool Holds(string audience) {
            if (string.IsNullOrEmpty(audience)) {
                return true;
            }

            return string.Equals(audience, OperationAudiences.Anyone, StringComparison.OrdinalIgnoreCase)
                   || _audiences.Contains(audience);
        }

        public static OperationCaller Development() {
            return new OperationCaller("development", OperationAudiences.Developer);
        }

        public override string ToString() {
            return Name;
        }
    }
}
