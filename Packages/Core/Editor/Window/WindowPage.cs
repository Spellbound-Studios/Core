// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using Spellbound.Core.Controls.Editor;
using UnityEngine.UIElements;

namespace Spellbound.Core.Window.Editor {
    public abstract class WindowPage {
        private string _headerSentence = string.Empty;
        private string _headerState = HeaderStates.Neutral;
        private string _footerText = string.Empty;

        public string Title { get; internal set; } = string.Empty;

        public string HeaderSentence {
            get => _headerSentence;
            set {
                _headerSentence = value ?? string.Empty;
                SurfaceChanged?.Invoke(this);
            }
        }

        public string HeaderState {
            get => _headerState;
            set {
                _headerState = HeaderStates.Normalize(value);
                SurfaceChanged?.Invoke(this);
            }
        }

        public string FooterText {
            get => _footerText;
            set {
                _footerText = value ?? string.Empty;
                SurfaceChanged?.Invoke(this);
            }
        }

        public event Action<WindowPage> SurfaceChanged;

        public abstract VisualElement CreateContent();

        public virtual VisualElement CreateHeaderControls() {
            return null;
        }

        public virtual void Shown() { }

        public virtual void Hidden() { }
    }
}
