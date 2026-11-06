// Copyright (c) Spellbound Studios. All rights reserved.

using Spellbound.Core.Operations;

namespace Spellbound.Core.Tests.OperationFixture {
    public sealed class FixtureLiveCounter {
        public int Count { get; private set; }

        [Operation("count-up", "Raises the live counter.", OperationEffect.Writes)]
        public string CountUp([Argument("How much to add.")] int amount = 1) {
            Count += amount;
            return Count.ToString();
        }
    }
}
