// Copyright (c) Spellbound Studios. All rights reserved.

using System.Collections.Generic;
using NUnit.Framework;
using Spellbound.Core.Ledger;

namespace Spellbound.Core.Tests {
    public class LedgerTableTests {
        private const string Header = "| Id | Ours | Ours (type) | Unity item | Verdict | Judged at | Reason "
                                      + "| Evidence | Watch packages | Watch words |";

        private const string Separator = "|---|---|---|---|---|---|---|---|---|---|";

        private const string MovedRow = "| U-004 | Core ObjectPool | `Spellbound.Core.ObjectPooling.ObjectPool`1` "
                                        + "| UnityEngine.Pool.ObjectPool | Moved | 6000.4 | Same job with delegates "
                                        + "| docs 6000.4 Pool.ObjectPool_1 | | |";

        private const string WatchRow = "| U-011 | Core drawer | | BatchRendererGroup | Watch "
                                        + "| 6000.4; com.unity.render-pipelines.universal 17.4.0 "
                                        + "| Data-driven instances | docs 6000.4 "
                                        + "| com.unity.render-pipelines.universal; com.unity.entities.graphics "
                                        + "| BatchRendererGroup; GPU Resident Drawer; DOTS instancing |";

        private static string Fixture => "# Ledger\r\n\r\nSome prose.\r\n\r\n" + Header + "\r\n" + Separator + "\r\n"
                                         + MovedRow + "\r\n" + WatchRow + "\r\n\r\nAfter the table.\r\n";

        [Test]
        public void TheFixtureParsesToTwoRowsWithEveryField() {
            var problems = new List<string>();
            var rows = LedgerTable.Parse(Fixture, "Ledger.md", problems);

            CollectionAssert.IsEmpty(problems);
            Assert.AreEqual(2, rows.Count);

            var moved = rows[0];
            Assert.AreEqual("U-004", moved.Id);
            Assert.AreEqual("Core ObjectPool", moved.Ours);
            Assert.AreEqual("Spellbound.Core.ObjectPooling.ObjectPool`1", moved.OursType);
            Assert.AreEqual("UnityEngine.Pool.ObjectPool", moved.UnityItem);
            Assert.AreEqual(LedgerVerdict.Moved, moved.Verdict);
            Assert.AreEqual(1, moved.JudgedAt.Count);
            Assert.IsTrue(moved.EditorJudgement.HasValue);
            Assert.AreEqual("6000.4", moved.EditorJudgement.Value.Version);
            Assert.AreEqual("Same job with delegates", moved.Reason);
            Assert.AreEqual("docs 6000.4 Pool.ObjectPool_1", moved.Evidence);
            CollectionAssert.IsEmpty(moved.WatchPackages);
            CollectionAssert.IsEmpty(moved.WatchWords);
            Assert.AreEqual("Ledger.md", moved.Source);
            Assert.AreEqual(7, moved.Line);

            var watch = rows[1];
            Assert.AreEqual("U-011", watch.Id);
            Assert.IsNull(watch.OursType);
            Assert.AreEqual(LedgerVerdict.Watch, watch.Verdict);
            Assert.AreEqual(2, watch.JudgedAt.Count);
            Assert.IsTrue(watch.TryJudgementFor("com.unity.render-pipelines.universal", out var judgement));
            Assert.AreEqual("17.4.0", judgement.Version);
            Assert.IsFalse(watch.TryJudgementFor("com.unity.entities.graphics", out _));
            CollectionAssert.AreEqual(new[] { "com.unity.render-pipelines.universal", "com.unity.entities.graphics" },
                watch.WatchPackages);
            CollectionAssert.AreEqual(new[] { "BatchRendererGroup", "GPU Resident Drawer", "DOTS instancing" },
                watch.WatchWords);
            Assert.AreEqual(8, watch.Line);
            Assert.AreEqual("U-011 Core drawer: Watch", watch.ToString());
        }

