// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Collections.Generic;
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

        public void Dispose() {
            if (IsDiscard || _disposed) {
                return;
            }

            _disposed = true;
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

        private void Bump(ObservationSlot slot) {
            slot.Version++;
            _packageVersions.TryGetValue(slot.Key.Package, out var version);
            _packageVersions[slot.Key.Package] = version + 1;
        }

        public override string ToString() => Name;
    }
}
