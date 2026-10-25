// Copyright (c) Spellbound Studios. All rights reserved.

using System.Collections;
using NUnit.Framework;
using Spellbound.Core.Operations;
using UnityEngine;
using UnityEngine.TestTools;

namespace Spellbound.Core.PlayModeTests {
    public class OperationRunYieldTests {
        [UnityTest]
        public IEnumerator OperationThatCompletesLaterCanBeYielded() {
            var host = new OperationHost();
            var startFrame = Time.frameCount;
            var run = host.Run("fixture wait --milliseconds 120", OperationCaller.Development());
            Assert.IsFalse(run.IsDone);

            yield return run;

            Assert.IsTrue(run.IsDone);
            Assert.AreEqual("succeeded: waited 120", run.Result.Text);
            Assert.Greater(Time.frameCount, startFrame);
            Debug.Log("OperationRunYieldTests: waited " + (Time.frameCount - startFrame) + " frames for the run to complete");
        }

        [UnityTest]
        public IEnumerator CompletedOperationYieldsAtOnce() {
            var host = new OperationHost();
            var run = host.Run("fixture echo hello", OperationCaller.Development());
            Assert.IsTrue(run.IsDone);
            var startFrame = Time.frameCount;

            yield return run;

            Assert.LessOrEqual(Time.frameCount - startFrame, 1);
            Assert.AreEqual("succeeded: hello", run.Result.Text);
        }

        [UnityTest]
        public IEnumerator PlayingOnlyOperationRunsWhilePlaying() {
            var host = new OperationHost();
            var run = host.Run("fixture playing-only", OperationCaller.Development());
            yield return run;
            Assert.AreEqual("succeeded: playing", run.Result.Text);
            var editing = host.Run("fixture editing-only", OperationCaller.Development());
            yield return editing;
            Assert.AreEqual(OperationStatus.Refused, editing.Result.Status);
        }
    }
}
