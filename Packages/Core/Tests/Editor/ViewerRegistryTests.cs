// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Collections.Generic;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using Spellbound.Core.Viewers;
using UnityEngine;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace Spellbound.Core.Tests {
    public class ViewerRegistryTests {
        private static readonly Vector3 Origin = new Vector3(1f, 2f, 3f);
        private static readonly Vector3 Destination = new Vector3(-4f, 5f, 6f);

        [Test]
        public void Add_IssuesIdsFromOneAndNeverReusesThem() {
            var registry = new ViewerRegistry();
            var first = registry.Add(Origin, 50f, 200f);
            var second = registry.Add(Origin, 50f, 200f);
            Assert.IsTrue(registry.Remove(first));
            var third = registry.Add(Origin, 50f, 200f);

            Assert.AreEqual(1u, first.Value);
            Assert.AreEqual(2u, second.Value);
            Assert.AreEqual(3u, third.Value);
            Assert.AreEqual(2, registry.Count);
        }

        [Test]
        public void Add_RefusesANegativeOrUndefinedRadius() {
            var registry = new ViewerRegistry();
            Assert.Throws<ArgumentOutOfRangeException>(() => registry.Add(Origin, -1f, 10f));
            Assert.Throws<ArgumentOutOfRangeException>(() => registry.Add(Origin, 10f, float.NaN));
            Assert.Throws<ArgumentOutOfRangeException>(() => registry.Add(Origin, float.PositiveInfinity, 10f));
            Assert.AreEqual(0, registry.Count);
        }

        [Test]
        public void Add_RefusesAPositionThatIsNotFinite() {
            var registry = new ViewerRegistry();
            Assert.Throws<ArgumentOutOfRangeException>(() => registry.Add(new Vector3(float.NaN, 0f, 0f), 1f, 1f));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => registry.Add(new Vector3(0f, float.PositiveInfinity, 0f), 1f, 1f));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => registry.Add(new Vector3(0f, 0f, float.NegativeInfinity), 1f, 1f));
            Assert.AreEqual(0, registry.Count);
        }

        [Test]
        public void Move_RefusesAPositionThatIsNotFinite() {
            var registry = new ViewerRegistry();
            var id = registry.Add(Origin, 50f, 200f);
            Assert.Throws<ArgumentOutOfRangeException>(() => registry.Move(id, new Vector3(0f, float.NaN, 0f)));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => registry.Move(id, new Vector3(float.NegativeInfinity, 0f, 0f)));
            Assert.AreEqual(Origin, registry.Viewers[0].Position);
        }

        [Test]
        public void Update_ChangesThePositionAndRadiiAndRaisesMovedOnce() {
            var registry = new ViewerRegistry();
            var moved = new List<Viewer>();
            var id = registry.Add(Origin, 50f, 200f);
            registry.Moved += (in Viewer viewer) => moved.Add(viewer);

            Assert.IsTrue(registry.Update(id, Destination, 10f, 20f));

            Assert.AreEqual(1, moved.Count);
            AssertViewer(moved[0], id, Destination, 10f, 20f);
            AssertViewer(registry.Viewers[0], id, Destination, 10f, 20f);
        }

        [Test]
        public void Update_AnswersFalseForAnUnknownIdAndRefusesWhatAddRefuses() {
            var registry = new ViewerRegistry();
            var id = registry.Add(Origin, 50f, 200f);
            var moved = 0;
            registry.Moved += (in Viewer viewer) => moved++;

            Assert.IsFalse(registry.Update(new ViewerId(99), Destination, 1f, 1f));
            Assert.IsFalse(registry.Update(ViewerId.None, Destination, 1f, 1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => registry.Update(id, Destination, -1f, 1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => registry.Update(id, Destination, 1f, float.NaN));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => registry.Update(id, new Vector3(float.NaN, 0f, 0f), 1f, 1f));
            Assert.AreEqual(0, moved);
            AssertViewer(registry.Viewers[0], id, Origin, 50f, 200f);
        }

        [Test]
        public void ViewerId_NoneIsDefaultAndEqualityFollowsTheValue() {
            Assert.AreEqual(default(ViewerId), ViewerId.None);
            Assert.IsTrue(ViewerId.None.IsNone);
            Assert.IsTrue(new ViewerId(7) == new ViewerId(7));
            Assert.IsTrue(new ViewerId(7) != new ViewerId(8));
            Assert.IsFalse(new ViewerId(7).IsNone);
        }

        [Test]
        public void Move_AnswersFalseForAnUnknownId() {
            var registry = new ViewerRegistry();
            registry.Add(Origin, 50f, 200f);
            Assert.IsFalse(registry.Move(new ViewerId(99), Destination));
            Assert.IsFalse(registry.Move(ViewerId.None, Destination));
            Assert.AreEqual(Origin, registry.Viewers[0].Position);
        }

        [Test]
        public void Remove_AnswersFalseTheSecondTime() {
            var registry = new ViewerRegistry();
            var id = registry.Add(Origin, 50f, 200f);
            Assert.IsTrue(registry.Remove(id));
            Assert.IsFalse(registry.Remove(id));
            Assert.IsFalse(registry.Remove(ViewerId.None));
            Assert.AreEqual(0, registry.Count);
        }

        [Test]
        public void Events_FireOnceEachWithTheViewersValues() {
            var registry = new ViewerRegistry();
            var added = new List<Viewer>();
            var moved = new List<Viewer>();
            var removed = new List<Viewer>();
            registry.Added += (in Viewer viewer) => added.Add(viewer);
            registry.Moved += (in Viewer viewer) => moved.Add(viewer);
            registry.Removed += (in Viewer viewer) => removed.Add(viewer);

            var id = registry.Add(Origin, 50f, 200f);
            Assert.IsTrue(registry.Move(id, Destination));
            Assert.IsTrue(registry.Remove(id));

            Assert.AreEqual(1, added.Count);
            AssertViewer(added[0], id, Origin, 50f, 200f);
            Assert.AreEqual(1, moved.Count);
            AssertViewer(moved[0], id, Destination, 50f, 200f);
            Assert.AreEqual(1, removed.Count);
            AssertViewer(removed[0], id, Destination, 50f, 200f);
        }

        [Test]
        public void Read_FillsTheCallersList() {
            var registry = new ViewerRegistry();
            var first = registry.Add(Origin, 50f, 200f);
            var second = registry.Add(Destination, 10f, 20f);
            var into = new List<Viewer> { default, default, default };

            registry.Read(into);

            Assert.AreEqual(2, into.Count);
            AssertViewer(into[0], first, Origin, 50f, 200f);
            AssertViewer(into[1], second, Destination, 10f, 20f);
            Assert.Throws<ArgumentNullException>(() => registry.Read(null));
        }

        [Test]
        public void Viewers_IsASpanOfCountEntries() {
            var registry = new ViewerRegistry(1);
            var ids = new List<ViewerId>();

            for (var i = 0; i < 6; i++) {
                ids.Add(registry.Add(new Vector3(i, 0f, 0f), i, i * 2f));
            }

            registry.Remove(ids[2]);
            var viewers = registry.Viewers;

            Assert.AreEqual(registry.Count, viewers.Length);
            Assert.AreEqual(5, viewers.Length);
            var seen = new HashSet<ViewerId>();

            foreach (var viewer in viewers) {
                Assert.IsTrue(seen.Add(viewer.Id));
                Assert.AreEqual(viewer.Id.Value - 1f, viewer.Position.x);
            }

            Assert.IsFalse(seen.Contains(ids[2]));
        }

        [Test]
        public void Viewers_NoAllocation() {
            var registry = new ViewerRegistry();
            var id = registry.Add(Origin, 50f, 200f);
            registry.Add(Destination, 10f, 20f);
            var into = new List<Viewer> { default, default, default, default };
            var movedCount = 0;
            ViewerHandler onMoved = (in Viewer viewer) => movedCount++;
            registry.Moved += onMoved;
            var total = 0f;
            TestDelegate action = () => {
                registry.Move(id, Destination);
                registry.Update(id, Origin, 40f, 100f);
                registry.Read(into);
                var viewers = registry.Viewers;

                for (var i = 0; i < viewers.Length; i++) {
                    total += viewers[i].CollisionRadius;
                }
            };

            action();
            var constraint = ((IResolveConstraint)Is.Not.AllocatingGCMemory()).Resolve();
            constraint.ApplyTo(action);
            Assert.That(action, constraint);
            Assert.Greater(total, 0f);
            Assert.Greater(movedCount, 0);
            Assert.AreEqual(2, into.Count);
        }

        [Test]
        public void TwoRegistriesShareNothing() {
            var first = new ViewerRegistry();
            var second = new ViewerRegistry();
            var secondEvents = 0;
            second.Added += (in Viewer viewer) => secondEvents++;
            second.Moved += (in Viewer viewer) => secondEvents++;
            second.Removed += (in Viewer viewer) => secondEvents++;

            var inFirst = first.Add(Origin, 50f, 200f);
            first.Move(inFirst, Destination);
            var inSecond = second.Add(Destination, 5f, 6f);

            Assert.AreEqual(1u, inFirst.Value);
            Assert.AreEqual(1u, inSecond.Value);
            Assert.AreEqual(1, secondEvents);
            Assert.AreEqual(1, first.Count);
            Assert.AreEqual(1, second.Count);
            AssertViewer(second.Viewers[0], inSecond, Destination, 5f, 6f);

            Assert.IsTrue(first.Remove(inFirst));
            Assert.AreEqual(0, first.Count);
            Assert.AreEqual(1, second.Count);
            Assert.AreEqual(1, secondEvents);
        }

        [Test]
        public void ACrossingIsRemovedFromOneRegistryThenAddedToTheOther() {
            var leaving = new ViewerRegistry();
            var entering = new ViewerRegistry();
            var order = new List<string>();
            leaving.Removed += (in Viewer viewer) => order.Add("removed " + viewer.Position);
            entering.Added += (in Viewer viewer) => order.Add("added " + viewer.Position);
            var id = leaving.Add(Origin, 50f, 200f);
            var crossing = leaving.Viewers[0];

            Assert.IsTrue(leaving.Remove(id));
            entering.Add(crossing.Position, crossing.CollisionRadius, crossing.DetailRadius);

            CollectionAssert.AreEqual(new[] { "removed " + Origin, "added " + Origin }, order);
            Assert.AreEqual(0, leaving.Count);
            Assert.AreEqual(1, entering.Count);
        }

        private static void AssertViewer(in Viewer viewer, ViewerId id, Vector3 position, float collisionRadius,
            float detailRadius) {
            Assert.AreEqual(id, viewer.Id);
            Assert.AreEqual(position, viewer.Position);
            Assert.AreEqual(collisionRadius, viewer.CollisionRadius);
            Assert.AreEqual(detailRadius, viewer.DetailRadius);
        }
    }
}
