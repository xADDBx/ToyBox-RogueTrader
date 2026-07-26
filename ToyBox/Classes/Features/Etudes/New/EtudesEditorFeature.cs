using Kingmaker.AreaLogic.Etudes;
using Kingmaker.Blueprints.Area;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.Designers.EventConditionActionSystem.Conditions;
using Kingmaker.ElementsSystem;
using System.Diagnostics;
using ToyBox.Infrastructure.Blueprints.BlueprintActions;
using ToyBox.Infrastructure.Inspector;
using ToyBox.Infrastructure.Utilities;
using UnityEngine;

namespace ToyBox.Features.Etudes;

public partial class EtudesEditorFeature : Feature {
    [LocalizedString("ToyBox_Features_Etudes_EtudesFeature_Name", "Etudes Editor")]
    public override partial string Name { get; }

    [LocalizedString("ToyBox_Features_Etudes_EtudesFeature_Description", "Browse the game's Etudes in a tree view, with state and relationship details.")]
    public override partial string Description { get; }
    private readonly EtudesTreeModel2 m_Model = new();

    private Browser<string>? m_AreaBrowser;
    private readonly Dictionary<string, BlueprintArea?> m_AreaByName = [];
    private BlueprintArea? m_SelectedArea;
    private static readonly Lazy<float> m_AssetIdWidth = new(() => {
        return CalculateLargestLabelWidth(BPLoader.GetBlueprintsOfType<BlueprintEtude>().Select(bp => bp.AssetGuid.ToString()), GUI.skin.textField);
    });
    private const int m_IndentWidth = 25;

    private string m_SearchText = "";
    private bool m_ShowOnlyFlagLike = false;
    private readonly HashSet<EtudeRecord> m_Enclosing = [];

    public override void Enable() {
        Main.OnLocaleChanged += ClearCaches;
        Main.OnHideGUIAction += ClearCaches;
    }

    public override void Disable() {
        Main.OnLocaleChanged -= ClearCaches;
        Main.OnHideGUIAction -= ClearCaches;
    }

    private void ClearCaches() {
        m_Model.Invalidate();
        m_AreaBrowser = null;
        m_AreaByName.Clear();
        m_SelectedArea = null;
        m_NeedInitAreaBrowserWidth = null;
    }

    public override void OnGui() {
        if (!IsInGame()) {
            UI.Label(SharedStrings.ThisCannotBeUsedFromTheMainMenu.Red().Bold());
            return;
        }

        if (m_Model.ErrorDuringBuild != null) {
            UI.Label(m_EncounteredErrorWhileBuildingEtuLocalizedText.Red().Bold() + $":\n{m_Model.ErrorDuringBuild}");
            return;
        }

        if (!m_Model.TryEnsureSnapshot(out var snapshot) || snapshot == null) {
            UI.Label(m_LoadingText.Orange().Bold());
            return;
        }
        if (!EnsureAreaBrowser()) {
            return;
        }

        using (VerticalScope()) {
            DrawHeader(snapshot);

            using (HorizontalScope()) {
                DrawLeftPane();
                Space(10);
                DrawTreePane(snapshot);
            }
        }
    }

