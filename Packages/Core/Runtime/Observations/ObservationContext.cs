// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Collections.Generic;
using Spellbound.Core.Logging;
using Spellbound.Core.Simulation;

namespace Spellbound.Core.Observations {
    public sealed class ObservationContext : IDisposable {
        internal const string DiscardName = "discard";
        internal static readonly TickRate DefaultRate = TickRate.PerSecond(50);

        private readonly Dictionary<uint, ObservationSlot> _slotsByKey = new Dictionary<uint, ObservationSlot>();
        private readonly List<ObservationSlot> _slots = new List<ObservationSlot>();
        private readonly Dictionary<string, uint> _packageVersions =
            new Dictionary<string, uint>(StringComparer.Ordinal);
        private readonly Dictionary<(uint, ulong), GuardState> _guardStates =
            new Dictionary<(uint, ulong), GuardState>();
        private readonly Dictionary<uint, (int Repairs, TickSpan Window, TickSpan Hold)> _guardTicks =
            new Dictionary<uint, (int Repairs, TickSpan Window, TickSpan Hold)>();
        private readonly Dictionary<uint, GuardPolicyValues> _guardPolicies = new Dictionary<uint, GuardPolicyValues>();
        private readonly Dictionary<uint, (double RecordsPerSecond, double Credit, ulong LastTick,
            ObservationReason Reason)> _rateCaps =
            new Dictionary<uint, (double RecordsPerSecond, double Credit, ulong LastTick, ObservationReason Reason)>();
        private readonly Dictionary<uint, ObservationDeclaration> _queuedDeclarations =
            new Dictionary<uint, ObservationDeclaration>();
        private readonly HashSet<uint> _refusedQueuedKeys = new HashSet<uint>();
        private int _recorderCells = ObservationSettings.DefaultRecorderCells;
        private FlightRecorder _recorder;
        private bool _disposed;

        internal ObservationContext(string name, bool isDiscard, TickRate rate) {
            Name = name;
            IsDiscard = isDiscard;
            Rate = rate;
        }

        public static ObservationContext Discard { get; } =
            new ObservationContext(DiscardName, true, DefaultRate);

        public string Name { get; }

        public bool IsDiscard { get; }

        public TickRate Rate { get; }

        public bool IsDisposed => _disposed;

        internal int GuardEntryCount => _guardStates.Count;

        internal FlightRecorder Recorder {
            get {
                if (_recorder == null && !IsDiscard && !_disposed) {
                    _recorder = new FlightRecorder(_recorderCells);
                }

                return _recorder;
            }
        }

        internal int RecordCount => _recorder?.Count ?? 0;

        public int Read(List<ObservationReading> readings) {
            return Read(null, readings);
        }

        public int Read(string package, List<ObservationReading> readings) {
            if (readings == null) {
                throw new ArgumentNullException(nameof(readings));
            }

            readings.Clear();

            if (IsDiscard) {
                return 0;
            }

            for (var i = 0; i < _slots.Count; i++) {
                var slot = _slots[i];

                if (!slot.HasReading) {
                    continue;
                }

                if (package != null && !string.Equals(slot.Key.Package, package, StringComparison.Ordinal)) {
                    continue;
                }

                readings.Add(slot.Reading());
            }

            return readings.Count;
        }

        public bool TryRead(ObservationKey key, out ObservationReading reading) {
            if (!IsDiscard && _slotsByKey.TryGetValue(key.Hash, out var slot) && slot.HasReading) {
                reading = slot.Reading();
                return true;
            }

            reading = default;
            return false;
        }

        public int ReadRecords(List<RecordedOccurrence> records) {
            if (records == null) {
                throw new ArgumentNullException(nameof(records));
            }

            if (_recorder == null) {
                records.Clear();
                return 0;
            }

            return _recorder.Read(records);
        }

        public int ReadReasons(ObservationKey key, List<ReasonCount> reasons) {
            if (reasons == null) {
                throw new ArgumentNullException(nameof(reasons));
            }

            reasons.Clear();

            if (!IsDiscard && _slotsByKey.TryGetValue(key.Hash, out var slot)) {
                slot.ReadReasons(reasons);
            }

            return reasons.Count;
        }

        public uint Version(ObservationKey key) {
            return !IsDiscard && _slotsByKey.TryGetValue(key.Hash, out var slot) ? slot.Version : 0u;
        }

        public uint PackageVersion(string package) {
            return !IsDiscard && package != null && _packageVersions.TryGetValue(package, out var version)
                ? version
                : 0u;
        }

        public double Budget(ObservationKey key) {
            if (!IsDiscard && _slotsByKey.TryGetValue(key.Hash, out var slot)) {
                return slot.Budget;
            }

            var declaration = ObservationRegistry.Find(key);
            return declaration != null ? declaration.DefaultBudget : double.NaN;
        }

        public int QualityLevel(ObservationKey key) {
            if (!IsDiscard && _slotsByKey.TryGetValue(key.Hash, out var slot)) {
                return slot.QualityLevel;
            }

            return ObservationRegistry.Find(key)?.DefaultQualityLevel ?? 0;
        }

