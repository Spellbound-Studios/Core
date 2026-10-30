// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Spellbound.Core.Logging;
using Spellbound.Core.Observations;
using UnityEngine;

namespace Spellbound.Core.Operations {
    public sealed class OperationHost {
        private const string TraceSource = "OperationHost";

        private readonly Func<OperationSettings> _settingsLoader;
        private readonly List<OperationHistoryEntry> _history = new List<OperationHistoryEntry>();
        private readonly Dictionary<string, OperationStatistic> _statistics = new Dictionary<string, OperationStatistic>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, OperationGroupObservations> _groupObservations =
            new Dictionary<string, OperationGroupObservations>(StringComparer.OrdinalIgnoreCase);
        private OperationSettings _settings;

        public OperationCatalog Catalog { get; }
        public ArgumentParserRegistry Parsers { get; }
        public ResultRendererRegistry Renderers { get; }
        public IReadOnlyList<OperationHistoryEntry> History => _history;

        public IReadOnlyList<OperationStatistic> Statistics {
            get {
                var snapshot = new List<OperationStatistic>(_statistics.Values);
                snapshot.Sort((left, right) => string.CompareOrdinal(left.Line, right.Line));
                return snapshot;
            }
        }

        public OperationSettings Settings {
            get => _settings ??= (_settingsLoader != null ? _settingsLoader() : null) ?? OperationSettings.Load();
            set => _settings = value;
        }

        public OperationHost() : this(null, null) { }

        public OperationHost(OperationCatalog catalog, Func<OperationSettings> settingsLoader) {
            Catalog = catalog ?? new OperationCatalog();
            Parsers = new ArgumentParserRegistry(Catalog.MarkedAssemblies);
            Renderers = new ResultRendererRegistry(Catalog.MarkedAssemblies);
            _settingsLoader = settingsLoader;

            foreach (var group in Catalog.Groups) {
                GroupObservations(group.Name);
            }
        }

        public OperationRun Run(string line, OperationCaller caller, ObservationContext observations) {
            return Run(line, caller, null, observations);
        }

        public OperationRun Run(string line, OperationCaller caller, IReadOnlyDictionary<string, string> placeholders,
            ObservationContext observations) {
            if (caller == null) {
                throw new ArgumentNullException(nameof(caller));
            }

            if (observations == null) {
                throw new ArgumentNullException(nameof(observations));
            }

            var run = new OperationRun();
            var text = line ?? string.Empty;

            if (!TryPrepare(text, placeholders, out var description, out var bound, out var failure)) {
                Finish(run, text, caller, observations, failure, null, null);
                return run;
            }

            Execute(run, description, bound.Values, text, caller, observations, bound.Confirmed, bound.DryRun,
                CancellationToken.None);
            return run;
        }

        public OperationRun Run(OperationInvocation invocation, OperationCaller caller,
            ObservationContext observations) {
            if (invocation == null) {
                throw new ArgumentNullException(nameof(invocation));
            }

            if (caller == null) {
                throw new ArgumentNullException(nameof(caller));
            }

            if (observations == null) {
                throw new ArgumentNullException(nameof(observations));
            }

            var run = new OperationRun();
            var description = Catalog.Describe(invocation.Identifier);
            var text = invocation.Line ?? ("#" + invocation.Identifier);

            if (description == null) {
                Finish(run, text, caller, observations,
                    OperationResult.Invalid("no operation has the identifier " + invocation.Identifier),
                    null, null);
                return run;
            }

            if (!ArgumentBinder.TryBindValues(description, invocation.Values, out var values, out var error)) {
                Finish(run, text, caller, observations, OperationResult.Invalid(error), description, null);
                return run;
            }

            Execute(run, description, values, invocation.Line ?? description.Line, caller, observations,
                invocation.Confirmed,
                invocation.DryRun, CancellationToken.None);
            return run;
        }

        public OperationRun RunAll(IReadOnlyList<string> lines, OperationCaller caller,
            ObservationContext observations) {
            return RunAll(lines, caller, null, observations);
        }

        public OperationRun RunAll(IReadOnlyList<string> lines, OperationCaller caller,
            IReadOnlyDictionary<string, string> placeholders, ObservationContext observations) {
            if (lines == null) {
                throw new ArgumentNullException(nameof(lines));
            }

            if (observations == null) {
                throw new ArgumentNullException(nameof(observations));
            }

            var chain = new OperationRun();

            if (lines.Count == 0) {
                chain.Complete(OperationResult.Success("no lines to run"));
                return chain;
            }

            RunFrom(chain, lines, 0, caller, placeholders, observations);
            return chain;
        }

