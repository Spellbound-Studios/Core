// Copyright (c) Spellbound Studios. All rights reserved.

using NUnit.Framework;
using Spellbound.Core.Observations;
using Spellbound.Core.Simulation;
using Unity.Collections.LowLevel.Unsafe;

namespace Spellbound.Core.Tests {
    public class GuardStateSizeTests {
        [Test]
        public void AGuardStateIsThirtyTwoBytes() {
            Assert.AreEqual(32, UnsafeUtility.SizeOf<GuardState>());
        }

        [Test]
        public void EveryFieldKeepsItsOwnValue() {
            var state = new GuardState {
                GuardHash = 0xA1B2C3D4u,
                Subject = 0x1122334455667788UL,
                WindowStart = new Tick(0x0102030405060708UL),
                RepairCount = 0x7EADBEEF,
                HoldUntil = new Tick(0x8877665544332211UL)
            };

            Assert.AreEqual(0xA1B2C3D4u, state.GuardHash);
            Assert.AreEqual(0x1122334455667788UL, state.Subject);
            Assert.AreEqual(0x0102030405060708UL, state.WindowStart.Value);
            Assert.AreEqual(0x7EADBEEF, state.RepairCount);
            Assert.AreEqual(0x8877665544332211UL, state.HoldUntil.Value);
        }
    }
}
