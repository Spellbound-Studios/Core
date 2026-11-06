// Copyright (c) Spellbound Studios. All rights reserved.

namespace Spellbound.Core.Operations {
    internal delegate bool TryParseArgument<T>(string text, out T value);

    internal sealed class DelegateArgumentParser<T> : IArgumentParser<T> {
        private readonly TryParseArgument<T> _parse;

        public string Expected { get; }

        public DelegateArgumentParser(string expected, TryParseArgument<T> parse) {
            Expected = expected;
            _parse = parse;
        }

        public bool TryParse(string text, out T value) {
            return _parse(text, out value);
        }
    }
}
