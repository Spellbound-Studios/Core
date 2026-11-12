// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Globalization;

namespace Spellbound.Core.Logging {
    internal readonly struct LogCallSite : IEquatable<LogCallSite> {
        public readonly string Source;
        public readonly string Member;
        public readonly int Line;

        public LogCallSite(string source, string member, int line) {
            Source = source ?? string.Empty;
            Member = member ?? string.Empty;
            Line = line;
        }

        public bool Equals(LogCallSite other) =>
            Line == other.Line
            && string.Equals(Source, other.Source, StringComparison.Ordinal)
            && string.Equals(Member, other.Member, StringComparison.Ordinal);

        public override bool Equals(object obj) => obj is LogCallSite other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Source, Member, Line);

        public override string ToString() => Source + "." + Member + ":" + Line.ToString(CultureInfo.InvariantCulture);
    }
}
