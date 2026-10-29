// Copyright (c) Spellbound Studios. All rights reserved.

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Spellbound.Core.Logging;
using Spellbound.Core.Operations;
using Spellbound.Core.Tests.OperationFixture;
using UnityEngine;
using UnityEngine.TestTools;

namespace Spellbound.Core.Tests {
    public class OperationHostTests {
        private static readonly Assembly Fixture = typeof(FixtureOperations).Assembly;
        private static readonly Assembly Core = typeof(OperationHost).Assembly;

        private OperationHost _host;
        private OperationCaller _developer;
        private RecordingLogSink _recording;
        private Log.SinkSuspension _suspension;

        [SetUp]
        public void SetUp() {
            _suspension = Log.SuspendSinks();
            _recording = new RecordingLogSink(LogLevel.Verbose);
            Log.AddSink(_recording, null, LogLevel.Verbose);
            _host = new OperationHost(new OperationCatalog(() => new[] { Fixture, Core }), () => ScriptableObject.CreateInstance<OperationSettings>());
            _developer = OperationCaller.Development();
            FixtureOperations.Reset();
        }

        [TearDown]
        public void TearDown() {
            Log.RemoveSink(_recording);
            _suspension.Dispose();
        }

        private string Text(string line) => _host.Run(line, _developer).Result.Text;

        [Test]
        public void PositionalAndNamedArgumentsAreTheSameCall() {
            Assert.AreEqual("succeeded: 5", Text("fixture add 2 3"));
            Assert.AreEqual("succeeded: 5", Text("fixture add --left 2 --right 3"));
            Assert.AreEqual("succeeded: 5", Text("fixture add --right=3 --left=2"));
            Assert.AreEqual("succeeded: 5", Text("fixture add --right 3 2"));
        }

        [Test]
        public void DefaultsFillMissingArguments() {
            Assert.AreEqual("succeeded: 3", Text("fixture add 2"));
        }

        [Test]
        public void QuotedTextStaysOneValue() {
            Assert.AreEqual("succeeded: hello big world", Text("fixture echo \"hello big world\""));
            Assert.AreEqual("succeeded: say \"hi\"", Text("fixture echo \"say \\\"hi\\\"\""));
            Assert.AreEqual("succeeded: --not-a-flag", Text("fixture echo \"--not-a-flag\""));
        }

        [Test]
        public void NegativeNumbersAreValuesNotOptions() {
            Assert.AreEqual("succeeded: -4", Text("fixture add -5 1"));
            Assert.AreEqual("succeeded: -2/1.5/0", Text("fixture move -2,1.5,0"));
        }

        [Test]
        public void ShortNameWorksWhenUnique() {
            Assert.AreEqual("succeeded: 3", Text("add 1 2"));
        }

        [Test]
        public void UnknownOperationSuggestsTheNearest() {
            var text = Text("fixture ad 1 2");
            StringAssert.StartsWith("invalid: no operation 'fixture ad'. Did you mean 'fixture add'?", text);
            StringAssert.Contains("Run: core help --group fixture", text);
        }

        [Test]
        public void UnknownGroupIsInvalid() {
            StringAssert.StartsWith("invalid: no operation 'nowhere echo'.", Text("nowhere echo hi"));
        }

        [Test]
        public void GroupAloneAsksForAnOperation() {
            StringAssert.StartsWith("invalid: 'fixture' is a group; name an operation after it", Text("fixture"));
        }

        [Test]
        public void EmptyLineIsInvalid() {
            StringAssert.StartsWith("invalid: the line is empty", Text("   "));
        }

        [Test]
        public void BadIntegerNamesTheArgumentAndTheExpectation() {
            Assert.AreEqual("invalid: --right: '3x' is not an integer (expected: integer, default 1)\nRun: core help --group fixture",
                Text("fixture add 1 3x"));
        }

        [Test]
        public void BadEnumListsTheLegalValues() {
            Assert.AreEqual("invalid: --tier: 'Legendary' is not one of Common, Rare, Epic\nRun: core help --group fixture",
                Text("fixture tier Legendary"));
            Assert.AreEqual("succeeded: Epic", Text("fixture tier epic"));
        }

        [Test]
        public void MissingRequiredArgumentIsInvalid() {
            StringAssert.StartsWith("invalid: --left is required (expected: integer)", Text("fixture add"));
        }

        [Test]
        public void UnknownArgumentListsTheArguments() {
            StringAssert.StartsWith("invalid: no argument '--middle' on fixture add; fixture add takes --left, --right", Text("fixture add --middle 1"));
        }

        [Test]
        public void ExtraPositionalValueIsInvalid() {
            StringAssert.StartsWith("invalid: unexpected value '3'; fixture add takes --left, --right", Text("fixture add 1 2 3"));
        }

