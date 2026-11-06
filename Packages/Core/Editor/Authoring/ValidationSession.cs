// Copyright (c) Spellbound Studios. All rights reserved.

using Spellbound.Core.Observations;
using Spellbound.Core.Operations;

namespace Spellbound.Core.Authoring.Editor {
    internal static class ValidationSession {
        public const string BannerObservationsName = "core.definition-banner";
        public const string DemotionsSessionKey = "Spellbound.Core.Authoring.DemotedChecks";

        private static OperationSettings _settings;
        private static ObservationContext _bannerObservations;

        public static OperationSettings Settings {
            get {
                if (_settings == null) {
                    _settings = OperationSettings.Load();
                }

                return _settings;
            }
        }

        public static ObservationContext BannerObservations =>
            _bannerObservations ??= CoreKit.Observations.CreateContext(BannerObservationsName);
    }
}
