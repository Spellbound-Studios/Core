// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Collections.Generic;

namespace Spellbound.Core.Operations {
    public interface IArgumentTypeParser {
        bool CanParse(Type type);
        string Expected(Type type);
        IReadOnlyList<string> LegalValues(Type type);
        bool TryParse(string text, Type type, out object value);
    }
}