        [Test]
        public void ListsVectorsColoursAndFlagsParse() {
            Assert.AreEqual("succeeded: 6", Text("fixture sum 1,2,3"));
            Assert.AreEqual("succeeded: 1/2/3", Text("fixture move 1,2,3"));
            Assert.AreEqual("succeeded: 1/2/3", Text("fixture move \"1 2 3\""));
            Assert.AreEqual("succeeded: FF0000FF", Text("fixture paint #ff0000"));
            Assert.AreEqual("succeeded: FF0000FF", Text("fixture paint red"));
            Assert.AreEqual("succeeded: 0000FF80", Text("fixture paint 0,0,1,0.5"));
            Assert.AreEqual("succeeded: True:0", Text("fixture flag --on"));
            Assert.AreEqual("succeeded: True:7", Text("fixture flag --on --count 7"));
            Assert.AreEqual("succeeded: False:7", Text("fixture flag --on false --count 7"));
            Assert.AreEqual("succeeded: True:9", Text("fixture flag true 9"));
        }

        [Test]
        public void CamelCaseParametersAreNamedInKebabCase() {
            Assert.AreEqual("succeeded: 12", Text("fixture camel-case-name --max-hit-points 12"));
        }

        [Test]
        public void DestroyingOperationIsRefusedWithoutConfirm() {
            Assert.AreEqual("refused: this operation deletes data\nAdd --dry-run to preview or --confirm to apply", Text("fixture wipe"));
            Assert.AreEqual(0, FixtureOperations.WipeCount);
            Assert.AreEqual("previewed: would wipe 3 things", Text("fixture wipe --dry-run"));
            Assert.AreEqual(0, FixtureOperations.WipeCount);
            Assert.AreEqual("succeeded: wiped 3 things", Text("fixture wipe --confirm"));
            Assert.AreEqual(1, FixtureOperations.WipeCount);
        }

        [Test]
        public void AudienceIsCheckedAgainstTheCaller() {
            Assert.AreEqual("refused: fixture secret needs audience 'approver'; this caller holds 'developer'", Text("fixture secret"));
            var approver = new OperationCaller("window", OperationAudiences.Developer, OperationAudiences.Approver);
            Assert.AreEqual("succeeded: approved", _host.Run("fixture secret", approver).Result.Text);
            var nobody = new OperationCaller("nobody");
            Assert.AreEqual("succeeded: hi", _host.Run("fixture echo hi", nobody).Result.Text);
            Assert.AreEqual("refused: fixture add needs audience 'developer'; this caller holds nothing", _host.Run("fixture add 1", nobody).Result.Text);
        }

        [Test]
        public void AvailabilityIsCheckedInEditMode() {
            Assert.AreEqual("refused: fixture playing-only is available only while playing\nRun editor_play first (Unity CLI)",
                Text("fixture playing-only"));
            Assert.AreEqual("succeeded: editing", Text("fixture editing-only"));
        }

        [Test]
        public void OperationExceptionBecomesFailedWithItsHint() {
            Assert.AreEqual("failed: No match score exists.\nStart a match first.", Text("fixture boom"));
            Assert.AreEqual(0, _recording.Count);
        }

        [Test]
        public void OtherExceptionsBecomeFailedAndAreLogged() {
            Assert.AreEqual("failed: fixture crash threw InvalidOperationException: broken", Text("fixture crash"));
            Assert.AreEqual(1, _recording.Count);
            Assert.AreEqual(LogLevel.Error, _recording.Entries[0].Level);
        }

        [Test]
        public void ReadyResultsPassThrough() {
            Assert.AreEqual("refused: not today", Text("fixture verdict"));
        }

        [Test]
        public void NullReturnIsSucceededAlone() {
            Assert.AreEqual("succeeded", Text("fixture nothing"));
        }

        [Test]
        public void TablesRenderAligned() {
            Assert.AreEqual("succeeded: name    count\napples  3\npears   12", Text("fixture rows"));
        }

        [Test]
        public void CustomRendererDrawsItsType() {
            Assert.AreEqual("succeeded: badge of gold", Text("fixture badge"));
        }

        [Test]
        public void ArgumentParser_CustomImplementation() {
            Assert.AreEqual("succeeded: 4,7", Text("fixture coordinate 4:7"));
            StringAssert.StartsWith("invalid: --at: 'nope' is not a column:row (expected: column:row)", Text("fixture coordinate nope"));
        }

        [Test]
        public void ResultRenderer_CustomImplementation() {
            _host.Renderers.Register(new UpperCaseRenderer());
            Assert.AreEqual("succeeded: HELLO", Text("fixture echo hello"));
        }

        [Test]
        public void ArgumentParserRegisteredInCodeWinsOverTheBuiltIn() {
            _host.Parsers.Register(new DoublingIntegerParser());
            Assert.AreEqual("succeeded: 6", Text("fixture add 1 2"));
        }

