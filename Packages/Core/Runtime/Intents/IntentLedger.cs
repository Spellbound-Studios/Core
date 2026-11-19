// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Collections.Generic;
using Spellbound.Core.Observations;
using Spellbound.Core.Simulation;

namespace Spellbound.Core.Intents {
    public sealed class IntentLedger {
        private const int EntryBytes = 12;

        private readonly int _windowSize;
        private readonly TickSpan _departureGrace;
        private readonly ObservationContext _observations;
        private readonly Dictionary<IntentActor, IntentWindow> _windows = new Dictionary<IntentActor, IntentWindow>();
        private readonly List<IntentWindow> _departed = new List<IntentWindow>();
        private readonly Stack<IntentWindow> _free = new Stack<IntentWindow>();

        public IntentLedger(int windowSize, TickSpan departureGrace, ObservationContext observations) {
            if (windowSize < 1) {
                throw new ArgumentOutOfRangeException(nameof(windowSize), windowSize,
                    "An intent window holds at least one entry.");
            }

            _windowSize = windowSize;
            _departureGrace = departureGrace;
            _observations = observations ?? throw new ArgumentNullException(nameof(observations));
        }

        public int ActorCount => _windows.Count;

        public long Bytes => (long)_windows.Count * _windowSize * EntryBytes;

        public IntentLedgerCheck Check(in IntentKey key, out IntentReceipt receipt) {
            receipt = IntentReceipt.None;

            if (key.Sequence == 0 || key.Actor == IntentActor.None) {
                CoreObservations.IntentStale.Add(_observations);
                return IntentLedgerCheck.Stale;
            }

            if (!_windows.TryGetValue(key.Actor, out var window)) {
                return IntentLedgerCheck.Unseen;
            }

            var check = Find(window, key.Sequence, out var slot);

            if (check == IntentLedgerCheck.Applied) {
                receipt = window.Receipts[slot];
                CoreObservations.IntentRepeats.Add(_observations);
            }
            else if (check == IntentLedgerCheck.Stale) {
                CoreObservations.IntentStale.Add(_observations);
            }

            return check;
        }

        public bool Record(in IntentKey key, in IntentReceipt receipt) {
            if (key.Sequence == 0 || key.Actor == IntentActor.None) {
                return false;
            }

            if (!_windows.TryGetValue(key.Actor, out var window)) {
                window = _free.Count > 0 ? _free.Pop() : new IntentWindow(_windowSize);
                window.Reset(key.Actor);
                _windows.Add(key.Actor, window);
                ReportBytes();
            }

            if (Find(window, key.Sequence, out var slot) != IntentLedgerCheck.Unseen) {
                return false;
            }

            window.Sequences[slot] = key.Sequence;
            window.Receipts[slot] = receipt;

            if (key.Sequence > window.Highest) {
                window.Highest = key.Sequence;
            }

            return true;
        }

        public void Depart(IntentActor actor, Tick now) {
            if (_windows.TryGetValue(actor, out var window)) {
                window.DropAt = now.Plus(_departureGrace);

                if (!window.IsDeparted) {
                    window.DepartedIndex = _departed.Count;
                    _departed.Add(window);
                }
            }
        }

        public void Return(IntentActor actor) {
            if (_windows.TryGetValue(actor, out var window) && window.IsDeparted) {
                RemoveDeparted(window);
            }
        }

        public int Expire(Tick now) {
            var dropped = 0;

            for (var index = _departed.Count - 1; index >= 0; index--) {
                var window = _departed[index];

                if (window.DropAt > now) {
                    continue;
                }

                Drop(window);
                dropped++;
            }

            if (dropped > 0) {
                CoreObservations.IntentWindowsDropped.Add(_observations, dropped);
                ReportBytes();
            }

            return dropped;
        }

        private IntentLedgerCheck Find(IntentWindow window, uint sequence, out int slot) {
            slot = (int)(sequence % (uint)_windowSize);

            if ((ulong)sequence + (ulong)_windowSize <= window.Highest) {
                return IntentLedgerCheck.Stale;
            }

            return window.Sequences[slot] == sequence ? IntentLedgerCheck.Applied : IntentLedgerCheck.Unseen;
        }

        private void Drop(IntentWindow window) {
            RemoveDeparted(window);
            _windows.Remove(window.Actor);
            _free.Push(window);
        }

        private void RemoveDeparted(IntentWindow window) {
            var index = window.DepartedIndex;
            var last = _departed.Count - 1;

            if (index != last) {
                var moved = _departed[last];
                _departed[index] = moved;
                moved.DepartedIndex = index;
            }

            _departed.RemoveAt(last);
            window.DepartedIndex = -1;
        }

        private void ReportBytes() {
            CoreObservations.IntentLedgerBytes.Set(_observations, Bytes);
        }
    }
}
