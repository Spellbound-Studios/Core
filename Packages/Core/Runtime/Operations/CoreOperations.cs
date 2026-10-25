// Copyright (c) Spellbound Studios. All rights reserved.

using System.Text;
using Spellbound.Core.Operations;
using UnityEngine;
using UnityEngine.Rendering;

[assembly: OperationGroup("core",
    "Gives every package one vocabulary: how to log, pack bytes, identify a thing, roll from a seeded stream, count steps of time, place a pose in a reference frame, measure a cost against a budget, declare an operation, and validate what a designer or an agent authors.")]

namespace Spellbound.Core.Operations {
    public static class CoreOperations {
        [Operation("help", "Lists the operation groups, or the operations of one group with their arguments.", OperationEffect.Reads,
            Audience = OperationAudiences.Anyone)]
        public static string Help(OperationContext context,
            [Argument("The group to list; leave it out to list every group.")] string group = null,
            [Argument("True to list every operation of every group.")] bool all = false) {
            var catalog = context.Host.Catalog;
            var builder = new StringBuilder();

            if (!string.IsNullOrWhiteSpace(group)) {
                var found = catalog.Group(group);

                if (found == null) {
                    throw new OperationException("no group '" + group + "'", "Run: core help");
                }

                AppendGroup(builder, found, true);
                return builder.ToString();
            }

            var operations = 0;

            foreach (var entry in catalog.Groups) {
                operations += entry.Operations.Count;
            }

            builder.Append(catalog.Groups.Count).Append(" groups, ").Append(operations).Append(" operations");

            foreach (var entry in catalog.Groups) {
                builder.Append('\n');
                AppendGroup(builder, entry, all);
            }

            if (!all) {
                builder.Append("\nRun: core help --group <name>, or core help --all");
            }

            return builder.ToString();
        }

        [Operation("about", "Describes this process: the Unity version, the render pipeline and the operation groups it holds.",
            OperationEffect.Reads, Audience = OperationAudiences.Anyone)]
        public static string About(OperationContext context) {
            var builder = new StringBuilder();
            builder.Append("Unity ").Append(Application.unityVersion);
            builder.Append(", ").Append(Application.isEditor ? "editor" : "player");
            builder.Append(Application.isPlaying ? ", playing" : ", not playing");
            var pipeline = GraphicsSettings.currentRenderPipeline;
            builder.Append("\nrender pipeline: ").Append(pipeline != null ? pipeline.name : "built-in");

            foreach (var group in context.Host.Catalog.Groups) {
                builder.Append('\n').Append(group.Name).Append(": ").Append(group.Operations.Count).Append(" operations in ")
                    .Append(string.Join(", ", group.Assemblies));
            }

            return builder.ToString();
        }

        [Operation("history", "Lists the operation lines this host has run, oldest first, with their status, caller and frame.",
            OperationEffect.Reads)]
        public static OperationTable History(OperationContext context,
            [Argument("How many entries to skip from the start.")] int offset = 0,
            [Argument("How many entries to list.")] int limit = 50) {
            var table = new OperationTable("frame", "caller", "status", "line");
            var history = context.Host.History;

            for (var i = offset; i < history.Count && i < offset + limit; i++) {
                var entry = history[i];
                table.AddRow(entry.Frame, entry.Caller, OperationResult.StatusWord(entry.Status), entry.Line);
            }

            return table;
        }

        [Operation("stats", "Lists each operation this host has run with its calls, failures and durations in milliseconds.",
            OperationEffect.Reads)]
        public static OperationTable Statistics(OperationContext context) {
            var table = new OperationTable("operation", "calls", "failures", "total ms", "max ms");

            foreach (var statistic in context.Host.Statistics) {
                table.AddRow(statistic.Line, statistic.Calls, statistic.Failures, statistic.TotalMilliseconds.ToString("F3"),
                    statistic.MaximumMilliseconds.ToString("F3"));
            }

            return table;
        }

        private static void AppendGroup(StringBuilder builder, OperationGroupDescription group, bool withOperations) {
            builder.Append(group.Name);

            if (!string.IsNullOrWhiteSpace(group.Job)) {
                builder.Append(": ").Append(group.Job);
            }

            if (!withOperations) {
                builder.Append(" (").Append(group.Operations.Count).Append(" operations)");
                return;
            }

            foreach (var operation in group.Operations) {
                builder.Append("\n  ").Append(operation.Usage());
                builder.Append("\n    ").Append(operation.Summary);
                builder.Append(" [").Append(operation.Effect.ToString().ToLowerInvariant()).Append(", ").Append(operation.Audience);

                if (operation.Availability != OperationAvailability.Anytime) {
                    builder.Append(", ").Append(ArgumentText.KebabCase(operation.Availability.ToString()));
                }

                builder.Append(']');

                foreach (var argument in operation.Arguments) {
                    if (string.IsNullOrWhiteSpace(argument.Summary)) {
                        continue;
                    }

                    builder.Append("\n    --").Append(argument.Name).Append(": ").Append(argument.Summary);
                }
            }
        }
    }
}
