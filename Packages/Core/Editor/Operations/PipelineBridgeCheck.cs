// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Collections.Generic;
using Spellbound.Core.Logging;
using UnityEditor;

namespace Spellbound.Core.Operations.Editor {
    public static class PipelineBridgeCheck {
        private const string SessionKey = "Spellbound.Core.PipelineBridgeCheck.Reported";

        [InitializeOnLoadMethod]
        private static void Run() {
            if (SessionState.GetBool(SessionKey, false)) {
                return;
            }

            var message = Describe(EditorInstalledPackages.VersionOf(PipelineBridgeRanges.PipelinePackageName), BridgeLoaded(),
                PipelineBridgeRanges.Read());

            if (message == null) {
                return;
            }

            SessionState.SetBool(SessionKey, true);

            using (Log.AddScopedSink(new UnityConsoleSink(), null, LogLevel.Warning)) {
                Log.Emit(LogLevel.Warning, message, nameof(PipelineBridgeCheck), nameof(Run), 0);
            }
        }

        public static bool BridgeLoaded() {
            return Type.GetType(PipelineBridgeRanges.BridgeTypeName) != null;
        }

        public static string Describe(string installedPipelineVersion, bool bridgeLoaded, IReadOnlyList<string> supportedRanges) {
            if (string.IsNullOrEmpty(installedPipelineVersion) || bridgeLoaded) {
                return null;
            }

            var ranges = supportedRanges == null || supportedRanges.Count == 0 ? "range (none declared)" : string.Join(" or ", supportedRanges);
            return "Spellbound agent road off: " + PipelineBridgeRanges.PipelinePackageName + " " + installedPipelineVersion
                   + " is outside the supported " + ranges + "; update Spellbound.Core or install a Pipeline inside " + ranges + ".";
        }
    }
}
