// Copyright (c) Spellbound Studios. All rights reserved.

using System.Collections.Generic;
using System.Text;

namespace Spellbound.Core.Operations {
    public static class AgentContextBlocks {
        public const string OperationsBlock = "operations";
        public const string DoorsBlock = "doors";

        public static string Operations(OperationGroupDescription group, string packageLine) {
            var builder = new StringBuilder();

            if (!string.IsNullOrWhiteSpace(packageLine)) {
                builder.Append(packageLine.Trim()).Append('\n');
            }

            builder.Append("Operations of the group `").Append(group.Name).Append("`, run as `<group> <operation> [arguments]`:");

            if (group.Operations.Count == 0) {
                builder.Append("\n- none yet");
            }

            foreach (var operation in group.Operations) {
                builder.Append("\n- `").Append(operation.Usage()).Append("`: ").Append(operation.Summary);
                builder.Append(" [").Append(operation.Effect.ToString().ToLowerInvariant()).Append(", ").Append(operation.Audience);

                if (operation.Availability != OperationAvailability.Anytime) {
                    builder.Append(", ").Append(ArgumentText.KebabCase(operation.Availability.ToString()));
                }

                builder.Append(']');

                foreach (var argument in operation.Arguments) {
                    builder.Append("\n  - `--").Append(argument.Name).Append("`");

                    if (!string.IsNullOrWhiteSpace(argument.Summary)) {
                        builder.Append(": ").Append(argument.Summary);
                    }

                    if (argument.HasDefault) {
                        builder.Append(" Default: ").Append(argument.Default == null ? "none" : argument.DefaultText).Append('.');
                    }
                }
            }

            return builder.ToString();
        }

        public static string Doors(IReadOnlyList<BridgeDoor> doors) {
            var builder = new StringBuilder();
            builder.Append("Bridge doors in this project, the constructors of the types a bridge assembly adds:");

            if (doors == null || doors.Count == 0) {
                builder.Append("\n- none");
                return builder.ToString();
            }

            foreach (var door in doors) {
                builder.Append("\n- `").Append(door.TypeName).Append("` in `").Append(door.AssemblyName).Append('`');

                foreach (var member in door.Members) {
                    builder.Append("\n  - `").Append(member).Append('`');
                }
            }

            return builder.ToString();
        }
    }
}
