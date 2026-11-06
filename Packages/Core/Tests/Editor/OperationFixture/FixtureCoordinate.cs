// Copyright (c) Spellbound Studios. All rights reserved.

namespace Spellbound.Core.Tests.OperationFixture {
    public readonly struct FixtureCoordinate {
        public int Column { get; }
        public int Row { get; }

        public FixtureCoordinate(int column, int row) {
            Column = column;
            Row = row;
        }
    }
}