        public void SetBudget(ObservationKey key, double budget) {
            if (IsDiscard) {
                return;
            }

            var slot = Slot(key, null);

            if (slot.HasBudgetOverride && slot.BudgetOverride.Equals(budget)) {
                return;
            }

            slot.HasBudgetOverride = true;
            slot.BudgetOverride = budget;
            Bump(slot);
        }

        public void SetQualityLevel(ObservationKey key, int level) {
            if (IsDiscard) {
                return;
            }

            var slot = Slot(key, null);

            if (slot.HasQualityLevelOverride && slot.QualityLevelOverride == level) {
                return;
            }

            slot.HasQualityLevelOverride = true;
            slot.QualityLevelOverride = level;
            Bump(slot);
        }

        public NativeObservationQueue CreateQueue(int capacity) {
            return new NativeObservationQueue(this, capacity);
        }

        public void Dispose() {
            if (IsDiscard || _disposed) {
                return;
            }

            _disposed = true;

            if (_recorder != null) {
                _recorder.Dispose();
                _recorder = null;
            }

            ObservationContexts.Remove(this);
        }

        internal void AddCount(ObservationDeclaration declaration, ObservationReason reason, long amount) {
            if (amount < 0) {
                throw new ArgumentOutOfRangeException(nameof(amount), amount, "A counter only goes up.");
            }

            if (IsDiscard) {
                return;
            }

            var slot = Slot(declaration.Key, declaration);
            var first = !slot.HasReading;
            slot.HasReading = true;
            slot.Count++;

            if (!reason.IsNone) {
                ObservationReasons.Ensure(reason);
                slot.AddReason(reason, amount);
            }

            if (amount == 0 && !first) {
                return;
            }

            slot.Value += amount;
            slot.Total = slot.Value;

            if (amount > slot.Peak) {
                slot.Peak = amount;
            }

            Bump(slot);
        }

        internal void SetGuardPolicy(ObservationKey key, GuardPolicyValues values) {
            if (IsDiscard) {
                return;
            }

            _guardPolicies[key.Hash] = values;
            _guardTicks.Remove(key.Hash);
        }

        internal void SetRecorderCells(int cells) {
            if (cells < 1) {
                throw new ArgumentOutOfRangeException(nameof(cells), cells,
                    "A flight recorder needs at least one cell.");
            }

            if (IsDiscard || cells == _recorderCells) {
                return;
            }

            _recorderCells = cells;

            if (_recorder != null) {
                _recorder.Dispose();
                _recorder = null;
            }
        }

        internal void SetRateCap(ObservationKey key, double recordsPerSecond) {
            if (double.IsNaN(recordsPerSecond) || double.IsInfinity(recordsPerSecond) || recordsPerSecond < 0) {
                throw new ArgumentOutOfRangeException(nameof(recordsPerSecond), recordsPerSecond,
                    "A rate cap is a finite number of records a second, zero or more.");
            }

            if (IsDiscard) {
                return;
            }

            if (recordsPerSecond == 0) {
                _rateCaps.Remove(key.Hash);
                return;
            }

            _rateCaps[key.Hash] = (recordsPerSecond, RateCapBurst(recordsPerSecond), 0UL,
                ObservationReasons.Register(key.Name));
        }

        internal void FlagWindow(Tick from, Tick to) {
            _recorder?.FlagWindow(from, to);
        }

        internal (int Repairs, TickSpan Window, TickSpan Hold) GuardTicks(Guard guard) {
            var hash = guard.Key.Hash;

            if (_guardTicks.TryGetValue(hash, out var ticks)) {
                return ticks;
            }

            if (!_guardPolicies.TryGetValue(hash, out var policy)) {
                policy = guard.Defaults;
            }

            ticks = (policy.Repairs, Rate.FromSeconds(policy.WindowSeconds), Rate.FromSeconds(policy.HoldSeconds));
            _guardTicks.Add(hash, ticks);
            return ticks;
        }

        internal bool TryGetGuardState(uint guardHash, ulong subject, out GuardState state) {
            return _guardStates.TryGetValue((guardHash, subject), out state);
        }

        internal void SetGuardState(in GuardState state) {
            _guardStates[(state.GuardHash, state.Subject)] = state;
        }

        internal void RemoveGuardState(uint guardHash, ulong subject) {
            _guardStates.Remove((guardHash, subject));
        }

        internal void RecordOccurrence(ObservationDeclaration declaration) {
            if (IsDiscard || declaration.IsGuard) {
                return;
            }

            var slot = Slot(declaration.Key, declaration);
            slot.HasReading = true;
            slot.Count++;
            slot.Value = slot.Count;
            slot.Total = slot.Count;
            slot.Peak = slot.Count;
            Bump(slot);
        }

