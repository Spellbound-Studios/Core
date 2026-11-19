// Copyright (c) Spellbound Studios. All rights reserved.

using Spellbound.Core.Logging;
using Spellbound.Core.Observations;

namespace Spellbound.Core.Operations {
    internal sealed class OperationGroupObservations {
        private const string DurationSuffix = ".operations";
        private const string FailuresSuffix = ".operation_failures";

        private readonly Cost _duration;
        private readonly Counter _failures;

        private OperationGroupObservations(Cost duration, Counter failures) {
            _duration = duration;
            _failures = failures;
        }

        internal static OperationGroupObservations Declare(string group) {
            var groupKey = (group ?? string.Empty).ToLowerInvariant();

            if (!ObservationKey.TryParse(groupKey + DurationSuffix, out _, out var error)) {
                Log.Error("The operation group '" + group + "' records no observations: " + error);
                return null;
            }

            var duration = CoreKit.Observations.Cost(groupKey + DurationSuffix,
                "The duration in milliseconds of each operation of the group '" + groupKey
                + "' run by a host; the count is the calls and the peak the slowest call.",
                ObservationUnit.Milliseconds);
            var failures = CoreKit.Observations.Counter(groupKey + FailuresSuffix,
                "An operation of the group '" + groupKey + "' answered invalid, failed or refused."
                + " The reason is the operation line.");
            return new OperationGroupObservations(duration, failures);
        }

        internal void Record(ObservationContext observations, OperationDescription description,
            double elapsedMilliseconds,
            bool succeeded) {
            if (observations == null || observations.IsDiscard) {
                return;
            }

            _duration.Record(observations, elapsedMilliseconds);

            if (!succeeded) {
                _failures.Add(observations, CoreKit.Observations.Reason(description.Line));
            }
        }
    }
}
