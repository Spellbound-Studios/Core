// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Collections.Generic;
using NUnit.Framework;
using Spellbound.Core.Operations;

namespace Spellbound.Core.Tests.PublicSurface {
    public class OperationRunTests {
        [Test]
        public void ANewRunWaitsUntilItIsCompleted() {
            var run = new OperationRun();
            Assert.IsFalse(run.IsDone);
            Assert.IsTrue(run.keepWaiting);
            Assert.IsNull(run.Result);
        }

        [Test]
        public void ReportedLinesReachTheRunAndItsListeners() {
            var run = new OperationRun();
            var heard = new List<string>();
            run.Reported += heard.Add;
            run.Report("first");
            run.Report("second");
            CollectionAssert.AreEqual(new[] { "first", "second" }, run.Lines);
            CollectionAssert.AreEqual(new[] { "first", "second" }, heard);
        }

        [Test]
        public void CompletingARunHandsItsResultToWhenDone() {
            var run = new OperationRun();
            OperationResult observed = null;
            run.WhenDone(result => observed = result);
            Assert.IsNull(observed);
            var answer = OperationResult.Success("answered");
            run.Complete(answer);
            Assert.IsTrue(run.IsDone);
            Assert.IsFalse(run.keepWaiting);
            Assert.AreSame(answer, run.Result);
            Assert.AreSame(answer, observed);
        }

        [Test]
        public void WhenDoneOnACompletedRunAnswersAtOnce() {
            var run = new OperationRun();
            var answer = OperationResult.Failed("the deciding peer refused");
            run.Complete(answer);
            OperationResult observed = null;
            run.WhenDone(result => observed = result);
            Assert.AreSame(answer, observed);
        }

        [Test]
        public void TheTaskOfAWaitingRunCompletesWithItsResult() {
            var run = new OperationRun();
            var task = run.AsTask();
            Assert.IsFalse(task.IsCompleted);
            var answer = OperationResult.Success("answered");
            run.Complete(answer);
            Assert.IsTrue(task.IsCompleted);
            Assert.AreSame(answer, task.Result);
        }

        [Test]
        public void ARunCompletesOnce() {
            var run = new OperationRun();
            run.Complete(OperationResult.Success("answered"));
            Assert.Throws<InvalidOperationException>(() => run.Complete(OperationResult.Success("again")));
        }
    }
}
