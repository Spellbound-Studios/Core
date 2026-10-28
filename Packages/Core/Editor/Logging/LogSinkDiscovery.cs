// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Collections.Generic;
using UnityEditor;

namespace Spellbound.Core.Logging.Editor {
    public static class LogSinkDiscovery {
        public static IReadOnlyList<LogSinkDescription> Discover() {
            var found = new List<LogSinkDescription>();

            foreach (var type in TypeCache.GetTypesDerivedFrom<ILogSink>()) {
                if (!IsDiscoverable(type)) {
                    continue;
                }

                try {
                    var sink = (ILogSink)Activator.CreateInstance(type);
                    found.Add(new LogSinkDescription(type.AssemblyQualifiedName, sink.DisplayName));
                }
                catch (Exception exception) {
                    Log.Error("The log sink " + type.FullName + " could not be created: " + exception.Message);
                }
            }

            found.Sort((left, right) => {
                var byName = string.CompareOrdinal(left.DisplayName, right.DisplayName);
                return byName != 0 ? byName : string.CompareOrdinal(left.QualifiedTypeName, right.QualifiedTypeName);
            });

            return found;
        }

        public static bool AppendMissing(LogConfig config) {
            return AppendMissing(config, Discover());
        }

        public static bool AppendMissing(LogConfig config, IEnumerable<LogSinkDescription> discovered) {
            if (config == null) {
                throw new ArgumentNullException(nameof(config));
            }

            var current = config.sinks ?? Array.Empty<SinkEntry>();
            var known = new HashSet<string>(StringComparer.Ordinal);

            foreach (var entry in current) {
                if (entry.qualifiedTypeName != null) {
                    known.Add(entry.qualifiedTypeName);
                }
            }

            var entries = new List<SinkEntry>(current);
            var lowestLevel = LogLevelDefines.CompiledLevels(config.globalLevel)[0];

            if (discovered != null) {
                foreach (var sink in discovered) {
                    if (sink == null || !known.Add(sink.QualifiedTypeName)) {
                        continue;
                    }

                    entries.Add(new SinkEntry {
                        qualifiedTypeName = sink.QualifiedTypeName,
                        displayName = sink.DisplayName,
                        enabled = false,
                        filterLevel = lowestLevel
                    });
                }
            }

            if (entries.Count == current.Length) {
                return false;
            }

            config.sinks = entries.ToArray();
            return true;
        }

        private static bool IsDiscoverable(Type type) {
            return type.IsPublic && !type.IsAbstract && !type.ContainsGenericParameters
                   && type.GetConstructor(Type.EmptyTypes) != null;
        }
    }
}