        internal void RecordOccurrence(ObservationDeclaration declaration, Tick tick, ulong subject,
            ReadOnlySpan<uint> values) {
            if (IsDiscard) {
                return;
            }

            if (values.Length > RecordedOccurrence.MaximumValues) {
                throw new ArgumentException(
                    "An occurrence records at most " + RecordedOccurrence.MaximumValues + " values; "
                    + values.Length + " were given.", nameof(values));
            }

            if (!declaration.IsGuard) {
                RecordOccurrence(declaration);
            }

            if (_disposed || OverRateCap(declaration.Key.Hash, tick)) {
                return;
            }

            Recorder.Write(declaration.Key.Hash, tick, subject, values);
        }

        internal void ApplyQueued(in NativeObservationEntry entry) {
            if (IsDiscard) {
                return;
            }

            var kind = (ObservationKind)entry.Kind;
            var declaration = QueuedDeclaration(entry.Key);

            if (declaration == null) {
                RefuseQueued(entry.Key, "which no observation declares");
                return;
            }

            if (declaration.IsGuard || declaration.Kind != kind) {
                RefuseQueued(entry.Key, "as a " + kind + " entry, but it declares " + declaration);
                return;
            }

            if (kind == ObservationKind.Occurrence) {
                RecordOccurrence(declaration, new Tick(entry.TickValue), entry.Subject, ReadOnlySpan<uint>.Empty);
                return;
            }

            if (entry.Amount < 0) {
                RefuseQueued(entry.Key, "with a negative amount, and a counter only goes up");
                return;
            }

            AddCount(declaration, ObservationReason.None, entry.Amount);
        }

        internal void SetGauge(ObservationDeclaration declaration, double value) {
            if (IsDiscard) {
                return;
            }

            var slot = Slot(declaration.Key, declaration);
            var first = !slot.HasReading;
            slot.HasReading = true;
            slot.Count++;

            if (first || value > slot.Peak) {
                slot.Peak = value;
            }

            if (!first && slot.Value.Equals(value)) {
                return;
            }

            slot.Value = value;
            slot.Total = value;
            Bump(slot);
        }

        internal void RecordCost(ObservationDeclaration declaration, double amount) {
            if (IsDiscard) {
                return;
            }

            var slot = Slot(declaration.Key, declaration);
            var first = !slot.HasReading;
            slot.HasReading = true;
            slot.Count++;
            slot.Value = amount;
            slot.Total += amount;

            if (first || amount > slot.Peak) {
                slot.Peak = amount;
            }

            Bump(slot);
        }

        private ObservationSlot Slot(ObservationKey key, ObservationDeclaration declaration) {
            if (declaration != null) {
                ObservationRegistry.Ensure(declaration);
            }

            if (!_slotsByKey.TryGetValue(key.Hash, out var slot)) {
                slot = new ObservationSlot(key, declaration);
                _slotsByKey.Add(key.Hash, slot);
                _slots.Add(slot);
                return slot;
            }

            if (declaration != null) {
                slot.Declaration = declaration;
            }

            return slot;
        }

        private ObservationDeclaration QueuedDeclaration(uint keyHash) {
            if (_queuedDeclarations.TryGetValue(keyHash, out var declaration)) {
                return declaration;
            }

            if (_refusedQueuedKeys.Contains(keyHash)) {
                return null;
            }

            var declared = ObservationRegistry.Declared();

            for (var i = 0; i < declared.Count; i++) {
                _queuedDeclarations[declared[i].Key.Hash] = declared[i];
            }

            return _queuedDeclarations.TryGetValue(keyHash, out declaration) ? declaration : null;
        }

        private void RefuseQueued(uint keyHash, string failure) {
            if (!_refusedQueuedKeys.Add(keyHash)) {
                return;
            }

            Log.Error("The observation context '" + Name + "' drained the key hash " + keyHash + " from a native"
                      + " queue " + failure + "; its entries are skipped.");
        }

        private bool OverRateCap(uint keyHash, Tick tick) {
            if (!_rateCaps.TryGetValue(keyHash, out var cap)) {
                return false;
            }

            var credit = cap.Credit;

            if (tick.Value > cap.LastTick) {
                credit = Math.Min(RateCapBurst(cap.RecordsPerSecond),
                    credit + (tick.Value - cap.LastTick) * cap.RecordsPerSecond);
                cap.LastTick = tick.Value;
            }

            if (credit >= Rate.TicksPerSecond) {
                cap.Credit = credit - Rate.TicksPerSecond;
                _rateCaps[keyHash] = cap;
                return false;
            }

            cap.Credit = credit;
            _rateCaps[keyHash] = cap;
            AddCount(CoreObservations.RecorderDropped.Declaration, cap.Reason, 1);
            return true;
        }

        private double RateCapBurst(double recordsPerSecond) => Math.Max(Rate.TicksPerSecond, recordsPerSecond);

        private void Bump(ObservationSlot slot) {
            slot.Version++;
            _packageVersions.TryGetValue(slot.Key.Package, out var version);
            _packageVersions[slot.Key.Package] = version + 1;
        }

        public override string ToString() => Name;
    }
}
