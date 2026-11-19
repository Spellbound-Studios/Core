// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Collections.Generic;
using Spellbound.Core.Logging;
using UnityEditor;
using UnityEngine;

namespace Spellbound.Core.Authoring.Editor {
    public sealed class DefinitionPreviewerRegistry {
        private static DefinitionPreviewerRegistry _default;

        private readonly Func<IEnumerable<Type>> _typeSource;
        private List<DefinitionPreviewer> _previewers;

        public static DefinitionPreviewerRegistry Default => _default ??= new DefinitionPreviewerRegistry(TypeCacheTypes);

        public DefinitionPreviewerRegistry() : this(TypeCacheTypes) { }

        public DefinitionPreviewerRegistry(Func<IEnumerable<Type>> typeSource) {
            _typeSource = typeSource ?? TypeCacheTypes;
        }

        public DefinitionPreviewerRegistry(IEnumerable<DefinitionPreviewer> previewers) {
            _previewers = Sorted(previewers);
        }

        public IReadOnlyList<DefinitionPreviewer> Previewers {
            get {
                EnsureLoaded();
                return _previewers;
            }
        }

        public static IEnumerable<Type> TypeCacheTypes() {
            var types = new List<Type>(TypeCache.GetTypesDerivedFrom<DefinitionPreviewer>());
            types.Sort((left, right) => string.CompareOrdinal(left.FullName, right.FullName));
            return types;
        }

        public static void Reset() {
            _default = null;
        }

        public DefinitionPreviewer For(Type assetType) {
            DefinitionPreviewer best = null;

            foreach (var previewer in Previewers) {
                if (!previewer.AppliesTo(assetType)) {
                    continue;
                }

                var closer = best != null && best.DefinitionType.IsAssignableFrom(previewer.DefinitionType)
                             && best.DefinitionType != previewer.DefinitionType;

                if (best == null || closer) {
                    best = previewer;
                }
            }

            return best;
        }

        public DefinitionPreview Preview(ScriptableObject definition) {
            if (definition == null) {
                throw new ArgumentNullException(nameof(definition));
            }

            var previewer = For(definition.GetType());

            if (previewer != null) {
                return previewer.Preview(definition);
            }

            var typeName = definition.GetType().Name;
            return new DefinitionPreview(definition.name + " (" + typeName + "), no previewer registered for " + typeName);
        }

        private void EnsureLoaded() {
            if (_previewers != null) {
                return;
            }

            var found = new List<DefinitionPreviewer>();

            foreach (var type in _typeSource()) {
                if (type == null || type.IsAbstract || type.ContainsGenericParameters
                    || !typeof(DefinitionPreviewer).IsAssignableFrom(type)) {
                    continue;
                }

                var constructor = type.GetConstructor(Type.EmptyTypes);

                if (constructor == null) {
                    Log.Error("The definition previewer " + type.FullName
                              + " needs a public constructor with no parameters; it was not registered.");
                    continue;
                }

                found.Add((DefinitionPreviewer)constructor.Invoke(null));
            }

            _previewers = Sorted(found);
        }

        private static List<DefinitionPreviewer> Sorted(IEnumerable<DefinitionPreviewer> previewers) {
            var kept = new List<DefinitionPreviewer>();

            if (previewers != null) {
                foreach (var previewer in previewers) {
                    if (previewer != null) {
                        kept.Add(previewer);
                    }
                }
            }

            kept.Sort((left, right) => string.CompareOrdinal(left.GetType().FullName, right.GetType().FullName));
            return kept;
        }
    }
}
