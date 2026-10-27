// Copyright (c) Spellbound Studios. All rights reserved.

using System.Collections.Generic;
using NUnit.Framework;
using Spellbound.Core.Ledger;
using Spellbound.Core.Ledger.Editor;

namespace Spellbound.Core.Tests {
    public class LedgerCheckTests {
        private const string Universal = "com.unity.render-pipelines.universal";
        private const string Entities = "com.unity.entities.graphics";

        private const string Changelog = "## [17.6.0] - 2026-09-01\n"
                                         + "- Added a BatchRendererGroup callback.\n- Fixed fog.\n"
                                         + "## [17.5.0] - 2026-06-01\n- Fixed decals.\n"
                                         + "## [17.4.0] - 2026-03-01\n- Added BatchRendererGroup support.\n";

        private HashSet<string> _types;
        private Dictionary<string, string> _installed;
        private Dictionary<string, string> _changelogs;

        [SetUp]
        public void SetUp() {
            _types = new HashSet<string> { "Spellbound.Core.Present", "Spellbound.Core.ObjectPooling.ObjectPool`1" };
            _installed = new Dictionary<string, string> { { Universal, "17.6.0" } };
            _changelogs = new Dictionary<string, string> { { Universal, Changelog } };
        }

        private LedgerCheck Check(string editorVersion = "6000.4.8f1") {
            return new LedgerCheck(editorVersion, Presence,
                name => _installed.TryGetValue(name, out var version) ? version : null,
                name => _changelogs.TryGetValue(name, out var text) ? text : null);
        }

        private LedgerTypePresence Presence(string name) {
            if (_types.Contains(name)) {
                return LedgerTypePresence.Present;
            }

            return name.StartsWith("Spellbound.Core.")
                ? LedgerTypePresence.Absent
                : LedgerTypePresence.AssemblyNotLoaded;
        }

        private static LedgerRow Row(string id, LedgerVerdict verdict, string type, string judged,
            string[] watchPackages = null, string[] watchWords = null) {
            var judgedAt = new List<LedgerJudgement>();

            foreach (var entry in judged.Split(';')) {
                if (LedgerJudgement.TryParse(entry, out var judgement)) {
                    judgedAt.Add(judgement);
                }
            }

            return new LedgerRow(id, "Core " + id, type, "Unity item", verdict, judgedAt, "reason", "evidence",
                watchPackages, watchWords, "Fixture.md", 3);
        }

        private LedgerRowResult One(LedgerRow row, string editorVersion = "6000.4.8f1") {
            return Check(editorVersion).Run(new[] { row }, null, 1).Rows[0];
        }

        [Test]
        public void AMovedRowWhoseTypeExistsIsWrong() {
            var result = One(Row("U-004", LedgerVerdict.Moved, "Spellbound.Core.ObjectPooling.ObjectPool`1", "6000.4"));

            Assert.IsTrue(result.IsWrong);
            Assert.IsFalse(result.IsStale);
            CollectionAssert.Contains(result.Lines,
                "type Spellbound.Core.ObjectPooling.ObjectPool`1 still exists; a Moved row's type is deleted");
        }

        [Test]
        public void AKeepsRowWhoseTypeIsGoneIsWrong() {
            var result = One(Row("U-001", LedgerVerdict.Keeps, "Spellbound.Core.Gone", "6000.4"));

            Assert.IsTrue(result.IsWrong);
            CollectionAssert.Contains(result.Lines,
                "type Spellbound.Core.Gone no longer exists; re-judge or delete the row");
        }

        [Test]
        public void ABuildsOnRowWhoseTypeExistsIsFine() {
            var result = One(Row("U-002", LedgerVerdict.BuildsOn, "Spellbound.Core.Present", "6000.4"));

            Assert.IsFalse(result.IsWrong);
            Assert.IsFalse(result.IsStale);
            CollectionAssert.IsEmpty(result.Lines);
        }

        [Test]
        public void AWatchRowWithNoTypeIsNeitherStaleNorWrong() {
            var result = One(Row("U-003", LedgerVerdict.Watch, null, "6000.4"));

            Assert.IsFalse(result.IsWrong);
            Assert.IsFalse(result.IsStale);
        }

        [Test]
        public void AnOlderEditorJudgementIsStaleWithTheSearchAddress() {
            var result = One(Row("U-005", LedgerVerdict.Watch, null, "6000.3", null, new[] { "Resident Drawer" }));

            Assert.IsTrue(result.IsStale);
            Assert.IsFalse(result.IsWrong);
            Assert.AreEqual("editor 6000.4 passed since 6000.3; read "
                            + "https://docs.unity3d.com/6000.4/Documentation/Manual/30_search.html?q=Resident+Drawer",
                result.Lines[0]);
        }

