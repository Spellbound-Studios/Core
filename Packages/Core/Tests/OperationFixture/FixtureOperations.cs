// Copyright (c) Spellbound Studios. All rights reserved.

using System.Collections.Generic;
using System.Threading.Tasks;
using Spellbound.Core.Observations;
using Spellbound.Core.Operations;
using UnityEngine;

[assembly: OperationGroup("fixture", "Operations that exist so the catalog and the host can be tested.")]

namespace Spellbound.Core.Tests.OperationFixture {
    public static class FixtureOperations {
        public static int WipeCount { get; private set; }

        [Operation("echo", "Returns the text it was given.", OperationEffect.Reads, Audience = OperationAudiences.Anyone)]
        public static string Echo([Argument("The text to return.")] string text) => text;

        [Operation("add", "Adds two integers.", OperationEffect.Reads)]
        public static string Add([Argument("The left operand.")] int left, [Argument("The right operand.")] int right = 1) =>
            (left + right).ToString();

        [Operation("tier", "Names a tier.", OperationEffect.Reads)]
        public static string Tier([Argument("The tier.")] FixtureTier tier) => tier.ToString();

        [Operation("sum", "Sums a list of integers.", OperationEffect.Reads)]
        public static string Sum([Argument("The integers to sum.")] int[] values) {
            var total = 0;

            foreach (var value in values) {
                total += value;
            }

            return total.ToString();
        }

        [Operation("move", "Reports a position.", OperationEffect.Reads)]
        public static string Move([Argument("Where to.")] Vector3 at) => at.x + "/" + at.y + "/" + at.z;

        [Operation("paint", "Reports a colour.", OperationEffect.Reads)]
        public static string Paint([Argument("The colour.")] Color colour) => ColorUtility.ToHtmlStringRGBA(colour);

        [Operation("flag", "Reports a flag.", OperationEffect.Reads)]
        public static string Flag([Argument("The flag.")] bool on, [Argument("A number after the flag.")] int count = 0) =>
            on + ":" + count;

        [Operation("wipe", "Deletes everything, or says what it would delete.", OperationEffect.Destroys)]
        public static string Wipe(OperationContext context) {
            if (context.DryRun) {
                return "would wipe 3 things";
            }

            WipeCount++;
            return "wiped 3 things";
        }

        [Operation("secret", "Needs the approver audience.", OperationEffect.Writes, Audience = OperationAudiences.Approver)]
        public static string Secret() => "approved";

        [Operation("playing-only", "Exists only while playing.", OperationEffect.Reads, Availability = OperationAvailability.WhilePlaying)]
        public static string PlayingOnly() => "playing";

        [Operation("editing-only", "Exists only while editing.", OperationEffect.Reads, Availability = OperationAvailability.WhileEditing)]
        public static string EditingOnly() => "editing";

        [Operation("boom", "Refuses with a hint.", OperationEffect.Reads)]
        public static string Boom() => throw new OperationException("No match score exists.", "Start a match first.");

        [Operation("crash", "Throws an ordinary exception.", OperationEffect.Reads)]
        public static string Crash() => throw new System.InvalidOperationException("broken");

        [Operation("wait", "Completes after a delay.", OperationEffect.Reads)]
        public static async Task<string> Wait([Argument("How long to wait.")] int milliseconds) {
            await Task.Delay(milliseconds);
            return "waited " + milliseconds;
        }

        [Operation("coordinate", "Reports a coordinate parsed by a custom parser.", OperationEffect.Reads)]
        public static string Coordinate([Argument("The coordinate as column:row.")] FixtureCoordinate at) => at.Column + "," + at.Row;

        [Operation("badge", "Returns a value a custom renderer draws.", OperationEffect.Reads)]
        public static FixtureBadge Badge() => new FixtureBadge("gold");

        [Operation("rows", "Returns a table.", OperationEffect.Reads)]
        public static OperationTable Rows() => new OperationTable("name", "count").AddRow("apples", 3).AddRow("pears", 12);

        [Operation("many", "Returns many lines.", OperationEffect.Reads)]
        public static List<string> Many([Argument("How many lines.")] int count) {
            var lines = new List<string>();

            for (var i = 0; i < count; i++) {
                lines.Add("line " + i);
            }

            return lines;
        }

        [Operation("list", "Returns many lines and takes paging arguments.", OperationEffect.Reads)]
        public static List<string> List([Argument("How many lines.")] int count, [Argument("Skip.")] int offset = 0,
            [Argument("Take.")] int limit = 1000) => Many(count);

        [Operation("progress", "Reports progress lines.", OperationEffect.Reads)]
        public static string Progress(OperationContext context) {
            context.Report("step 1");
            context.Report("step 2");
            return "done";
        }

        [Operation("verdict", "Returns a ready result.", OperationEffect.Reads)]
        public static OperationResult Verdict() => OperationResult.Refused("not today");

        [Operation("nothing", "Returns null.", OperationEffect.Reads)]
        public static string Nothing() => null;

        [Operation("camel-case-name", "Has a camel case parameter.", OperationEffect.Reads)]
        public static string CamelCaseName([Argument("A camel case parameter.")] int maxHitPoints) => maxHitPoints.ToString();

        [Operation("observe", "Remembers the observation context the run was given.", OperationEffect.Reads)]
        public static string Observe(OperationContext context) {
            LastObservations = context.Observations;
            return "observed " + context.Observations.Name;
        }

        public static ObservationContext LastObservations { get; private set; }

        public static void Reset() {
            WipeCount = 0;
            LastObservations = null;
        }
    }
}
