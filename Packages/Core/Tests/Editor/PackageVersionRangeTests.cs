// Copyright (c) Spellbound Studios. All rights reserved.

using NUnit.Framework;
using Spellbound.Core.Operations.Editor;

namespace Spellbound.Core.Tests {
    public class PackageVersionRangeTests {
        [Test]
        public void PrereleaseOrdersBelowItsRelease() {
            Assert.Less(PackageVersionRange.Compare("0.8.0-exp.1", "0.8.0"), 0);
            Assert.Less(PackageVersionRange.Compare("0.8.0-exp.1", "0.8.0-exp.2"), 0);
            Assert.Less(PackageVersionRange.Compare("0.8.0-exp.2", "0.8.0-exp.10"), 0);
            Assert.Less(PackageVersionRange.Compare("0.9.1-exp.1", "0.10.0-exp.1"), 0);
            Assert.AreEqual(0, PackageVersionRange.Compare("1.2.3", "1.2.3+build"));
        }

        [Test]
        public void HalfOpenRangeIncludesItsLowerBoundOnly() {
            var range = PackageVersionRange.Parse("[0.8.0-exp.1,0.9.0-exp.1)");
            Assert.IsTrue(range.Contains("0.8.0-exp.1"));
            Assert.IsTrue(range.Contains("0.8.3"));
            Assert.IsFalse(range.Contains("0.9.0-exp.1"));
            Assert.IsFalse(range.Contains("0.7.0-exp.1"));
        }

        [Test]
        public void OtherShapesParse() {
            Assert.IsTrue(PackageVersionRange.Parse("(1.0,2.0]").Contains("2.0"));
            Assert.IsFalse(PackageVersionRange.Parse("(1.0,2.0]").Contains("1.0"));
            Assert.IsTrue(PackageVersionRange.Parse("[2.4.5]").Contains("2.4.5"));
            Assert.IsFalse(PackageVersionRange.Parse("[2.4.5]").Contains("2.4.6"));
            Assert.IsTrue(PackageVersionRange.Parse("1.3").Contains("4.0"));
            Assert.IsFalse(PackageVersionRange.Parse("1.3").Contains("1.2.9"));
        }

        [Test]
        public void TheBridgeRangesSupportTheKnownPipelineVersions() {
            var ranges = new[] { "[0.8.0-exp.1,0.9.0-exp.1)", "[0.9.0-exp.1,0.10.0-exp.1)" };
            Assert.IsTrue(PipelineBridgeRanges.Supports("0.8.0-exp.1", ranges));
            Assert.IsTrue(PipelineBridgeRanges.Supports("0.9.1-exp.1", ranges));
            Assert.IsFalse(PipelineBridgeRanges.Supports("0.7.0-exp.1", ranges));
            Assert.IsFalse(PipelineBridgeRanges.Supports("0.10.0-exp.1", ranges));
        }
    }
}
