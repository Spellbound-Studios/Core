// Copyright (c) Spellbound Studios. All rights reserved.

using System;

namespace Spellbound.Core.Tests.AuthoringFixture {
    [Serializable]
    public class FixtureThrowingLayer : FixtureLayer {
        public const string ConstructionMessage = "the fixture layer was told to throw while constructed";

        public static bool ThrowOnConstruction { get; set; }

        public FixtureThrowingLayer() {
            if (ThrowOnConstruction) {
                throw new InvalidOperationException(ConstructionMessage);
            }
        }
    }
}
