// Copyright (c) Spellbound Studios. All rights reserved.

using System;

namespace Spellbound.Core.Operations {
    internal interface IArgumentParserAdapter {
        Type ArgumentType { get; }
        string Expected { get; }
        bool TryParse(string text, out object value);
    }

    internal sealed class ArgumentParserAdapter<T> : IArgumentParserAdapter {
        private readonly IArgumentParser<T> _parser;

        public Type ArgumentType => typeof(T);
        public string Expected => _parser.Expected;

        public ArgumentParserAdapter(IArgumentParser<T> parser) {
            _parser = parser;
        }

        public bool TryParse(string text, out object value) {
            if (_parser.TryParse(text, out var typed)) {
                value = typed;
                return true;
            }

            value = null;
            return false;
        }
    }
}