        public OperationInvocation Parse(string line, IReadOnlyDictionary<string, string> placeholders, out OperationResult failure) {
            if (!TryPrepare(line ?? string.Empty, placeholders, out var description, out var bound, out failure)) {
                return null;
            }

            return new OperationInvocation(description.Identifier, bound.Values, line, bound.Confirmed, bound.DryRun);
        }

        private void RunFrom(OperationRun chain, IReadOnlyList<string> lines, int index, OperationCaller caller,
            IReadOnlyDictionary<string, string> placeholders, ObservationContext observations) {
            var run = Run(lines[index], caller, placeholders, observations);

            run.WhenDone(result => {
                foreach (var line in run.Lines) {
                    chain.Report(line);
                }

                if (result.Status != OperationStatus.Succeeded || index == lines.Count - 1) {
                    chain.Complete(result);
                    return;
                }

                chain.Append(result);
                RunFrom(chain, lines, index + 1, caller, placeholders, observations);
            });
        }

        private bool TryPrepare(string line, IReadOnlyDictionary<string, string> placeholders,
            out OperationDescription description, out ArgumentBinder.Bound bound, out OperationResult failure) {
            description = null;
            bound = null;
            failure = null;
            var settings = Settings;

            if (line.Length > settings.maximumLineLength) {
                failure = OperationResult.Invalid("the line is " + line.Length.ToString("N0") + " characters; the maximum is set in OperationSettings ("
                                                  + settings.maximumLineLength.ToString("N0") + ")");
                return false;
            }

            if (!LineTokenizer.TryTokenize(line, out var tokens, out var error)) {
                failure = OperationResult.Invalid(error);
                return false;
            }

            if (tokens.Count == 0) {
                failure = OperationResult.Invalid("the line is empty\nRun: core help");
                return false;
            }

            if (!LineTokenizer.TryFillPlaceholders(tokens, placeholders, out error)) {
                failure = OperationResult.Invalid(error);
                return false;
            }

            if (!TryResolve(tokens, out description, out var consumed, out failure)) {
                return false;
            }

            if (settings.IsGroupDisabled(description.Group)) {
                failure = OperationResult.Refused("the group '" + description.Group + "' is disabled in OperationSettings");
                return false;
            }

            tokens.RemoveRange(0, consumed);

            if (!ArgumentBinder.TryBind(description, tokens, Parsers, out bound, out error)) {
                failure = OperationResult.Invalid(error + "\nRun: core help --group " + description.Group);
                return false;
            }

            return true;
        }

        private bool TryResolve(List<LineToken> tokens, out OperationDescription description, out int consumed, out OperationResult failure) {
            description = null;
            consumed = 0;
            failure = null;
            var first = tokens[0].Text;
            var group = Catalog.Group(first);

            if (group != null) {
                if (tokens.Count < 2 || tokens[1].IsOption) {
                    failure = OperationResult.Invalid("'" + first + "' is a group; name an operation after it\nRun: core help --group " + group.Name);
                    return false;
                }

                description = group.Find(tokens[1].Text);
                consumed = 2;

                if (description != null) {
                    return true;
                }

                failure = OperationResult.Invalid(Unknown(first + " " + tokens[1].Text, group));
                return false;
            }

            var candidates = Catalog.FindByName(first);

            if (candidates.Count == 1) {
                description = candidates[0];
                consumed = 1;
                return true;
            }

            if (candidates.Count > 1) {
                var lines = new string[candidates.Count];

                for (var i = 0; i < lines.Length; i++) {
                    lines[i] = candidates[i].Line;
                }

                failure = OperationResult.Invalid("'" + first + "' matches " + string.Join(" and ", lines) + "; write the group");
                return false;
            }

            var typed = tokens.Count >= 2 && !tokens[1].IsOption ? first + " " + tokens[1].Text : first;
            failure = OperationResult.Invalid(Unknown(typed, null));
            return false;
        }

        private string Unknown(string typed, OperationGroupDescription group) {
            var suggestion = Suggest(typed, group);
            var text = "no operation '" + typed + "'.";

            if (suggestion != null) {
                text += " Did you mean '" + suggestion + "'?";
            }

            return text + "\nRun: core help" + (group != null ? " --group " + group.Name : string.Empty);
        }

