// Copyright (c) Spellbound Studios. All rights reserved.

using System.Collections.Generic;
using System.Linq;
using Spellbound.Core.Authoring;
using Spellbound.Core.Authoring.Editor;
using Spellbound.Core.Controls.Editor;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Spellbound.Core.Packages.Editor {
    [CustomEditor(typeof(PackageIdentity))]
    public sealed class PackageIdentityInspector : UnityEditor.Editor {
        public const string ClassName = "sb-package-identity";
        public const string JudgmentClassName = "sb-judgment";
        public const string JudgmentTooltip = "A code owner reviews every change to this field.";
        public const string PackageCardName = "spellbound-identity-package";
        public const string MustNotOwnCardName = "spellbound-identity-must-not-own";
        public const string CheckedCardName = "spellbound-identity-checked";
        public const string SkeletonCardName = "spellbound-identity-skeleton";
        public const string ReportName = "spellbound-identity-report";
        public const string PackageCardTitle = "What the package is";
        public const string MustNotOwnCardTitle = "What it must not own";
        public const string CheckedCardTitle = "How it is checked";
        public const string SkeletonCardTitle = "Skeleton";

        private const float CardSpacing = 4f;

        public override VisualElement CreateInspectorGUI() {
            var identity = (PackageIdentity)target;
            var checks = new[] { PackageLabel.OwnValidation(typeof(PackageIdentity)) };
            var header = new HeaderBar(TitleOf(identity));
            var kind = ChoiceFor(nameof(PackageIdentity.kind), PackageIdentity.Kinds);
            var layer = ChoiceFor(nameof(PackageIdentity.layer), PackageIdentity.Layers);
            var status = ChoiceFor(nameof(PackageIdentity.status), PackageIdentity.Statuses);
            var report = new ReportView { name = ReportName };
            report.style.marginTop = CardSpacing;
            var root = new VisualElement();
            root.AddToClassList(ClassName);
            root.Add(header);
            root.Add(Card(PackageCardName, PackageCardTitle, TextFor(nameof(PackageIdentity.job), true), kind, layer,
                status, TextFor(nameof(PackageIdentity.frontDoor), false)));
            root.Add(Card(MustNotOwnCardName, MustNotOwnCardTitle, ListFor(nameof(PackageIdentity.mustNotOwn))));
            root.Add(Card(CheckedCardName, CheckedCardTitle, ToggleFor(nameof(PackageIdentity.hotPath)),
                ToggleFor(nameof(PackageIdentity.deterministicOutput)), ListFor(nameof(PackageIdentity.simulation)),
                ListFor(nameof(PackageIdentity.machineMatchedJobs))));
            root.Add(Card(SkeletonCardName, SkeletonCardTitle, IntegerFor(nameof(PackageIdentity.skeletonVersion))));
            root.Add(report);

            void Refresh() {
                if (identity == null) {
                    return;
                }

                header.Title = TitleOf(identity);
                ShowChoices(kind, PackageIdentity.Kinds, identity.kind);
                ShowChoices(layer, PackageIdentity.Layers, identity.layer);
                ShowChoices(status, PackageIdentity.Statuses, identity.status);
                var validation = new ValidationReport(SubjectOf(identity));
                identity.Validate(validation);
                report.Bind(validation, checks);
            }

            void RefreshAfter(IEventHandler changed) {
                if (changed is IBindable field && !string.IsNullOrEmpty(field.bindingPath)) {
                    Refresh();
                }
            }

            Refresh();
            root.RegisterCallback<ChangeEvent<string>>(change => RefreshAfter(change.target));
            root.RegisterCallback<ChangeEvent<bool>>(change => RefreshAfter(change.target));
            root.RegisterCallback<ChangeEvent<int>>(change => RefreshAfter(change.target));
            root.TrackSerializedObjectValue(serializedObject, _ => Refresh());
            return root;
        }

        private TextField TextFor(string field, bool multiline) {
            var text = new TextField(LabelOf(field)) { bindingPath = field, multiline = multiline };
            text.SetValueWithoutNotify(serializedObject.FindProperty(field).stringValue);

            if (multiline) {
                text.style.whiteSpace = WhiteSpace.Normal;
            }

            return Marked(text, field);
        }

        private DropdownField ChoiceFor(string field, IReadOnlyList<string> known) {
            var current = serializedObject.FindProperty(field).stringValue;
            var choice = new DropdownField(LabelOf(field)) { bindingPath = field };
            ShowChoices(choice, known, current);
            choice.SetValueWithoutNotify(current);
            return Marked(choice, field);
        }

        private Toggle ToggleFor(string field) {
            var toggle = new Toggle(LabelOf(field)) { bindingPath = field };
            toggle.SetValueWithoutNotify(serializedObject.FindProperty(field).boolValue);
            return Marked(toggle, field);
        }

        private IntegerField IntegerFor(string field) {
            var integer = new IntegerField(LabelOf(field)) { bindingPath = field };
            integer.SetValueWithoutNotify(serializedObject.FindProperty(field).intValue);
            return Marked(integer, field);
        }

        private PropertyField ListFor(string field) {
            return Marked(new PropertyField(serializedObject.FindProperty(field), LabelOf(field)), field);
        }

        private static void ShowChoices(DropdownField choice, IReadOnlyList<string> known, string current) {
            var choices = new List<string>(known);

            if (!string.IsNullOrWhiteSpace(current) && !choices.Contains(current)) {
                choices.Add(current);
            }

            if (choice.choices == null || !choice.choices.SequenceEqual(choices)) {
                choice.choices = choices;
            }
        }

        private static T Marked<T>(T element, string field) where T : VisualElement {
            if (PackageIdentity.IsJudgmentField(field)) {
                element.AddToClassList(JudgmentClassName);
                element.tooltip = JudgmentTooltip;
            }

            return element;
        }

        private static string LabelOf(string field) {
            return ObjectNames.NicifyVariableName(field);
        }

        private static string TitleOf(PackageIdentity identity) {
            return string.IsNullOrWhiteSpace(identity.job) ? identity.name : identity.job.Trim();
        }

        private static string SubjectOf(PackageIdentity identity) {
            var path = AssetDatabase.GetAssetPath(identity);
            return string.IsNullOrEmpty(path) ? identity.name : path;
        }

        private static SectionCard Card(string name, string title, params VisualElement[] fields) {
            var card = new SectionCard(title) { name = name };
            card.style.marginTop = CardSpacing;

            foreach (var field in fields) {
                card.Add(field);
            }

            return card;
        }
    }
}
