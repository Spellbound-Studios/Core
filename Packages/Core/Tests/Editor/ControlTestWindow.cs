// Copyright (c) Spellbound Studios. All rights reserved.

using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace Spellbound.Core.Tests {
    public sealed class ControlTestWindow : EditorWindow {
        public static ControlTestWindow Open(VisualElement content) {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) {
                Assert.Ignore(
                    "A control needs a panel and a panel needs a graphics device; run this in an editor with one.");
            }

            var window = CreateInstance<ControlTestWindow>();
            window.titleContent = new GUIContent(nameof(ControlTestWindow));
            window.Show();
            window.rootVisualElement.Add(content);
            return window;
        }
    }
}
