// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Spellbound.Core.Operations {
    public sealed class OperationRun : CustomYieldInstruction {
        private readonly List<OperationResult> _results = new List<OperationResult>();
        private readonly List<string> _lines = new List<string>();
        private TaskCompletionSource<OperationResult> _completion;
        private Action<OperationResult> _whenDone;

        public bool IsDone { get; private set; }
        public OperationResult Result => IsDone ? _results[_results.Count - 1] : null;
        public IReadOnlyList<OperationResult> Results => _results;
        public IReadOnlyList<string> Lines => _lines;
        public override bool keepWaiting => !IsDone;

        public event Action<string> Reported;

        internal OperationRun() { }

        public static OperationRun Completed(OperationResult result) {
            var run = new OperationRun();
            run.Complete(result);
            return run;
        }

        public Task<OperationResult> AsTask() {
            if (IsDone) {
                return Task.FromResult(Result);
            }

            _completion ??= new TaskCompletionSource<OperationResult>();
            return _completion.Task;
        }

        internal void Report(string line) {
            if (line == null) {
                return;
            }

            _lines.Add(line);
            Reported?.Invoke(line);
        }

        internal void Append(OperationResult result) {
            if (result != null) {
                _results.Add(result);
            }
        }

        internal void Complete(OperationResult result) {
            if (IsDone) {
                throw new InvalidOperationException("The operation run has already completed.");
            }

            _results.Add(result ?? OperationResult.Failed("the operation produced no result"));
            IsDone = true;
            _completion?.TrySetResult(Result);
            var whenDone = _whenDone;
            _whenDone = null;
            whenDone?.Invoke(Result);
        }

        internal void WhenDone(Action<OperationResult> continuation) {
            if (IsDone) {
                continuation(Result);
                return;
            }

            _whenDone += continuation;
        }
    }
}
