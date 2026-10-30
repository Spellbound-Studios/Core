// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Spellbound.Core.Operations;

namespace Spellbound.Core.Observations {
    public static class ObservationOperations {
        private static readonly List<ReasonCount> Reasons = new List<ReasonCount>();

        [Operation("observations",
            "Lists the declared observation keys with their readings in one world: kind, unit, value, count, peak,"
            + " budget and quality level.",
            OperationEffect.Reads, Audience = OperationAudiences.Anyone)]
        public static OperationResult Observations(OperationContext context,
            [Argument("The world whose readings to show; leave it out when one world is live.")] string world = null,
            [Argument("Only the keys that start with this text.")] string prefix = null) {
            if (!TryResolveContext(world, out var observations, out var failure)) {
                return failure;
            }

            var declared = CoreKit.Observations.Declared();
            var table = new OperationTable("key", "kind", "unit", "value", "count", "peak", "budget", "level");
            var keys = 0;
            var withReadings = 0;

            foreach (var declaration in declared) {
                if (!Matches(declaration.Key, prefix)) {
                    continue;
                }

                keys++;

                if (observations != null && observations.TryRead(declaration.Key, out var reading)) {
                    withReadings++;
                    table.AddRow(declaration.Key.Name, Kind(declaration.Kind), declaration.Unit.Name,
                        Number(reading.Value),
                        reading.Count.ToString(CultureInfo.InvariantCulture), Number(reading.Peak),
                        Budget(reading.Budget),
                        reading.QualityLevel.ToString(CultureInfo.InvariantCulture));
                    continue;
                }

                table.AddRow(declaration.Key.Name, Kind(declaration.Kind), declaration.Unit.Name, string.Empty,
                    string.Empty,
                    string.Empty, Budget(declaration.DefaultBudget),
                    declaration.DefaultQualityLevel.ToString(CultureInfo.InvariantCulture));
            }

            var builder = new StringBuilder();
            builder.Append(keys).Append(keys == 1 ? " key, " : " keys, ").Append(withReadings).Append(" with readings");

            if (observations != null) {
                builder.Append(" in world '").Append(observations.Name).Append('\'');
            }
            else {
                builder.Append("; no world is live, so the declarations alone are listed");
            }

            if (keys > 0) {
                builder.Append('\n').Append(table.ToText());
            }

            return OperationResult.Success(builder.ToString());
        }

        [Operation("observation",
            "Prints one observation key: its declaration, its reading in one world and the counts by reason.",
            OperationEffect.Reads, Audience = OperationAudiences.Anyone)]
        public static OperationResult Observation(OperationContext context,
            [Argument("The key, as package.subject.name.")] string key,
            [Argument("The world whose reading to show; leave it out when one world is live.")] string world = null) {
            if (!ObservationKey.TryParse(key, out var parsed, out var error)) {
                return OperationResult.Invalid(error + "\nRun: core observations");
            }

            if (!TryResolveContext(world, out var observations, out var failure)) {
                return failure;
            }

            ObservationDeclaration declaration = null;

            foreach (var candidate in CoreKit.Observations.Declared()) {
                if (candidate.Key == parsed) {
                    declaration = candidate;
                    break;
                }
            }

            if (declaration == null) {
                return OperationResult.Invalid("no observation key '" + key + "' is declared\nRun: core observations");
            }

            var builder = new StringBuilder();
            builder.Append(declaration.Key.Name).Append(": ").Append(Kind(declaration.Kind)).Append(" in ")
                .Append(declaration.Unit.Name);

            if (declaration.HasDefaultBudget) {
                builder.Append(", default budget ").Append(Number(declaration.DefaultBudget));
            }

            builder.Append(", default quality level ").Append(declaration.DefaultQualityLevel);
            builder.Append('\n').Append(declaration.Description);

            if (observations == null) {
                builder.Append("\nno world is live");
                return OperationResult.Success(builder.ToString());
            }

            if (!observations.TryRead(parsed, out var reading)) {
                builder.Append("\nno reading in world '").Append(observations.Name).Append('\'');
                return OperationResult.Success(builder.ToString());
            }

            builder.Append("\nworld '").Append(observations.Name).Append("': value ").Append(Number(reading.Value))
                .Append(", count ").Append(reading.Count).Append(", peak ").Append(Number(reading.Peak))
                .Append(", total ").Append(Number(reading.Total)).Append(", budget ").Append(Budget(reading.Budget))
                .Append(", level ").Append(reading.QualityLevel).Append(", version ").Append(reading.Version);

            if (observations.ReadReasons(parsed, Reasons) > 0) {
                Reasons.Sort((left, right) => right.Count.CompareTo(left.Count));
                var reasons = new OperationTable("reason", "count");

                foreach (var entry in Reasons) {
                    reasons.AddRow(entry.Reason.Name ?? entry.Reason.Id.ToString(CultureInfo.InvariantCulture),
                        entry.Count.ToString(CultureInfo.InvariantCulture));
                }

                builder.Append('\n').Append(reasons.ToText());
            }

            return OperationResult.Success(builder.ToString());
        }

        private static bool TryResolveContext(string world, out ObservationContext observations,
            out OperationResult failure) {
            failure = null;
            var live = ObservationContexts.All;

            if (!string.IsNullOrWhiteSpace(world)) {
                observations = ObservationContexts.Find(world.Trim());

                if (observations != null) {
                    return true;
                }

                failure = OperationResult.Invalid("no world named '" + world + "' is live" + Worlds(live));
                return false;
            }

            if (live.Count == 0) {
                observations = null;
                return true;
            }

            if (live.Count == 1) {
                observations = live[0];
                return true;
            }

            observations = null;
            failure = OperationResult.Invalid("several worlds are live; name one with --world" + Worlds(live));
            return false;
        }

        private static string Worlds(IReadOnlyList<ObservationContext> live) {
            if (live.Count == 0) {
                return string.Empty;
            }

            var names = new string[live.Count];

            for (var i = 0; i < live.Count; i++) {
                names[i] = live[i].Name;
            }

            return "\nLive worlds: " + string.Join(", ", names);
        }

        private static bool Matches(ObservationKey key, string prefix) {
            return string.IsNullOrEmpty(prefix) || key.Name.StartsWith(prefix.Trim(), StringComparison.Ordinal);
        }

        private static string Kind(ObservationKind kind) => kind.ToString().ToLowerInvariant();

        private static string Budget(double budget) {
            return double.IsNaN(budget) ? string.Empty : Number(budget);
        }

        private static string Number(double value) {
            if (Math.Abs(value - Math.Round(value)) < 0.0000001 && Math.Abs(value) < 1e15) {
                return ((long)Math.Round(value)).ToString(CultureInfo.InvariantCulture);
            }

            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }
    }
}