        [Test]
        public void LongResultsAreCutWithTheRightHint() {
            _host.Settings.maximumResultLines = 3;
            Assert.AreEqual("succeeded: line 0\nline 1\nline 2\n... 7 more; the maximum is set in OperationSettings", Text("fixture many 10"));
            Assert.AreEqual("succeeded: line 0\nline 1\nline 2\n... 7 more; add --offset 3 --limit 3", Text("fixture list 10"));
        }

        [Test]
        public void LongLinesAreInvalid() {
            _host.Settings.maximumLineLength = 20;
            StringAssert.StartsWith("invalid: the line is 25 characters; the maximum is set in OperationSettings (20)", Text("fixture echo aaaaaaaaaaaa"));
        }

        [Test]
        public void DisabledGroupIsRefused() {
            _host.Settings.disabledGroups = new[] { "fixture" };
            Assert.AreEqual("refused: the group 'fixture' is disabled in OperationSettings", Text("fixture echo hi"));
        }

        [Test]
        public void ProgressLinesReachTheRun() {
            var run = _host.Run("fixture progress", _developer);
            CollectionAssert.AreEqual(new[] { "step 1", "step 2" }, run.Lines);
            Assert.AreEqual("succeeded: done", run.Result.Text);
        }

        [Test]
        public void ProgressLinesAreRaisedAsTheyArrive() {
            var seen = new List<string>();
            var run = _host.Run("fixture echo quiet", _developer);
            run.Reported += seen.Add;
            Assert.AreEqual(0, seen.Count);
            var reporting = _host.Run("fixture progress", _developer);
            Assert.AreEqual(2, reporting.Lines.Count);
        }

        [Test]
        public void PlaceholdersAreFilledAfterTokenising() {
            var placeholders = new Dictionary<string, string> { { "text", "two words --not-a-flag" } };
            Assert.AreEqual("succeeded: two words --not-a-flag", _host.Run("fixture echo {text}", _developer, placeholders).Result.Text);
            Assert.AreEqual("succeeded: pre-two words --not-a-flag", _host.Run("fixture echo pre-{text}", _developer, placeholders).Result.Text);
            Assert.AreEqual("invalid: placeholder {missing} has no value (supplied: text)",
                _host.Run("fixture echo {missing}", _developer, placeholders).Result.Text);
            Assert.AreEqual("invalid: placeholder {text} has no value (supplied: none)", Text("fixture echo {text}"));
        }

        [Test]
        public void RunAllStopsAtTheFirstResultThatIsNotSucceeded() {
            var chain = _host.RunAll(new[] { "fixture echo one", "fixture add 1 x", "fixture echo three" }, _developer);
            Assert.IsTrue(chain.IsDone);
            Assert.AreEqual(2, chain.Results.Count);
            Assert.AreEqual("succeeded: one", chain.Results[0].Text);
            Assert.AreEqual(OperationStatus.Invalid, chain.Result.Status);
            var whole = _host.RunAll(new[] { "fixture echo one", "fixture echo two" }, _developer);
            Assert.AreEqual(2, whole.Results.Count);
            Assert.AreEqual("succeeded: two", whole.Result.Text);
            Assert.IsTrue(whole.Result.Succeeded);
        }

        [Test]
        public void RunAllCarriesProgressLines() {
            var chain = _host.RunAll(new[] { "fixture progress", "fixture echo end" }, _developer);
            CollectionAssert.AreEqual(new[] { "step 1", "step 2" }, chain.Lines);
        }

        [Test]
        public void InvocationRunsWithTypedValues() {
            var description = _host.Catalog.Describe("fixture", "add");
            var invocation = new OperationInvocation(description.Identifier, new object[] { 2, 5 });
            Assert.AreEqual("succeeded: 7", _host.Run(invocation, _developer).Result.Text);
            var wrong = new OperationInvocation(description.Identifier, new object[] { "two", 5 });
            Assert.AreEqual("invalid: --left: a String is not a Int32", _host.Run(wrong, _developer).Result.Text);
            var unknown = new OperationInvocation(12345u, new object[0]);
            Assert.AreEqual("invalid: no operation has the identifier 12345", _host.Run(unknown, _developer).Result.Text);
            var destroy = new OperationInvocation(_host.Catalog.Describe("fixture", "wipe").Identifier, new object[0], null, true);
            Assert.AreEqual("succeeded: wiped 3 things", _host.Run(destroy, _developer).Result.Text);
        }