    private void DrawHeader(EtudeSnapshot snapshot) {
        if (snapshot.IsSearching) {
            using (HorizontalScope()) {
                UI.Label(SharedStrings.SearchInProgresText.Orange());
                if (UI.Button(SharedStrings.CancelText.Cyan())) {
                    snapshot.CancelSearch();
                }
            }
        }
        using (HorizontalScope()) {
            UI.Label((m_RootText + ": ").Cyan() + Constants.RootEtudeId.Orange(), Width(310 * Main.UIScale));

            UI.Label(m_SearchTextLabel + ":", Width(200));
            _ = UI.ActionTextField(ref m_SearchText, "EtudesTreeSearch", null, prompt => {
                snapshot.Search(prompt);
            }, Width(200), Width(300 * Main.UIScale));
            Space(10);
            _ = UI.Button(SharedStrings.SearchText, () => {
                snapshot.Search(m_SearchText);
            });

            Space(20);
            _ = UI.Toggle(m_FlagsOnlyText.Cyan(), null, ref m_ShowOnlyFlagLike);

            Space(20);
            _ = UI.Toggle(m_ShowGuidsText.Cyan(), null, ref Settings.ShowAssetIDs);

            Space(20);
            _ = UI.Toggle(m_ShowCommentsText.Cyan(), null, ref Settings.ShowEtudeComments);

            Space(20);
            if (UI.Button(m_RefreshText.Cyan(), () => {
                snapshot.UpdateRuntimeStates();
            })) { }
        }

        if (snapshot.RootEtude == null) {
            UI.Label((m_RootNotFoundText + " ").Red().Bold() + Constants.RootEtudeId.Orange());
        }
    }
    private readonly TimedCache<float> m_AreaWidth = new(() => CalculateLargestLabelWidth(BPLoader.GetBlueprintsOfType<BlueprintArea>().Select(f => f.Name), GUI.skin.button));
    private bool? m_NeedInitAreaBrowserWidth = null;
    private bool EnsureAreaBrowser() {
        if (!m_NeedInitAreaBrowserWidth.HasValue) {
            m_NeedInitAreaBrowserWidth = false;
            _ = BPLoader.GetBlueprintsOfType<BlueprintArea>(bps => {
                Main.ScheduleForMainThread(() => {
                    foreach (var bp in bps) {
                        var title = BPHelper.GetTitle(bp);
                        if (!string.IsNullOrWhiteSpace(title)) {
                            m_AreaByName[title] = bp;
                        }
                    }
                    m_AreaByName[m_AllLocalizedText] = null;
                    m_AreaBrowser = new(sortKey: s => s == m_AllLocalizedText ? "" : s, searchKey: s => s, initialItems: m_AreaByName.Keys, showDivBetweenItems: false, orderInitialCollection: true);
                    m_NeedInitAreaBrowserWidth = true;
                });
            });
        } else if (m_NeedInitAreaBrowserWidth.Value) {
            m_AreaBrowser!.PageWidth = (int)(m_AreaWidth + (10 * Main.UIScale));
            m_NeedInitAreaBrowserWidth = false;
        }
        return m_AreaBrowser != null;
    }

    private void DrawLeftPane() {
        using (VerticalScope(GUI.skin.box, Width(300 * Main.UIScale))) {
            UI.Label(m_AreaFilterText.Cyan().Bold());

            m_AreaBrowser!.OnGUI(areaName => {
                var area = m_AreaByName.TryGetValue(areaName, out var a) ? a : null;
                var selected = m_SelectedArea == area;

                if (selected) {
                    _ = GUILayout.Toggle(true, areaName.Orange(), UI.LeftAlignedButtonStyle);
                } else {
                    if (GUILayout.Toggle(false, areaName.Cyan(), UI.LeftAlignedButtonStyle)) {
                        m_SelectedArea = area;
                    }
                }
            });
        }
    }

    private void DrawTreePane(EtudeSnapshot snapshot) {
        using (VerticalScope(GUI.skin.box, Width(EffectiveWindowWidth() - (320 * Main.UIScale)))) {
            if (snapshot.RootEtude == null) {
                UI.Label(m_NoEtudesText.Red().Bold());
                return;
            }

            // Optimally clearing here should not be necessary
            m_Enclosing.Clear();

            DrawEtudeRecursive(snapshot, snapshot.RootEtude, 0, false);
        }
    }

