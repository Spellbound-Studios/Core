// Copyright (c) Spellbound Studios. All rights reserved.

using NUnit.Framework;
using Spellbound.Core.Operations;

namespace Spellbound.Core.Tests {
    public class OperationResultTests {
        [Test]
        public void TheStatusWordComesFirst() {
            Assert.AreEqual("succeeded: done", OperationResult.Success("done").Text);
            Assert.AreEqual("invalid: bad", OperationResult.Invalid("bad").Text);
            Assert.AreEqual("failed: broke", OperationResult.Failed("broke").Text);
            Assert.AreEqual("refused: no", OperationResult.Refused("no").Text);
            Assert.AreEqual("previewed: would", OperationResult.Previewed("would").Text);
            Assert.AreEqual("succeeded", OperationResult.Success(null).Text);
        }

        [Test]
        public void SucceededCoversPreviewed() {
            Assert.IsTrue(OperationResult.Success("x").Succeeded);
            Assert.IsTrue(OperationResult.Previewed("x").Succeeded);
            Assert.IsFalse(OperationResult.Refused("x").Succeeded);
            Assert.IsFalse(OperationResult.Failed("x").Succeeded);
            Assert.IsFalse(OperationResult.Invalid("x").Succeeded);
        }

        [Test]
        public void ValueTravelsWithTheResult() {
            var value = new object();
            Assert.AreSame(value, OperationResult.Success("x", value).Value);
        }

        [Test]
        public void CallerHoldsAnyoneAndItsOwnAudiences() {
            var caller = new OperationCaller("tester", "developer");
            Assert.IsTrue(caller.Holds(OperationAudiences.Anyone));
            Assert.IsTrue(caller.Holds("Developer"));
            Assert.IsFalse(caller.Holds(OperationAudiences.Approver));
            Assert.IsTrue(OperationCaller.Development().Holds(OperationAudiences.Developer));
        }
    }
}
