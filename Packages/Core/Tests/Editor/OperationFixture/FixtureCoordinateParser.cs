// Copyright (c) Spellbound Studios. All rights reserved.

using Spellbound.Core.Operations;

namespace Spellbound.Core.Tests.OperationFixture {
    [ArgumentParser]
    public sealed class FixtureCoordinateParser : IArgumentParser<FixtureCoordinate> {
        public string Expected => "column:row";

        public bool TryParse(string text, out FixtureCoordinate value) {
            value = default;
            var parts = text.Split(':');

            if (parts.Length != 2 || !int.TryParse(parts[0], out var column) || !int.TryParse(parts[1], out var row)) {
                return false;
            }

            value = new FixtureCoordinate(column, row);
            return true;
        }
    }
}
