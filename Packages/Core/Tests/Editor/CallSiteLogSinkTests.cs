// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using Spellbound.Core.Logging;
using Spellbound.Core.Observations;

namespace Spellbound.Core.Tests {
    public class CallSiteLogSinkTests {
        private readonly List<ReasonCount> _reasons = new List<ReasonCount>();
        private ObservationContext _context;
        private CallSiteLogSink _sink;
        private Log.SinkSuspension _suspension;

        [SetUp]
        public void CreateSink() {
            _suspension = Log.SuspendSinks();
            _context = CoreKit.Observations.CreateContext("logsink_" + Guid.NewGuid().ToString("N").Substring(0, 8));
            _sink = new CallSiteLogSink(_context);
        }

        [TearDown]
        public void DisposeContext() {
            _context.Dispose();
            _suspension.Dispose();
        }

        [Test]
        public void AContextIsRequired() {
            Assert.Throws<ArgumentNullException>(() => new CallSiteLogSink(null));
            Assert.AreSame(_context, _sink.Observations);
            Assert.IsNotEmpty(_sink.DisplayName);
        }

        [Test]
        public void TwoEmitsFromOneCallSiteCountTwiceUnderOneReason() {
            _sink.Emit(LogLevel.Error, "BaseFloater", "first message", "Rescue", 171, null);
            _sink.Emit(LogLevel.Error, "BaseFloater", "second message", "Rescue", 171, null);
            Assert.IsTrue(_context.TryRead(CoreObservations.LogError.Key, out var reading));
            Assert.AreEqual(2, reading.Value);
            Assert.AreEqual(1, _context.ReadReasons(CoreObservations.LogError.Key, _reasons));
            Assert.AreEqual("BaseFloater.Rescue:171", _reasons[0].Reason.Name);
            Assert.AreEqual(2, _reasons[0].Count);
        }

        [Test]
        public void TwoCallSitesWithOneMessageCountApart() {
            _sink.Emit(LogLevel.Warning, "Rider", "same message", "Probe", 10, null);
            _sink.Emit(LogLevel.Warning, "Rider", "same message", "Release", 42, null);
            Assert.AreEqual(2, _context.ReadReasons(CoreObservations.LogWarning.Key, _reasons));
            var names = _reasons.ConvertAll(entry => entry.Reason.Name);
            CollectionAssert.Contains(names, "Rider.Probe:10");
            CollectionAssert.Contains(names, "Rider.Release:42");
        }

        [Test]
        public void EachLevelReachesItsOwnKey() {
            _sink.Emit(LogLevel.Verbose, "A", "m", "M", 1, null);
            _sink.Emit(LogLevel.Debug, "A", "m", "M", 2, null);
            _sink.Emit(LogLevel.Info, "A", "m", "M", 3, null);
            _sink.Emit(LogLevel.Warning, "A", "m", "M", 4, null);
            _sink.Emit(LogLevel.Error, "A", "m", "M", 5, null);
            _sink.Emit(LogLevel.None, "A", "m", "M", 6, null);
            Assert.AreEqual(1, Read(CoreObservations.LogVerbose));
            Assert.AreEqual(1, Read(CoreObservations.LogDebug));
            Assert.AreEqual(1, Read(CoreObservations.LogInfo));
            Assert.AreEqual(1, Read(CoreObservations.LogWarning));
            Assert.AreEqual(1, Read(CoreObservations.LogError));
            Assert.AreEqual(0, _sink.DroppedOffThread);
        }

        [Test]
        public void ALineFromAnotherThreadIsDroppedAndCounted() {
            var worker = new Thread(() => _sink.Emit(LogLevel.Error, "Worker", "m", "Run", 9, null));
            worker.Start();
            worker.Join();
            Assert.AreEqual(1, _sink.DroppedOffThread);
            Assert.IsFalse(_context.TryRead(CoreObservations.LogError.Key, out _));
            _sink.Emit(LogLevel.Error, "Main", "m", "Run", 9, null);
            Assert.AreEqual(1, Read(CoreObservations.LogError));
        }

        [Test]
        public void TheSinkRegistersThroughTheLoggerLikeEverySink() {
            using (Log.AddScopedSink(_sink, null, LogLevel.Error)) {
                Log.Error("a line through the logger");
            }

            Assert.IsTrue(_context.TryRead(CoreObservations.LogError.Key, out var reading));
            Assert.AreEqual(1, reading.Value);
            Assert.AreEqual(1, _context.ReadReasons(CoreObservations.LogError.Key, _reasons));
            StringAssert.StartsWith("CallSiteLogSinkTests.TheSinkRegistersThroughTheLoggerLikeEverySink:",
                _reasons[0].Reason.Name);
        }

        [Test]
        public void TheDiscardContextRecordsNothing() {
            var sink = new CallSiteLogSink(ObservationContext.Discard);
            sink.Emit(LogLevel.Error, "A", "m", "M", 1, null);
            Assert.IsFalse(ObservationContext.Discard.TryRead(CoreObservations.LogError.Key, out _));
        }

        private double Read(Counter counter) {
            Assert.IsTrue(_context.TryRead(counter.Key, out var reading));
            return reading.Value;
        }
    }
}