        [Test]
        public void AJudgementAtTheRunningEditorIsCurrent() {
            Assert.IsFalse(One(Row("U-006", LedgerVerdict.Watch, null, "6000.4")).IsStale);
        }

        [Test]
        public void ARowWithNoEditorJudgementIsWrong() {
            var result = One(Row("U-007", LedgerVerdict.Watch, null, Universal + " 17.6.0"));

            Assert.IsTrue(result.IsWrong);
            CollectionAssert.Contains(result.Lines, "no editor version in Judged at");
        }

        [Test]
        public void AWatchPackageThatMovedIsStaleWithItsChangelogLines() {
            var row = Row("U-011", LedgerVerdict.Watch, null, "6000.4; " + Universal + " 17.4.0", new[] { Universal },
                new[] { "BatchRendererGroup" });
            var result = One(row);

            Assert.IsTrue(result.IsStale);
            Assert.IsFalse(result.IsWrong);
            CollectionAssert.AreEqual(new[] {
                "watch package " + Universal + " moved 17.4.0 to 17.6.0",
                "  [17.6.0] - Added a BatchRendererGroup callback."
            }, result.Lines);
        }

        [Test]
        public void AWatchPackageWithNoWatchedLineSaysSo() {
            var row = Row("U-012", LedgerVerdict.Watch, null, "6000.4; " + Universal + " 17.4.0", new[] { Universal },
                new[] { "Volumetric" });

            CollectionAssert.Contains(One(row).Lines, "  no changelog line carries a watch word");
            _changelogs.Clear();
            CollectionAssert.Contains(One(row).Lines, "  no CHANGELOG.md to read");
        }

        [Test]
        public void AWatchPackageNotInstalledAddsItsLineAndIsNotStale() {
            var row = Row("U-013", LedgerVerdict.Watch, null, "6000.4; " + Entities + " 1.3.0", new[] { Entities });
            var result = One(row);

            Assert.IsFalse(result.IsStale);
            Assert.IsFalse(result.IsWrong);
            CollectionAssert.AreEqual(new[] { "watch package " + Entities + " is not installed here" }, result.Lines);
        }

        [Test]
        public void AWatchPackageWithNoJudgementIsWrong() {
            var result = One(Row("U-014", LedgerVerdict.Watch, null, "6000.4", new[] { Universal }));

            Assert.IsTrue(result.IsWrong);
            CollectionAssert.Contains(result.Lines,
                "watch package " + Universal + " has no judged version in Judged at");
        }

        [Test]
        public void TheResultTextsHaveTheirShapes() {
            var rows = new[] {
                Row("U-004", LedgerVerdict.Moved, "Spellbound.Core.ObjectPooling.ObjectPool`1", "6000.3"),
                Row("U-002", LedgerVerdict.BuildsOn, "Spellbound.Core.Present", "6000.4"),
                Row("U-005", LedgerVerdict.Watch, null, "6000.3")
            };
            var result = Check().Run(rows, new[] { "Other.md:4: the row has an empty Id" }, 2);

            Assert.IsFalse(result.Passed);
            Assert.AreEqual(2, result.StaleCount);
            Assert.AreEqual(1, result.WrongCount);
            Assert.AreEqual("2 rows stale, 1 rows wrong over 3 rows from 2 sources", result.Summary());

            var text = result.ToText().Split('\n');
            Assert.AreEqual(result.Summary(), text[0]);
            Assert.AreEqual("U-004 Core U-004 [Moved] stale, wrong", text[1]);
            Assert.AreEqual("  type Spellbound.Core.ObjectPooling.ObjectPool`1 still exists; "
                            + "a Moved row's type is deleted", text[2]);
            StringAssert.StartsWith("  editor 6000.4 passed since 6000.3; read https://docs.unity3d.com/6000.4/",
                text[3]);
            Assert.AreEqual("U-005 Core U-005 [Watch] stale", text[4]);
            Assert.AreEqual("problems:", text[6]);
            Assert.AreEqual("  Other.md:4: the row has an empty Id", text[7]);

            var form = result.MoveFormLines().Split('\n');
            Assert.AreEqual(2, form.Length);
            Assert.AreEqual("- U-004 Core U-004: type Spellbound.Core.ObjectPooling.ObjectPool`1 still exists; "
                            + "a Moved row's type is deleted", form[0]);
            StringAssert.StartsWith("- U-005 Core U-005: editor 6000.4 passed since 6000.3; read ", form[1]);
        }

        [Test]
        public void ACleanRunPasses() {
            var rows = new[] { Row("U-002", LedgerVerdict.BuildsOn, "Spellbound.Core.Present", "6000.4") };
            var result = Check().Run(rows, null, 1);

            Assert.IsTrue(result.Passed);
            Assert.AreEqual("0 rows stale, 0 rows wrong over 1 rows from 1 sources", result.ToText());
            Assert.AreEqual(string.Empty, result.MoveFormLines());
        }
    }
}