        private string Suggest(string typed, OperationGroupDescription group) {
            string best = null;
            var bestDistance = int.MaxValue;
            var groups = group != null ? new List<OperationGroupDescription> { group } : new List<OperationGroupDescription>(Catalog.Groups);

            foreach (var candidateGroup in groups) {
                foreach (var operation in candidateGroup.Operations) {
                    var distance = Math.Min(EditDistance(typed, operation.Line), EditDistance(typed, operation.Name) + 1);

                    if (distance < bestDistance) {
                        bestDistance = distance;
                        best = operation.Line;
                    }
                }
            }

            return bestDistance <= Math.Max(2, typed.Length / 3) ? best : null;
        }

        private static int EditDistance(string left, string right) {
            left = left.ToLowerInvariant();
            right = right.ToLowerInvariant();
            var previous = new int[right.Length + 1];
            var current = new int[right.Length + 1];

            for (var j = 0; j <= right.Length; j++) {
                previous[j] = j;
            }

            for (var i = 1; i <= left.Length; i++) {
                current[0] = i;

                for (var j = 1; j <= right.Length; j++) {
                    var cost = left[i - 1] == right[j - 1] ? 0 : 1;
                    current[j] = Math.Min(Math.Min(current[j - 1] + 1, previous[j] + 1), previous[j - 1] + cost);
                }

                var swap = previous;
                previous = current;
                current = swap;
            }

            return previous[right.Length];
        }

        private void Execute(OperationRun run, OperationDescription description, object[] values, string line, OperationCaller caller,
            ObservationContext observations, bool confirmed, bool dryRun, CancellationToken cancellationToken) {
            var watch = Stopwatch.StartNew();
            var gate = Gate(description, caller, confirmed, dryRun);

            if (gate != null) {
                Finish(run, line, caller, observations, gate, description, watch);
                return;
            }

            var context = new OperationContext(this, run, caller, observations, dryRun, confirmed, cancellationToken);
            var arguments = new object[values.Length + (description.ContextParameterIndex >= 0 ? 1 : 0)];
            var valueIndex = 0;

            for (var i = 0; i < arguments.Length; i++) {
                arguments[i] = i == description.ContextParameterIndex ? context : values[valueIndex++];
            }

            object returned;

            try {
                returned = description.Method.Invoke(description.Instance, arguments);
            }
            catch (TargetInvocationException exception) {
                var inner = exception.InnerException ?? exception;
                Finish(run, line, caller, observations, FromException(inner, description), description, watch);
                return;
            }
            catch (Exception exception) {
                Finish(run, line, caller, observations, FromException(exception, description), description, watch);
                return;
            }

            if (returned is Task task) {
                AwaitTask(run, task, description, line, caller, observations, dryRun, watch);
                return;
            }

            Finish(run, line, caller, observations, Render(returned, description, dryRun), description, watch);
        }

        private void AwaitTask(OperationRun run, Task task, OperationDescription description, string line, OperationCaller caller,
            ObservationContext observations, bool dryRun, Stopwatch watch) {
            var scheduler = SynchronizationContext.Current != null
                ? TaskScheduler.FromCurrentSynchronizationContext()
                : TaskScheduler.Default;

            task.ContinueWith(completed => {
                if (completed.IsFaulted) {
                    var inner = completed.Exception?.InnerException ?? completed.Exception;
                    Finish(run, line, caller, observations, FromException(inner, description), description, watch);
                    return;
                }

                if (completed.IsCanceled) {
                    Finish(run, line, caller, observations, OperationResult.Failed(description.Line + " was cancelled"),
                        description, watch);
                    return;
                }

                object value = null;
                var resultProperty = completed.GetType().GetProperty("Result");

                if (resultProperty != null && completed.GetType().IsGenericType) {
                    value = resultProperty.GetValue(completed);

                    if (value != null && value.GetType().Name == "VoidTaskResult") {
                        value = null;
                    }
                }

                Finish(run, line, caller, observations, Render(value, description, dryRun), description, watch);
            }, CancellationToken.None, TaskContinuationOptions.None, scheduler);
        }