        [Test]
        public void ARowWithNineCellsIsSkippedAndReportedWithItsLine() {
            var problems = new List<string>();
            var text = Header + "\n" + Separator + "\n| U-001 | a | | b | Keeps | 6000.4 | r | e | |\n" + MovedRow;
            var rows = LedgerTable.Parse(text, "Short.md", problems);

            Assert.AreEqual(1, rows.Count);
            Assert.AreEqual("U-004", rows[0].Id);
            Assert.AreEqual(1, problems.Count);
            StringAssert.StartsWith("Short.md:3: ", problems[0]);
            StringAssert.Contains("9 cells", problems[0]);
        }

        [Test]
        public void AnUnknownVerdictIsReported() {
            var problems = new List<string>();
            var text = Header + "\n" + Separator + "\n| U-002 | a | | b | Maybe | 6000.4 | r | e | | |";
            var rows = LedgerTable.Parse(text, "Verdict.md", problems);

            CollectionAssert.IsEmpty(rows);
            Assert.AreEqual(1, problems.Count);
            StringAssert.StartsWith("Verdict.md:3: unknown verdict 'Maybe'", problems[0]);
        }

        [Test]
        public void AnUnparsableJudgementIsReported() {
            var problems = new List<string>();
            var text = Header + "\n" + Separator + "\n| U-003 | a | | b | Keeps | soon | r | e | | |";

            CollectionAssert.IsEmpty(LedgerTable.Parse(text, "Judged.md", problems));
            StringAssert.StartsWith("Judged.md:3: Judged at entry 'soon'", problems[0]);
        }

        [Test]
        public void BuildsOnReadsAsBuildsOn() {
            var text = Header + "\n" + Separator + "\n| U-005 | a | | b | builds on | 6000.4 | r | e | | |";
            var rows = LedgerTable.Parse(text, "Builds.md");

            Assert.AreEqual(LedgerVerdict.BuildsOn, rows[0].Verdict);
            Assert.IsTrue(LedgerVerdicts.TryParse("Builds on", out var spaced));
            Assert.AreEqual(LedgerVerdict.BuildsOn, spaced);
            Assert.IsTrue(LedgerVerdicts.TryParse("buildson", out var joined));
            Assert.AreEqual(LedgerVerdict.BuildsOn, joined);
            Assert.AreEqual("Builds on", LedgerVerdicts.Text(LedgerVerdict.BuildsOn));
        }

        [Test]
        public void ATextWithNoTableGivesNoRows() {
            CollectionAssert.IsEmpty(LedgerTable.Parse("# Nothing\n\n- a bullet\n| a | b |\n", "None.md"));
            CollectionAssert.IsEmpty(LedgerTable.Parse(null, "None.md"));
        }

        [Test]
        public void TwoTablesInOneTextAreBothRead() {
            var text = Header + "\n" + Separator + "\n" + MovedRow + "\n\nBetween.\n\n" + Header + "\n" + Separator
                       + "\n" + WatchRow + "\n";
            var rows = LedgerTable.Parse(text, "Two.md");

            Assert.AreEqual(2, rows.Count);
            Assert.AreEqual("U-004", rows[0].Id);
            Assert.AreEqual("U-011", rows[1].Id);
        }

        [Test]
        public void RenderThenParseRoundTrips() {
            var rows = LedgerTable.Parse(Fixture, "Ledger.md");
            var again = LedgerTable.Parse(LedgerTable.Render(rows), "Ledger.md");

            Assert.AreEqual(rows.Count, again.Count);

            for (var i = 0; i < rows.Count; i++) {
                Assert.AreEqual(rows[i].Id, again[i].Id);
                Assert.AreEqual(rows[i].Ours, again[i].Ours);
                Assert.AreEqual(rows[i].OursType, again[i].OursType);
                Assert.AreEqual(rows[i].UnityItem, again[i].UnityItem);
                Assert.AreEqual(rows[i].Verdict, again[i].Verdict);
                CollectionAssert.AreEqual(rows[i].JudgedAt, again[i].JudgedAt);
                Assert.AreEqual(rows[i].Reason, again[i].Reason);
                Assert.AreEqual(rows[i].Evidence, again[i].Evidence);
                CollectionAssert.AreEqual(rows[i].WatchPackages, again[i].WatchPackages);
                CollectionAssert.AreEqual(rows[i].WatchWords, again[i].WatchWords);
            }

            StringAssert.StartsWith(Header, LedgerTable.Render(rows));
        }
    }
}