        [Test]
        public void ParseProducesAnInvocationTheHostRuns() {
            var invocation = _host.Parse("fixture add 2 --right 9", null, out var failure);
            Assert.IsNull(failure);
            Assert.AreEqual(_host.Catalog.Describe("fixture", "add").Identifier, invocation.Identifier);
            CollectionAssert.AreEqual(new object[] { 2, 9 }, invocation.Values);
            Assert.AreEqual("succeeded: 11", _host.Run(invocation, _developer).Result.Text);
            Assert.IsNull(_host.Parse("fixture add", null, out failure));
            Assert.AreEqual(OperationStatus.Invalid, failure.Status);
        }

        [Test]
        public void HistoryRecordsEveryLineWithStatusAndCaller() {
            Text("fixture echo one");
            Text("fixture add x");
            Assert.AreEqual(2, _host.History.Count);
            Assert.AreEqual("fixture echo one", _host.History[0].Line);
            Assert.AreEqual(OperationStatus.Succeeded, _host.History[0].Status);
            Assert.AreEqual("development", _host.History[0].Caller);
            Assert.AreEqual(OperationStatus.Invalid, _host.History[1].Status);
            _host.Settings.maximumHistoryEntries = 1;
            Text("fixture echo three");
            Assert.AreEqual(1, _host.History.Count);
            Assert.AreEqual("fixture echo three", _host.History[0].Line);
        }

        [Test]
        public void StatisticsCountCallsAndFailuresPerOperation() {
            Text("fixture echo one");
            Text("fixture echo two");
            Text("fixture boom");
            Text("nowhere nothing");
            var statistics = _host.Statistics;
            Assert.AreEqual(2, statistics.Count);
            var boom = statistics.First(statistic => statistic.Line == "fixture boom");
            Assert.AreEqual(1, boom.Calls);
            Assert.AreEqual(1, boom.Failures);
            var echo = statistics.First(statistic => statistic.Line == "fixture echo");
            Assert.AreEqual(2, echo.Calls);
            Assert.AreEqual(0, echo.Failures);
            Assert.GreaterOrEqual(echo.TotalMilliseconds, echo.MaximumMilliseconds);
        }

        [Test]
        public void TraceLogsEveryLineThroughTheLogger() {
            _host.Settings.trace = true;
            Text("fixture echo traced");
            Assert.AreEqual(1, _recording.Count);
            Assert.AreEqual(LogLevel.Info, _recording.Entries[0].Level);
            Assert.AreEqual("development ran 'fixture echo traced': succeeded: traced", _recording.Entries[0].Message);
            Assert.AreEqual("OperationHost", _recording.Entries[0].Source);
        }

        [Test]
        public void ResultSeverityFollowsTheStatus() {
            Assert.AreEqual(OperationSeverity.Information, _host.Run("fixture echo hi", _developer).Result.Severity);
            Assert.AreEqual(OperationSeverity.Error, _host.Run("fixture add x", _developer).Result.Severity);
            Assert.AreEqual(OperationSeverity.Warning, _host.Run("fixture secret", _developer).Result.Severity);
            Assert.AreEqual(OperationSeverity.Information, _host.Run("fixture wipe --dry-run", _developer).Result.Severity);
        }

        [UnityTest]
        public IEnumerator TaskReturningOperationCompletesLater() {
            var run = _host.Run("fixture wait --milliseconds 50", _developer);
            Assert.IsFalse(run.IsDone);
            Assert.IsTrue(run.keepWaiting);
            var task = run.AsTask();

            while (!run.IsDone) {
                yield return null;
            }

            Assert.IsTrue(task.IsCompleted);
            Assert.AreEqual("succeeded: waited 50", run.Result.Text);
            Assert.AreEqual("succeeded: waited 50", task.Result.Text);
            Assert.AreEqual(OperationStatus.Succeeded, _host.History[0].Status);
        }

        [Test]
        public void CallCostIsMeasured() {
            Text("fixture add 1 2");
            var watch = System.Diagnostics.Stopwatch.StartNew();
            const int Calls = 1000;

            for (var i = 0; i < Calls; i++) {
                _host.Run("fixture add 1 2", _developer);
            }

            var perCall = watch.Elapsed.TotalMilliseconds / Calls;
            Log.Emit(LogLevel.Info, "OperationHost call cost: " + perCall.ToString("F4") + " ms per 'fixture add 1 2' over " + Calls + " calls");
            Assert.Less(perCall, 5.0);
        }

        private sealed class UpperCaseRenderer : IResultRenderer {
            public bool TryRender(object value, out string text) {
                if (value is string content) {
                    text = content.ToUpperInvariant();
                    return true;
                }

                text = null;
                return false;
            }
        }

        private sealed class DoublingIntegerParser : IArgumentParser<int> {
            public string Expected => "integer";

            public bool TryParse(string text, out int value) {
                if (int.TryParse(text, out var parsed)) {
                    value = parsed * 2;
                    return true;
                }

                value = 0;
                return false;
            }
        }
    }
}