    private bool PassesFilter(EtudeSnapshot snapshot, EtudeRecord etude) {
        if (m_SelectedArea != null && !etude.IsIndirectlyLinkedToArea(m_SelectedArea.AssetGuid)) {
            return false;
        }

        if (m_ShowOnlyFlagLike && !etude.IsIndirectlyFlagLike()) {
            return false;
        }
        if (snapshot.DidSearch && !etude.IsMatched && !etude.IsDescendantMatched) {
            return false;
        }

        return true;
    }
    private void DrawEtudeRecursive(EtudeSnapshot snapshot, EtudeRecord etude, int indent, bool ignoreFilter) {
        if (m_Enclosing.Contains(etude)) {
            return;
        }
        // Should search ignore filters like area and flags only?
        if (!ignoreFilter && !PassesFilter(snapshot, etude)/* && !(snapshot.DidSearch && etude.IsDescendantMatched)*/) {
            return;
        }

        try {
            _ = m_Enclosing.Add(etude);
            using (HorizontalScope(GUILayout.ExpandWidth(true))) {
                using (HorizontalScope(Width(140 * Main.UIScale))) {
                    foreach (var action in BlueprintActionFeature.GetActionsForBlueprintType<BlueprintEtude>()) {
                        if (action.OnGui(etude.Blueprint, false, default) ?? false) {
                            Main.ScheduleForMainThread(snapshot.UpdateRuntimeStates);
                        }
                    }
                }

                Space(indent * m_IndentWidth);

                var name = (etude.Name ?? "<null>").Orange().Bold();
                var dcWidth = UI.DisclosureGlyphWidth.Value + (5 * Main.UIScale);

                if (etude.Children.Count > 0) {
                    if (UI.DisclosureToggle(ref etude.IsExpanded, name)) {
                        // Legacy behaviour is to uncollapse every children
                        etude.ExpandChildren(etude.IsExpanded);
                    }
                } else {
                    UnscaledSpace(dcWidth);
                    UI.Label(name);
                }

                Space(10);

                UI.Label(etude.State.ToString().Yellow(), AutoWidth());

                Space(10);

                if (etude.CompletesParent) {
                    UI.Label(m_CompletesParentText.Cyan(), AutoWidth());
                    Space(10);
                }

                if (etude.Elements.Count > 0) {
                    _ = UI.DisclosureToggle(ref etude.IsElementsExpanded, (m_ElementsText + $" ({etude.Elements.Count})").Cyan());
                }

                Space(10);
                var conflicts = etude.GetConflictingEtudes(snapshot);
                if (conflicts.Count > 1) {
                    _ = UI.DisclosureToggle(ref etude.IsConflictsExpanded, (m_ConflictsText + $" ({conflicts.Count - 1})").Cyan());
                }

                Space(10);

                InspectorUI.InspectToggle(etude.Blueprint, m_InspectText, options: AutoWidth());

                if (Settings.ShowAssetIDs) {
                    var tmp = etude.Blueprint.AssetGuid.ToString();
                    Space(5);
                    _ = UI.TextField(ref tmp, null, Width(m_AssetIdWidth.Value));
                }

                Space(10);

                var validation = etude.ValidationProblem;
                if (!string.IsNullOrEmpty(validation)) {
                    UI.Label(validation!.Cyan().Yellow(), Width(200 * Main.UIScale));
                }

                Space(10);

                if (Settings.ShowEtudeComments && !string.IsNullOrWhiteSpace(etude.Comment)) {
                    UI.Label(etude.Comment.Green(), GUILayout.ExpandWidth(true));
                }
            }

            InspectorUI.InspectIfExpanded(etude.Blueprint);

            // Expanded details
            if (etude.IsElementsExpanded || etude.IsConflictsExpanded) {
                DrawEtudeDetails(snapshot, etude, indent + 1);
            }

            // Children
            if (etude.IsRoot || etude.IsExpanded || etude.IsDescendantMatched) {
                foreach (var child in etude.Children) {
                    DrawEtudeRecursive(snapshot, child, indent + 1, ignoreFilter);
                }
            }
        } finally {
            _ = m_Enclosing.Remove(etude);
        }
    }

    private void DrawEtudeDetails(EtudeSnapshot snapshot, EtudeRecord etude, int indent) {
        using (HorizontalScope()) {
            Space(indent * m_IndentWidth);
            using (VerticalScope()) {
                if (etude.IsElementsExpanded) {
                    Div.DrawDiv();
                    DrawElements(snapshot, etude, indent);
                }

                if (etude.IsConflictsExpanded) {
                    Div.DrawDiv();
                    foreach (var c in etude.GetConflictingEtudes(snapshot)) {
                        DrawEtudeRecursive(snapshot, c, indent + 1, true);
                    }
                }
            }
        }
    }

    private void DrawElements(EtudeSnapshot snapshot, EtudeRecord e, int indent) {
        foreach (var element in e.Elements) {
            using (HorizontalScope(GUILayout.ExpandWidth(true))) {
                Space((indent + 1) * (int)(m_IndentWidth * Main.UIScale));

                using (HorizontalScope(420 * Main.UIScale)) {
                    if (element is GameAction ga) {
                        if (UI.Button((ga.GetCaption() ?? "?").Yellow())) {
                            try {
                                ga.RunAction();
                                Main.ScheduleForMainThread(snapshot.UpdateRuntimeStates);
                            } catch (Exception ex) {
                                Warn($"Failed to run action {ga.GetCaption()}:\n{ex}");
                            }
                        }
                    } else {
                        UI.Label((element.GetCaption() ?? "?").Yellow());
                    }

                    Space(10);
                    InspectorUI.InspectToggle(element, m_InspectText, options: Width(100 * Main.UIScale));
                }

                Space(10);
                if (element is Condition cond) {
                    UI.Label($"{element.GetType().Name.Cyan()} : {SafeEvaluate(cond.CheckCondition)}", Width(420 * Main.UIScale));
                } else if (element is Conditional conditional) {
                    var caption = string.Join(", ", conditional.ConditionsChecker.Conditions.Select(c => c.GetCaption()));
                    UI.Label($"{element.GetType().Name.Cyan()} : {SafeEvaluate(() => conditional.ConditionsChecker.Check())} - {caption.Yellow()}", Width(420 * Main.UIScale));
                } else {
                    UI.Label(element.GetType().Name.Cyan(), Width(420 * Main.UIScale));
                }

                if (Settings.ShowEtudeComments) {
                    UI.Label(element.GetDescription().Green(), GUILayout.ExpandWidth(true));
                }
            }

            InspectorUI.InspectIfExpanded(element);

            // Cross-links
            if (element is StartEtude started) {
                if (snapshot.EtudesById.TryGetValue(started.Etude.Guid, out var s)) {
                    DrawEtudeRecursive(snapshot, s, indent + 2, true);
                }
            } else if (element is EtudeStatus status) {
                if (snapshot.EtudesById.TryGetValue(status.m_Etude.Guid, out var s)) {
                    DrawEtudeRecursive(snapshot, s, indent + 2, true);
                }
            } else if (element is CompleteEtude completed) {
                if (snapshot.EtudesById.TryGetValue(completed.Etude.Guid, out var c)) {
                    DrawEtudeRecursive(snapshot, c, indent + 2, true);
                }
            }
        }
    }

