// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Spellbound.Core.Sections;
using Spellbound.Core.Simulation;
using Spellbound.Core.Tooling;

namespace Spellbound.Core.Tests {
    public class SavedTypesHoldNoTickTests {
        private const BindingFlags InstanceFields =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

        [Test]
        public void NoSectionTypeHoldsATickATickSpanOrATickRate() {
            var offenders = new List<string>();

            foreach (var assembly in AssemblyScanning.ScannableAssemblies()) {
                foreach (var type in AssemblyScanning.LoadableTypes(assembly)) {
                    if (!type.IsDefined(typeof(SectionAttribute), false)) {
                        continue;
                    }

                    for (var declaring = type; declaring != null; declaring = declaring.BaseType) {
                        foreach (var field in declaring.GetFields(InstanceFields)) {
                            if (IsTickType(field.FieldType)) {
                                offenders.Add(type.FullName + "." + field.Name + " (" + field.FieldType.Name + ")");
                            }
                        }
                    }
                }
            }

            Assert.IsEmpty(offenders, "Saved types hold durations in seconds: " + string.Join(", ", offenders));
        }

        private static bool IsTickType(Type type) =>
                type == typeof(Tick) || type == typeof(TickSpan) || type == typeof(TickRate);
    }
}