        private OperationResult Gate(OperationDescription description, OperationCaller caller, bool confirmed, bool dryRun) {
            if (description.Availability == OperationAvailability.WhilePlaying && !Application.isPlaying) {
                return OperationResult.Refused(description.Line + " is available only while playing\nRun editor_play first (Unity CLI)");
            }

            if (description.Availability == OperationAvailability.WhileEditing && Application.isPlaying) {
                return OperationResult.Refused(description.Line + " is available only while editing\nRun editor_stop first (Unity CLI)");
            }

            var audience = description.Audience;

            if (!string.Equals(audience, OperationAudiences.Authority, StringComparison.OrdinalIgnoreCase) && !caller.Holds(audience)) {
                var held = caller.Audiences.Count == 0 ? "nothing" : "'" + string.Join("', '", caller.Audiences) + "'";
                return OperationResult.Refused(description.Line + " needs audience '" + audience + "'; this caller holds " + held);
            }

            if (description.Effect == OperationEffect.Destroys && !confirmed && !dryRun) {
                return OperationResult.Refused("this operation deletes data\nAdd --dry-run to preview or --confirm to apply");
            }

            return null;
        }

        private OperationResult Render(object returned, OperationDescription description, bool dryRun) {
            OperationResult result;

            if (returned is OperationResult ready) {
                result = ready;
            }
            else {
                var text = Renderers.Render(returned);
                result = OperationResult.Success(Cut(text, description), returned);
            }

            return dryRun ? result.AsPreviewed() : result;
        }

        private string Cut(string text, OperationDescription description) {
            var maximum = Settings.maximumResultLines;

            if (maximum <= 0 || string.IsNullOrEmpty(text)) {
                return text;
            }

            var lines = text.Split('\n');

            if (lines.Length <= maximum) {
                return text;
            }

            var kept = string.Join("\n", lines, 0, maximum);
            var more = lines.Length - maximum;
            var hasPaging = HasArgument(description, "offset") && HasArgument(description, "limit");
            var hint = hasPaging
                ? "add --offset " + maximum + " --limit " + maximum
                : "the maximum is set in OperationSettings";
            return kept + "\n... " + more.ToString("N0") + " more; " + hint;
        }

        private static bool HasArgument(OperationDescription description, string name) {
            foreach (var argument in description.Arguments) {
                if (string.Equals(argument.Name, name, StringComparison.OrdinalIgnoreCase)) {
                    return true;
                }
            }

            return false;
        }

        private static OperationResult FromException(Exception exception, OperationDescription description) {
            if (exception is OperationException refusal) {
                var text = refusal.Message;

                if (!string.IsNullOrWhiteSpace(refusal.Hint)) {
                    text += "\n" + refusal.Hint;
                }

                return OperationResult.Failed(text);
            }

            Log.Error(description.Line + " threw " + exception.GetType().Name + ": " + exception.Message + "\n" + exception.StackTrace);
            return OperationResult.Failed(description.Line + " threw " + exception.GetType().Name + ": " + exception.Message);
        }

        private void Finish(OperationRun run, string line, OperationCaller caller, ObservationContext observations,
            OperationResult result,
            OperationDescription description, Stopwatch watch) {
            Record(line, caller, observations, result, description, watch);
            run.Complete(result);
        }

        private void Record(string line, OperationCaller caller, ObservationContext observations,
            OperationResult result,
            OperationDescription description, Stopwatch watch) {
            var settings = Settings;

            if (settings.maximumHistoryEntries > 0) {
                _history.Add(new OperationHistoryEntry(line, result.Status, caller.Name, FrameCount()));

                while (_history.Count > settings.maximumHistoryEntries) {
                    _history.RemoveAt(0);
                }
            }

            if (description != null && watch != null) {
                if (!_statistics.TryGetValue(description.Line, out var statistic)) {
                    statistic = new OperationStatistic(description.Line);
                    _statistics[description.Line] = statistic;
                }

                var elapsed = watch.Elapsed.TotalMilliseconds;
                statistic.Calls++;
                statistic.TotalMilliseconds += elapsed;
                statistic.MaximumMilliseconds = Math.Max(statistic.MaximumMilliseconds, elapsed);

                if (!result.Succeeded) {
                    statistic.Failures++;
                }

                GroupObservations(description.Group)?.Record(observations, description, elapsed, result.Succeeded);
            }

            if (settings.trace) {
                Log.Emit(LogLevel.Info, caller.Name + " ran '" + line + "': " + result.Text, TraceSource, nameof(Run), 0);
            }
        }

        private OperationGroupObservations GroupObservations(string group) {
            if (_groupObservations.TryGetValue(group, out var observations)) {
                return observations;
            }

            observations = OperationGroupObservations.Declare(group);
            _groupObservations[group] = observations;
            return observations;
        }

        private static int FrameCount() {
            try {
                return Time.frameCount;
            }
            catch (UnityException) {
                return -1;
            }
        }
    }
}