    private static string SafeEvaluate(Func<bool> check) {
        try {
            return check().ToString().Orange();
        } catch (Exception ex) {
            return $"{m_EvaluationFailedText} ({ex.GetType().Name})".Red();
        }
    }

    [LocalizedString("ToyBox_Features_Etudes_EtudesFeature_EvaluationFailedText", "Evaluation failed")]
    private static partial string m_EvaluationFailedText { get; }

    [LocalizedString("ToyBox_Features_Etudes_EtudesFeature_LoadingText", "Loading Etudes...")]
    private static partial string m_LoadingText { get; }

    [LocalizedString("ToyBox_Features_Etudes_EtudesFeature_RootText", "Root")]
    private static partial string m_RootText { get; }

    [LocalizedString("ToyBox_Features_Etudes_EtudesFeature_RootNotFoundText", "Root etude not found in current snapshot:")]
    private static partial string m_RootNotFoundText { get; }

    [LocalizedString("ToyBox_Features_Etudes_EtudesFeature_SearchTextLabel", "Search")]
    private static partial string m_SearchTextLabel { get; }

    [LocalizedString("ToyBox_Features_Etudes_EtudesFeature_FlagsOnlyText", "Flags Only")]
    private static partial string m_FlagsOnlyText { get; }

    [LocalizedString("ToyBox_Features_Etudes_EtudesFeature_ShowGuidsText", "Show GUIDs")]
    private static partial string m_ShowGuidsText { get; }

    [LocalizedString("ToyBox_Features_Etudes_EtudesFeature_ShowCommentsText", "Show Comments")]
    private static partial string m_ShowCommentsText { get; }

    [LocalizedString("ToyBox_Features_Etudes_EtudesFeature_RefreshText", "Refresh")]
    private static partial string m_RefreshText { get; }

    [LocalizedString("ToyBox_Features_Etudes_EtudesFeature_AreaFilterText", "Area Filter")]
    private static partial string m_AreaFilterText { get; }

    [LocalizedString("ToyBox_Features_Etudes_EtudesFeature_NoEtudesText", "No Etudes")]
    private static partial string m_NoEtudesText { get; }

    [LocalizedString("ToyBox_Features_Etudes_EtudesFeature_CompletesParentText", "Completes Parent")]
    private static partial string m_CompletesParentText { get; }

    [LocalizedString("ToyBox_Features_Etudes_EtudesFeature_InspectText", "Inspect")]
    private static partial string m_InspectText { get; }

    [LocalizedString("ToyBox_Features_Etudes_EtudesFeature_ElementsText", "Elements")]
    private static partial string m_ElementsText { get; }

    [LocalizedString("ToyBox_Features_Etudes_EtudesFeature_ConflictsText", "Conflicts")]
    private static partial string m_ConflictsText { get; }
    [LocalizedString("ToyBox_Features_Etudes_EtudesFeature_m_EncounteredErrorWhileBuildingEtuLocalizedText", "Encountered error while building Etudes Tree!")]
    private static partial string m_EncounteredErrorWhileBuildingEtuLocalizedText { get; }
    [LocalizedString("ToyBox_Features_Etudes_EtudesEditorFeature_m_AllLocalizedText", "All")]
    private static partial string m_AllLocalizedText { get; }
}
