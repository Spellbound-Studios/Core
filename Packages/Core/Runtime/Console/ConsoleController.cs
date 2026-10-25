// Copyright 2026 Spellbound Studio Inc.

using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Spellbound.Core.Console {
    /// <summary>
    /// The main controller for the developer console.
    /// Handles input configuration, execution, and UI coordination.
    /// Intended to work with UnityEngine.InputSystem and TMPro text objects.
    /// </summary>
    /// <remarks>
    /// If you're having trouble adding this to your game logic, be sure to check the docs where we provide an
    /// example of how you might incorporate this into an MVVM or MVC UI architecture. If you have any feedback on how
    /// to make this better, please feel free to post something in the discord.
    ///
    /// Cheers,
    /// Judsin
    /// </remarks>
    public class ConsoleController : MonoBehaviour {
        [Header("Config"), SerializeField, Tooltip("Configurable value.")]
        private int maxOutputLines = 100;

        [Header("GameObject References"), SerializeField, Tooltip("A text input gameobject.")]
        private TMP_InputField inputField;

        [SerializeField, Tooltip("Where the input text gets displayed.")]
        private TextMeshProUGUI outputText;

        [SerializeField, Tooltip("Enables scrolling capability.")]
        private ScrollRect scrollRect;

        [SerializeField] private RectTransform contentContainer;

        private readonly List<string> _outputHistory = new();
        private readonly List<string> _commandHistory = new();
        private int _commandHistoryIndex = -1;
        private bool _isVisible;

        // Public Helper
        public bool IsVisible => _isVisible;

        /// <summary>
        /// Event fired when console visibility changes.
        /// Example: Subscribe to disable player input when the console opens.
        /// </summary>
        public event Action<bool> OnVisibilityChanged = delegate { };

        #region Unity Lifecycle

        private void Awake() {
            CommandRegistry.Instance.AutoRegisterCommands();

            SetVisibilityImmediate(false);
        }

        private void OnEnable() {
            if (inputField != null)
                inputField.onSubmit.AddListener(OnSubmitInput);

            ConsoleLogger.LinePrinted += LogOutput;
            ConsoleLogger.ErrorPrinted += LogError;
            ConsoleLogger.Cleared += ClearOutput;
        }

        private void OnDisable() {
            if (inputField != null)
                inputField.onSubmit.RemoveListener(OnSubmitInput);

            ConsoleLogger.LinePrinted -= LogOutput;
            ConsoleLogger.ErrorPrinted -= LogError;
            ConsoleLogger.Cleared -= ClearOutput;
        }

        #endregion

        #region Public API

        /// <summary>
        /// Users can subscribe this to some input or button.
        /// Intended to be used by a user that wants to subscribe this to an input.
        /// </summary>
        public void OnTogglePerformed(InputAction.CallbackContext context) => ToggleConsole();

        /// <summary>
        /// Users can subscribe this to some input or button.
        /// Intended to be used by a user that wants to subscribe this to an input.
        /// </summary>
        public void OnHistoryUpPerformed(InputAction.CallbackContext context) => NavigateHistoryUp();

        /// <summary>
        /// Users can subscribe this to some input or button.
        /// Intended to be used by a user that wants to subscribe this to an input.
        /// </summary>
        public void OnHistoryDownPerformed(InputAction.CallbackContext context) => NavigateHistoryDown();

        /// <summary>
        /// Toggle console visibility.
        /// Intended to be called externally.
        /// </summary>
        public void ToggleConsole() => SetVisibility(!_isVisible);

        /// <summary>
        /// Open the console.
        /// Intended to be called externally.
        /// </summary>
        public void OpenConsole() => SetVisibility(true);

        /// <summary>
        /// Close the console.
        /// Intended to be called externally.
        /// </summary>
        public void CloseConsole() => SetVisibility(false);

        /// <summary>
        /// Navigate through the command history by going to the previous command submitted.
        /// Intended to be called externally.
        /// </summary>
        public void NavigateHistoryUp() => NavigateCommandHistory(-1);

        /// <summary>
        /// Navigate through the command history by going to the next command submitted.
        /// Intended to be called externally.
        /// </summary>
        public void NavigateHistoryDown() => NavigateCommandHistory(1);

        /// <summary>
        /// Bypasses the Show and Hide protected virtual methods.
        /// Intended to be called externally.
        /// </summary>
        public void SetVisibilityImmediate(bool visible) {
            _isVisible = visible;
            OnVisibilityChanged.Invoke(visible);
        }

        /// <summary>
        /// Clear the console output.
        /// </summary>
        public void ClearOutput() {
            _outputHistory.Clear();

            if (outputText != null)
                outputText.text = "";
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Set the console active state.
        /// </summary>
        protected virtual void SetVisibility(bool visible) {
            if (_isVisible == visible)
                return;

            _isVisible = visible;

            if (visible) {
                ShowUI();

                if (inputField != null) {
                    inputField.Select();
                    inputField.ActivateInputField();
                }
            }
            else
                HideUI();

            OnVisibilityChanged.Invoke(visible);
        }

        /// <summary>
        /// Override for custom show animations.
        /// </summary>
        protected virtual void ShowUI() {
            if (inputField != null)
                inputField.enabled = true;
        }

        /// <summary>
        /// Override for custom hide animations.
        /// </summary>
        protected virtual void HideUI() {
            if (inputField != null)
                inputField.enabled = false;
        }

        #endregion

        #region Internal Methods No Touchy

        /// <summary>
        /// Called when the user submits input.
        /// </summary>
        private void OnSubmitInput(string input) {
            if (string.IsNullOrWhiteSpace(input)) {
                if (inputField != null)
                    inputField.ActivateInputField();

                return;
            }

            // Echo the command.
            LogInput($"> {input}");

            // Add to the command history.
            _commandHistory.Add(input);
            _commandHistoryIndex = _commandHistory.Count;

            // Execute the command.
            ExecuteCommand(input);

            // Clear and refocus input.
            if (inputField != null) {
                inputField.text = string.Empty;
                inputField.ActivateInputField();
            }

            ScrollToBottom();
        }

        /// <summary>
        /// Execute a command string.
        /// </summary>
        private void ExecuteCommand(string input) {
            try {
                var result = CommandRegistry.Instance.ExecuteCommand(input);

                if (result.Success) {
                    if (!string.IsNullOrEmpty(result.Message))
                        LogOutput(result.Message);
                }
                else
                    LogError(result.Message);
            }
            catch (Exception ex) {
                LogError($"Error executing command: {ex.Message}");
            }
        }

        /// <summary>
        /// Navigate through the command history.
        /// </summary>
        private void NavigateCommandHistory(int direction) {
            if (_commandHistory.Count == 0 || inputField == null)
                return;

            _commandHistoryIndex += direction;
            _commandHistoryIndex = Mathf.Clamp(_commandHistoryIndex, 0, _commandHistory.Count);

            if (_commandHistoryIndex < _commandHistory.Count) {
                inputField.text = _commandHistory[_commandHistoryIndex];
                inputField.caretPosition = inputField.text.Length;
            }
            else
                inputField.text = "";
        }

        /// <summary>
        /// Scroll output to the bottom.
        /// </summary>
        private void ScrollToBottom() {
            if (scrollRect == null || contentContainer == null || outputText == null)
                return;

            var textHeight = outputText.preferredHeight;
            contentContainer.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, textHeight);

            Canvas.ForceUpdateCanvases();

            scrollRect.verticalNormalizedPosition = 0f;
        }

        /// <summary>
        /// Log user input to console.
        /// </summary>
        private void LogInput(string message) => AddOutput($"<color=#00FF00>{message}</color>");

        /// <summary>
        /// Log standard output to the console.
        /// </summary>
        public void LogOutput(string message) => AddOutput(message);

        /// <summary>
        /// Log error to console.
        /// </summary>
        public void LogError(string message) => AddOutput($"<color=#FF4444>{message}</color>");

        /// <summary>
        /// Add text to the output display.
        /// </summary>
        private void AddOutput(string message) {
            _outputHistory.Add(message);

            if (_outputHistory.Count > maxOutputLines)
                _outputHistory.RemoveAt(0);

            if (outputText != null)
                outputText.text = string.Join("\n", _outputHistory);
        }

        #endregion
    }
}