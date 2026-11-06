// Copyright (c) Spellbound Studios. All rights reserved.

using System;

namespace Spellbound.Core.Authoring.Editor {
    public sealed class ValidationScope {
        public static ValidationScope Everything { get; } = new ValidationScope(null, DateTime.MinValue);

        public BannerSchedule Schedule { get; }
        public DateTime ChangedAt { get; }
        public bool IsEditing => Schedule != null;

        private ValidationScope(BannerSchedule schedule, DateTime changedAt) {
            Schedule = schedule;
            ChangedAt = changedAt;
        }

        public static ValidationScope Editing(BannerSchedule schedule, DateTime changedAt) {
            if (schedule == null) {
                throw new ArgumentNullException(nameof(schedule));
            }

            return new ValidationScope(schedule, changedAt);
        }

        public bool Includes(DefinitionCheckDescription check) {
            return check != null && (!IsEditing || (check.RunsWhileEditing && !Schedule.IsDemoted(check.Label)));
        }
    }
}
