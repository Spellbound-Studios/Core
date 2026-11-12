// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Collections.Generic;
using NUnit.Framework;
using Spellbound.Core.Logging;
using Spellbound.Core.Observations;
using Spellbound.Core.Operations;
using UnityEngine;

namespace Spellbound.Core.Tests {
    public class ObservationOperationsTests {
        private static readonly Counter Rescues = CoreKit.Observations.Counter("optests.containment.rescues",
            "A body was moved out of terrain. The reason says why.");

        private static readonly Gauge Visible = CoreKit.Observations.Gauge("optests.draw.visible",
            "Instances visible after culling.", ObservationUnit.Count);

        private readonly List<ObservationContext> _contexts = new List<ObservationContext>();
        private OperationHost _host;
        private OperationCaller _anyone;
        private Log.SinkSuspension _suspension;

        [SetUp]
        public void SetUp() {
            _suspension = Log.SuspendSinks();
            _host = new OperationHost(new OperationCatalog(() => new[] { typeof(OperationHost).Assembly }),
                () => ScriptableObject.CreateInstance<OperationSettings>());
            _anyone = new OperationCaller("reader");
            DisposeEveryLiveContext();
        }

        [TearDown]
        public void TearDown() {
            foreach (var context in _contexts) {
                context.Dispose();
            }

            _contexts.Clear();
            _suspension.Dispose();
        }

        private string Text(string line) => _host.Run(line, _anyone, ObservationContext.Discard).Result.Text;

        private ObservationContext World(string name) {
            var context = CoreKit.Observations.CreateContext(name);
            _contexts.Add(context);
            return context;
        }

        [Test]
        public void WithNoLiveWorldTheDeclarationsAloneAreListed() {
            var text = Text("core observations --prefix optests.");
            StringAssert.StartsWith("succeeded: 2 keys, 0 with readings; no world is live", text);
            StringAssert.Contains("optests.containment.rescues", text);
            StringAssert.Contains("optests.draw.visible", text);
        }

        [Test]
        public void OneLiveWorldIsReadWithoutNamingIt() {
            var world = World("only");
            Rescues.Add(world, CoreKit.Observations.Reason("embedded"));
            Rescues.Add(world);
            var text = Text("core observations --prefix optests.");
            StringAssert.StartsWith("succeeded: 2 keys, 1 with readings in world 'only'", text);
            StringAssert.Contains("optests.containment.rescues", text);
            StringAssert.Contains(" 2 ", text);
        }

        [Test]
        public void SeveralLiveWorldsNeedAName() {
            World("first");
            World("second");
            var text = Text("core observations");
            StringAssert.StartsWith("invalid: several worlds are live; name one with --world", text);
            StringAssert.Contains("first, second", text);
            Visible.Set(_contexts[1], 7);
            var named = Text("core observations --world second --prefix optests.draw");
            StringAssert.StartsWith("succeeded: 1 key, 1 with readings in world 'second'", named);
            StringAssert.Contains(" 7 ", named);
        }

        [Test]
        public void AnUnknownWorldFails() {
            World("first");
            var text = Text("core observations --world nowhere");
            StringAssert.StartsWith("invalid: no world named 'nowhere' is live", text);
            StringAssert.Contains("Live worlds: first", text);
        }

        [Test]
        public void ThePrefixFiltersTheKeys() {
            var text = Text("core observations --prefix optests.draw");
            StringAssert.StartsWith("succeeded: 1 key, 0 with readings", text);
            StringAssert.DoesNotContain("optests.containment.rescues", text);
            Assert.AreEqual(
                "succeeded: 0 keys, 0 with readings; no world is live, so the declarations alone are listed",
                Text("core observations --prefix nothing.here"));
        }

        [Test]
        public void OneKeyPrintsItsDeclarationReadingAndReasons() {
            var world = World("only");
            Rescues.Add(world, CoreKit.Observations.Reason("embedded"));
            Rescues.Add(world, CoreKit.Observations.Reason("embedded"));
            Rescues.Add(world, CoreKit.Observations.Reason("below_floor"));
            var text = Text("core observation --key optests.containment.rescues");
            StringAssert.StartsWith("succeeded: optests.containment.rescues: counter in count, default quality level 0",
                text);
            StringAssert.Contains("A body was moved out of terrain. The reason says why.", text);
            StringAssert.Contains("world 'only': value 3, count 3", text);
            StringAssert.Contains("embedded", text);
            StringAssert.Contains("below_floor", text);
            var embeddedIndex = text.IndexOf("embedded", StringComparison.Ordinal);
            var belowIndex = text.IndexOf("below_floor", StringComparison.Ordinal);
            Assert.Less(embeddedIndex, belowIndex);
        }

        [Test]
        public void OneKeyWithoutAReadingSaysSo() {
            var world = World("only");
            var text = Text("core observation --key optests.draw.visible");
            StringAssert.Contains("no reading in world 'only'", text);
            world.Dispose();
            Assert.AreEqual(
                "succeeded: optests.draw.visible: gauge in count, default quality level 0"
                + "\nInstances visible after culling.\nno world is live",
                Text("core observation --key optests.draw.visible").Replace("\r\n", "\n"));
        }

        [Test]
        public void AnUnknownOrMalformedKeyFails() {
            StringAssert.StartsWith("invalid: no observation key 'optests.nothing.here' is declared",
                Text("core observation --key optests.nothing.here"));
            StringAssert.StartsWith("invalid: The observation key 'NoDots' needs at least two segments",
                Text("core observation --key NoDots"));
        }

        [Test]
        public void BothOperationsAreOpenToAnyone() {
            var description = _host.Catalog.Describe("core", "observations");
            Assert.IsNotNull(description);
            Assert.AreEqual(OperationAudiences.Anyone, description.Audience);
            Assert.AreEqual(OperationEffect.Reads, description.Effect);
            var single = _host.Catalog.Describe("core", "observation");
            Assert.IsNotNull(single);
            Assert.AreEqual(OperationAudiences.Anyone, single.Audience);
        }

        private static void DisposeEveryLiveContext() {
            var live = new List<ObservationContext>(ObservationContexts.All);

            foreach (var context in live) {
                context.Dispose();
            }
        }
    }
}
