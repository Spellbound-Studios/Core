// Copyright (c) Spellbound Studios. All rights reserved.

namespace Spellbound.Core.Operations {
    public interface IArgumentParser<T> {
        string Expected { get; }
        bool TryParse(string text, out T value);
    }
}
