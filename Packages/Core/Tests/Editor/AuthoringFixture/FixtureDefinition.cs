// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using Spellbound.Core.Authoring;
using UnityEngine;

namespace Spellbound.Core.Tests.AuthoringFixture {
    public class FixtureDefinition : ScriptableObject, IAuthoredDefinition {
        public const string TitleEmptyCode = "fixture.title-empty";
        public const string ThresholdNegativeCode = "fixture.threshold-negative";
        public const string TitleLongCode = "fixture.title-long";

        public string title;
        public int threshold;
        public bool throwOnValidate;

        public void Validate(ValidationReport report) {
            if (throwOnValidate) {
                throw new InvalidOperationException("the fixture was told to throw");
            }

            if (string.IsNullOrWhiteSpace(title)) {
                report.Error(TitleEmptyCode, "the title is empty", nameof(title), "Type a title.");
            }
            else if (title.Length > 20) {
                report.Information(TitleLongCode, "the title has " + title.Length + " characters", nameof(title));
            }

            if (threshold < 0) {
                report.Warning(ThresholdNegativeCode, "the threshold is " + threshold + ", below zero", nameof(threshold));
            }
        }
    }
}
