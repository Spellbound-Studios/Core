// Copyright (c) Spellbound Studios. All rights reserved.

using NUnit.Framework;
using Spellbound.Core.Ledger.Editor;

namespace Spellbound.Core.Tests {
    public class ChangelogScanTests {
        private const string Changelog = "# Changelog\r\n\r\n"
                                         + "## [Unreleased]\r\n\r\n- BatchRendererGroup unreleased work.\r\n\r\n"
                                         + "## [17.6.0] - 2026-09-01\r\n\r\n### Added\r\n"
                                         + "- Added a BatchRendererGroup culling callback.\r\n"
                                         + "- Fixed a shadow bug.\r\n\r\n"
                                         + "## [17.5.0] - 2026-06-01\r\n\r\n"
                                         + "- Changed batchrenderergroup upload.\r\n- Fixed decals.\r\n\r\n"
                                         + "## [17.4.0] - 2026-03-01\r\n\r\n- Added BatchRendererGroup support.\r\n";

        [Test]
        public void TheMatchingLinesOfTheNewerSectionsCarryTheirVersion() {
            var lines = ChangelogScan.LinesBetween(Changelog, "17.4.0", "17.6.0", new[] { "BatchRendererGroup" });

            CollectionAssert.AreEqual(new[] {
                "[17.6.0] - Added a BatchRendererGroup culling callback.",
                "[17.5.0] - Changed batchrenderergroup upload."
            }, lines);
        }

        [Test]
        public void NoWordsReturnsEveryLineOfThoseSections() {
            var lines = ChangelogScan.LinesBetween(Changelog, "17.4.0", "17.6.0", new string[0]);

            CollectionAssert.AreEqual(new[] {
                "[17.6.0] ### Added",
                "[17.6.0] - Added a BatchRendererGroup culling callback.",
                "[17.6.0] - Fixed a shadow bug.",
                "[17.5.0] - Changed batchrenderergroup upload.",
                "[17.5.0] - Fixed decals."
            }, lines);
        }

        [Test]
        public void TheUpperVersionIsTheLastSectionRead() {
            var lines = ChangelogScan.LinesBetween(Changelog, "17.4.0", "17.5.0", new[] { "BatchRendererGroup" });

            CollectionAssert.AreEqual(new[] { "[17.5.0] - Changed batchrenderergroup upload." }, lines);
        }

        [Test]
        public void NoChangelogGivesNoLines() {
            var lines = ChangelogScan.LinesBetween(null, "17.4.0", "17.6.0", new[] { "BatchRendererGroup" });

            CollectionAssert.IsEmpty(lines);
        }
    }
}
