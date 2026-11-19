// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using Spellbound.Core.Logging;

namespace Spellbound.Core.Operations {
    public sealed class ResultRendererRegistry {
        private readonly List<IResultRenderer> _renderers = new List<IResultRenderer>();
        private readonly Func<IEnumerable<Assembly>> _markedAssemblies;
        private bool _scanned;

        public ResultRendererRegistry() : this(null) { }

        public ResultRendererRegistry(Func<IEnumerable<Assembly>> markedAssemblies) {
            _markedAssemblies = markedAssemblies;
        }

        public void Register(IResultRenderer renderer) {
            if (renderer == null) {
                throw new ArgumentNullException(nameof(renderer));
            }

            _renderers.Insert(0, renderer);
        }

        public string Render(object value) {
            EnsureScanned();

            foreach (var renderer in _renderers) {
                if (renderer.TryRender(value, out var text)) {
                    return text ?? string.Empty;
                }
            }

            return RenderDefault(value);
        }

        private static string RenderDefault(object value) {
            switch (value) {
                case null:
                    return string.Empty;
                case string text:
                    return text;
                case OperationTable table:
                    return table.ToText();
                case IFormattable formattable:
                    return formattable.ToString(null, CultureInfo.InvariantCulture);
                case IEnumerable items: {
                    var builder = new StringBuilder();

                    foreach (var item in items) {
                        if (builder.Length > 0) {
                            builder.Append('\n');
                        }

                        builder.Append(RenderDefault(item));
                    }

                    return builder.ToString();
                }
                default:
                    return value.ToString();
            }
        }

        private void EnsureScanned() {
            if (_scanned) {
                return;
            }

            _scanned = true;
            var assemblies = _markedAssemblies != null ? _markedAssemblies() : MarkedAssemblyScan.Assemblies();

            foreach (var type in MarkedAssemblyScan.TypesWith<ResultRendererAttribute>(assemblies)) {
                if (!typeof(IResultRenderer).IsAssignableFrom(type) || type.IsAbstract
                    || type.GetConstructor(Type.EmptyTypes) == null) {
                    Log.Error("Result renderer " + type.FullName + " must implement IResultRenderer with a public parameterless constructor; it was skipped.");
                    continue;
                }

                try {
                    _renderers.Add((IResultRenderer)Activator.CreateInstance(type));
                }
                catch (Exception exception) {
                    Log.Error("Result renderer " + type.FullName + " could not be created: " + exception.Message);
                }
            }
        }
    }
}
