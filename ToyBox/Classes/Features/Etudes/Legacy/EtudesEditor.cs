using Kingmaker;
using Kingmaker.AreaLogic.Etudes;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Area;
using Kingmaker.Blueprints.JsonSystem.EditorDatabase;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.Designers.EventConditionActionSystem.Conditions;
using Kingmaker.ElementsSystem;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using ToyBox.Infrastructure.Blueprints.BlueprintActions;
using ToyBox.Infrastructure.Inspector;
using UnityEditor;
using UnityEngine;
using UnityModManagerNet;
using Application = UnityEngine.Application;

namespace ToyBox.Features.Etudes; 
public static class EtudesEditor {

    private static string? _parent;
    private static string _selected;
    private static Dictionary<string, EtudeInfo>? loadedEtudes => EtudesTreeModel.Instance.loadedEtudes;
    private static Dictionary<string, EtudeInfo> _filteredEtudes = new();

    // TODO: is this still the right root etude?
    internal static string rootEtudeId =
            "4f66e8b792ecfad46ae1d9ecfd7ecbc2";
    public static string searchText = "";
    public static string searchTextInput = "";
    private static bool _showOnlyFlagLikes;
    private static bool showComments => Settings.showEtudeComments;

    private static BlueprintArea? _selectedArea;
    private static Dictionary<string, BlueprintArea?> m_NameToAreaDict = [];
    private static Browser<string>? m_AreaBrowser;
    private static string _areaSearchText = "";
    //private EtudeChildrenDrawer etudeChildrenDrawer;

    public static Dictionary<string?, BlueprintEtude> toValues = new();
    public static void OnShowGUI() => UpdateEtudeStates();
    public static int lineNumber = 0;
    public static Rect firstRect;
    private static void Update() {
        //etudeChildrenDrawer?.Update();
    }

    private static void ReloadEtudes() {
        EtudesTreeModel.Instance.ReloadBlueprintsTree();
        //etudeChildrenDrawer = new EtudeChildrenDrawer(loadedEtudes, this);
        //etudeChildrenDrawer.ReferenceGraph = ReferenceGraph.Reload();
        if (loadedEtudes != null) {
            ApplyFilter();
        }
    }

    public static void OnGUI() {
        if (string.IsNullOrEmpty(_parent)) {
            _parent = rootEtudeId;
            _selected = _parent;
        }
        if (loadedEtudes == null) {
            ReloadEtudes();
            return;
        }
        if (m_AreaBrowser == null) {
            BPLoader.GetBlueprintsOfType<BlueprintArea>(bps => {
                Main.ScheduleForMainThread(() => {
                    foreach (var bp in bps) {
                        m_NameToAreaDict[BPHelper.GetTitle(bp)] = bp;
                        m_NameToAreaDict["All"] = null;
                    }
                    m_AreaBrowser = new(s => {
                        if (s == "All") {
                            return "";
                        } else {
                            return s;
                        }
                    }, s => s, m_NameToAreaDict.Keys, overridePageWidth: 300, showDivBetweenItems: false, orderInitialCollection: true);
                });
            });
            return;
        }
        UI.Label("Note".Orange().Bold()
            + " this is a new and exciting feature that allows you to see for the first time the structure and some basic relationships of ".Green()
            + "Etudes".Cyan().Bold()
            + " and other ".Green()
            + "Elements".Cyan().Bold()
            + " that control the progression of your game story. Etudes are hierarchical in structure and additionally contain a set of ".Green()
            + "Elements".Cyan().Bold()
            + " that can both conditions to check and actions to execute when the etude is started. As you browe you will notice there is a disclosure triangle next to the name which will show the children of the Etude.  Etudes that have ".Green()
            + "Elements".Cyan().Bold()
            + " will offer a second disclosure triangle next to the status that will show them to you.".Green());

        UI.Label("WARNING".Yellow().Bold() + " this tool can both miraculously fix your broken progression or it can break it even further. Save and back up your save before using.".Orange());
        using (HorizontalScope(AutoWidth())) {
            UI.Label("Search");
            Space(25);
            UI.ActionTextField(ref searchTextInput, "Search", (s) => { }, (s) => { searchText = s; UpdateSearchResults(); }, Width(400));
            Space(25);
            if (UI.Toggle("Flags Only", null, ref _showOnlyFlagLikes)) ApplyFilter();
            Space(25);
            UI.Toggle("Show GUIDs", null, ref Settings.showAssetIDs);
            Space(25);
            UI.Toggle("Show Comments (some in Russian)", null, ref Settings.showEtudeComments);
            //UI.Label($"Etude Hierarchy : {(loadedEtudes.Count == 0 ? "" : loadedEtudes[parent].Name)}", UI.AutoWidth());
            //UI.Label($"H : {(loadedEtudes.Count == 0 ? "" : loadedEtudes[selected].Name)}");

            //if (loadedEtudes.Count != 0) {
            //    UI.ActionButton("Refresh", () => ReloadEtudes(), UI.AutoWidth());
            //}

            //if (UI.Button("Update DREAMTOOL Index", UI.MinWidth(300), UI.MaxWidth(300))) {
            //    ReferenceGraph.CollectMenu();
            //    etudeChildrenDrawer.ReferenceGraph = ReferenceGraph.Reload();
            //    etudeChildrenDrawer.ReferenceGraph.AnalyzeReferencesInBlueprints();
            //    etudeChildrenDrawer.ReferenceGraph.Save();
            //}
        }
        using (HorizontalScope(GUI.skin.box, AutoWidth())) {
            //if (etudeChildrenDrawer != null) {
            //    using (UI.VerticalScope(GUI.skin.box, UI.MinHeight(60),
            //        UI.MinWidth(300))) {
            //        etudeChildrenDrawer.DefaultExpandedNodeWidth = UI.Slider("Ширина раскрытия нода", etudeChildrenDrawer.DefaultExpandedNodeWidth, 200, 2000);
            //    }
            //}

            //if (etudeChildrenDrawer != null && !etudeChildrenDrawer.BlockersInfo.IsEmpty) {
            //    using (UI.VerticalScope(GUI.skin.box, UI.MinHeight(60),
            //        UI.MinWidth(350))) {
            //        var info = etudeChildrenDrawer.BlockersInfo;
            //        var lockSelf = info.Blockers.Contains(info.Owner);
            //        if (lockSelf) {
            //            UI.Label("Completion блокируется условиями самого этюда");
            //        }

            //        if (info.Blockers.Count > 1 || !lockSelf) {
            //            UI.Label("Completion блокируется условиями детей: ");
            //            foreach (var blocker in info.Blockers) {
            //                var bluprint = blocker.Blueprint;
            //                if (UI.Button(bluprint.name)) {
            //                    Selection.activeObject = BlueprintEditorWrapper.Wrap(bluprint);
            //                }
            //            }
            //        }
            //    }
            //}
        }
        var remainingWidth = EffectiveWindowWidth();
        using (HorizontalScope()) {
            UI.Label(""); firstRect = GUILayoutUtility.GetLastRect();
            using (VerticalScope(GUI.skin.box)) {
                m_AreaBrowser.OnGUI(area => {
                    if (_selectedArea == m_NameToAreaDict[area]) {
                        GUILayout.Toggle(true, area.Orange(), UI.LeftAlignedButtonStyle);
                    } else {
                        if (GUILayout.Toggle(false, area.Cyan(), UI.LeftAlignedButtonStyle)) {
                            _selectedArea = m_NameToAreaDict[area];
                            ApplyFilter();
                        }
                    }
                });
            }
            remainingWidth -= 300;
            using (VerticalScope(GUI.skin.box)) { //, UI.Width(remainingWidth))) {
                //using (var scope = UI.ScrollViewScope(m_ScrollPos, GUI.skin.box)) {
                //UI.Label($"Hierarchy tree : {(loadedEtudes.Count == 0 ? "" : loadedEtudes[parent].Name)}", UI.MinHeight(50));

                if (_filteredEtudes.Count == 0) {
                    UI.Label("No Etudes");
                    //UI.ActionButton("Refresh", () => ReloadEtudes(), UI.AutoWidth());
                    return;
                }

                if (Application.isPlaying) {
                    foreach (var etude in Game.Instance.Player.EtudesSystem.Etudes.RawFacts) {
                        FillPlaymodeEtudeData(etude);
                    }
                }
                lineNumber = 0;
                ShowBlueprintsTree();

                //m_ScrollPos = scope.scrollPosition;
            }

            //using (UI.VerticalScope(GUI.skin.box, UI.ExpandWidth(true), UI.ExpandHeight(true))) {
            //    UI.Label("", UI.ExpandWidth(true), UI.ExpandHeight(true));

            //if (Event.current.type == EventType.Repaint) {
            //    workspaceRect = GUILayoutUtility.GetLastRect();
            //    etudeChildrenDrawer?.SetWorkspaceRect(workspaceRect);
            //}
            //etudeChildrenDrawer.OnGUI();
            //}
        }
    }

    private static HashSet<string> enclosingEtudes = new();
    private static void DrawEtude(string etudeID, EtudeInfo etude, int indent) {
        if (enclosingEtudes.Contains(etudeID)) return;
        var viewPort = UnityModManager.UI.Instance.mWindowRect;
        var topLines = firstRect.y / 30;
        var linesVisible = 1 + viewPort.height / 30;
        var scrollOffset = UnityModManager.UI.Instance.mScrollPosition[0].y / 30 - topLines;
        var viewPortLine = lineNumber - scrollOffset;
        var isVisible = viewPortLine >= 0 && viewPortLine < linesVisible;
#if false
        Mod.Log($"line: {lineNumber} - topLines: {topLines} scrollOffset: {scrollOffset} - {Event.current.type} - isVisible: {isVisible}");
#endif
        if (true || isVisible) {
            var name = etude.Name ?? "<Null>";
            if (etude.hasSearchResults || searchText.Length == 0 || name.ToLower().Contains(searchText.ToLower())) {
                enclosingEtudes.Add(etudeID);
                var components = etude.Blueprint.Components;
                //var gameActions = etude.Blueprint.ComponentsArray.SelectMany(c => {
                //    var actionsField = c.GetType().GetField("Actions");
                //    var actionList = (ActionList)actionsField?.GetValue(c);
                //    return actionList?.Actions ?? new GameAction[] { };
                //}).ToList();
                var conflicts = EtudesTreeModel.Instance.GetConflictingEtudes(etudeID);
                var conflictCount = conflicts.Count - 1;
                using (HorizontalScope(GUILayout.ExpandWidth(true))) {
                    using (HorizontalScope(Width(310))) {
                        foreach (var action in BlueprintActionFeature.GetActionsForBlueprintType<BlueprintEtude>()) {
                            if (action.OnGui(etude.Blueprint, false) ?? false) {
                                UpdateEtudeStates();
                            }
                        }
                    }
                    Space(indent * 75);
                    var style = GUIStyle.none;
                    style.fontStyle = FontStyle.Normal;
                    if (_selected == etudeID) name = name.Orange().Bold();

                    using (HorizontalScope(Width(825))) {
                        if (etude.ChildrenId.Count == 0) etude.ShowChildren = false;
                        if (UI.DisclosureToggle(ref etude.ShowChildren, name.Orange().Bold())) {
                            OpenCloseAllChildren(etude, etude.ShowChildren);
                        }
                        Space(25);
                        var eltCount = etude.Blueprint.m_AllElements.Count;
                        if (eltCount > 0)
                            UI.DisclosureToggle(ref etude.ShowElements, eltCount.ToString() + " " + "elements");
                        else
                            Space(178);
                        //UI.Space(126);
                        if (conflictCount > 0)
                            UI.DisclosureToggle(ref etude.ShowConflicts, conflictCount.ToString() + " " + "conflicts");
                        else
                            Space(178);

                        //UI.Space(126);
                        //if (gameActions.Count > 0)
                        //    UI.ToggleButton(ref etude.ShowActions, $"{gameActions.Count} actions", UI.Width(75));
                        //else
                        //    UI.Space(78);
                    }
                    //UI.ActionButton(UI.DisclosureGlyphOff + ">", () => OpenCloseAllChildren(etudeEntry, !etudeEntry.Foldout), GUI.skin.box, UI.AutoWidth());
                    //if (GUILayout.Button("Select", GUI.skin.box, UI.Width(100))) {
                    //    if (selected != etudeID) {
                    //        selected = etudeID;
                    //    }
                    //    else {
                    //        parent = etudeID;
                    //        //etudeChildrenDrawer.SetParent(parent, workspaceRect);
                    //    }
                    //    selectedEtude = ResourcesLibrary.TryGetBlueprint<BlueprintEtude>(etudeID);
                    //}

                    Space(100);
                    UI.Label(etude.State.ToString().Yellow(), Width(80));
                    Space(-2);
                    Space(25);
                    if (EtudeValidationProblem(etudeID, etude) is { } reason) {
                        UI.Label(reason.Cyan().Yellow(), Width(300));
                        Space(25);
                    }
                    UI.Label("🔗", AutoWidth());
                    if (etude.CompleteParent)
                        UI.Label("Completes Parent", AutoWidth());
                    if (etude.AllowActionStart) {
                        Space(25);
                        UI.Label("Can Start", Width(100));
                    }
                    InspectorUI.InspectToggle(etude, "Inspect", options:Width(100));
                    if (Settings.showAssetIDs) {
                        var guid = etudeID.ToString();
                        UI.TextField(ref guid);
                    }
                    if (showComments && !Settings.showAssetIDs && !string.IsNullOrEmpty(etude.Comment)) {
                        UI.Label(etude.Comment.Green(), GUILayout.ExpandWidth(true));
                    }
                    UI.Label("");
                }
                InspectorUI.InspectIfExpanded(etude);
                if (showComments && Settings.showAssetIDs && !string.IsNullOrEmpty(etude.Comment)) {
                    Space(-15);
                    using (HorizontalScope(Width(200))) {
                        Space(310);
                        Space(indent * 75);
                        Space(933);
                        UI.Label(etude.Comment.Green(), GUILayout.ExpandWidth(true));
                        UI.Label("");
                    }
                }
                indent += 2;
                if (etude.ShowElements) {
                    using (HorizontalScope(GUILayout.ExpandWidth(true))) {
                        Space(310);
                        Space(indent * 75);
                        using (VerticalScope()) {
                            foreach (var element in etude.Blueprint.m_AllElements) {
                                using (HorizontalScope(Width(10000))) {
                                    // UI.Label(element.NameSafe().orange()); -- this is useless at the moment
                                    using (HorizontalScope(450)) {
                                        if (element is GameAction gameAction) {
                                            try {
                                                UI.Button(gameAction.GetCaption().Yellow(), gameAction.RunAction);
                                            } catch (Exception e) {
                                                Warn($"{gameAction.GetCaption()} failed to run {e}");
                                            }
                                        } else
                                            UI.Label(element.GetCaption()?.Yellow() ?? "?");
                                        Space(25);
                                        InspectorUI.InspectToggle(element, "Inspect", options: Width(100));
                                        Space(0);
                                    }
                                    Space(25);
                                    if (element is Condition condition)
                                        UI.Label($"{element.GetType().Name.Cyan()} : {condition.CheckCondition().ToString().Orange()}", Width(250));
                                    else if (element is Conditional conditional)
                                        UI.Label($"{element.GetType().Name.Cyan()} : {conditional.ConditionsChecker.Check().ToString().Orange()} - {string.Join(", ", (IEnumerable<string>)conditional.ConditionsChecker.Conditions.Select(c => c.GetCaption())).Yellow()}", Width(250));
                                    else
                                        UI.Label(element.GetType().Name.Cyan(), Width(250));
                                    if (element is AnotherEtudeOfGroupIsPlaying otherGroup)
                                        UI.Label($"{conflictCount}", Width(50));
                                    else
                                        Width(53);
                                    Space(25);
                                    if (showComments)
                                        UI.Label(element.GetDescription().Green());

                                }
                                if (element is StartEtude started) {
                                    DrawEtudeTree(started.Etude.Guid, 2, true);
                                }
                                if (element is EtudeStatus status) {
                                    DrawEtudeTree(status.m_Etude.Guid, 2, true);
                                }
                                if (element is CompleteEtude completed) {
                                    DrawEtudeTree(completed.Etude.Guid, 2, true);
                                }
                                Div.DrawDiv();
                            }
                        }
                    }
                }

                if (etude.ShowConflicts) {
                    using (HorizontalScope(Width(10000))) {
                        Space(310);
                        Space(indent * 75);
                        using (VerticalScope()) {
                            foreach (var conflict in conflicts) {
                                DrawEtudeTree(conflict, 2, true);
                            }
                        }
                    }
                }
                //if (etude.ShowActions.IsOn()) {
                //    foreach (var action in gameActions) {
                //        using (UI.HorizontalScope()) {
                //            UI.Space(310);
                //            UI.Indent(indent);
                //            UI.ActionButton(action.GetCaption(), action.RunAction);
                //            UI.Space(25);
                //            UI.Label(action.GetDescription().green());
                //        }
                //    }
                //}
                lineNumber += 1;
            }
            enclosingEtudes.Remove(etudeID);
        }
    }
    private static void ShowBlueprintsTree() {
        using (VerticalScope()) {
            DrawEtude(rootEtudeId, loadedEtudes[rootEtudeId], 0);
            using (VerticalScope(GUI.skin.box)) {
                ShowParentTree(loadedEtudes[rootEtudeId], 1);
            }
        }
    }

    private static void DrawEtudeTree(string etudeID, int indent, bool ignoreFilter = false) {
        var etude = loadedEtudes[etudeID];
        DrawEtude(etudeID, etude, indent);

        if (etude.ChildrenId.Count > 0 && (etude.ShowChildren || etude.hasSearchResults)) {
            ShowParentTree(etude, indent + 1, ignoreFilter);
        }
    }
    private static void ShowParentTree(EtudeInfo etude, int indent, bool ignoreFilter = false) {
        foreach (var childID in etude.ChildrenId) {
            if (!ignoreFilter && !_filteredEtudes.ContainsKey(childID))
                continue;
            DrawEtudeTree(childID, indent, ignoreFilter);
        }
    }
    private static void UpdateSearchResults() {
        foreach (var entry in loadedEtudes)
            entry.Value.hasSearchResults = false;
        if (searchText.Length != 0) {
            foreach (var entry in loadedEtudes) {
                var etude = entry.Value;
                if (MatchString(etude.Name ?? "", searchText)
                    || MatchString(etude.Blueprint.AssetGuid.ToString(), searchText)) {
                    etude.hasSearchResults = true;
                    etude.TraverseParents(e => e.hasSearchResults = true);
                }
            }
        }
    }
    private static void ApplyFilter() {
        UpdateSearchResults();
        var etudesOfArea = new Dictionary<string, EtudeInfo>();

        _filteredEtudes = loadedEtudes;

        if (_selectedArea != null) {
            etudesOfArea = GetAreaEtudes();
            _filteredEtudes = etudesOfArea;
        }

        var flaglikeEtudes = new Dictionary<string, EtudeInfo>();

        if (_showOnlyFlagLikes) {
            flaglikeEtudes = GetFlaglikeEtudes();
            _filteredEtudes = _filteredEtudes.Keys.Intersect(flaglikeEtudes.Keys)
                .ToDictionary(t => t, t => _filteredEtudes[t]);
        }
    }

    //[MenuItem("CONTEXT/BlueprintEtude/Open in EtudeViewer")]
    //public static void OpenAssetInEtudeViewer() {
    //    BlueprintEtude blueprint = BlueprintEditorWrapper.Unwrap<BlueprintEtude>(Selection.activeObject);
    //    if (blueprint == null)
    //        return;

    //    EtudeChildrenDrawer.TryToSetParent(blueprint.AssetGuid);

    //}

    private static Dictionary<string, EtudeInfo> GetFlaglikeEtudes() {
        var etudesFlaglike = new Dictionary<string, EtudeInfo>();

        foreach (var etude in loadedEtudes) {
            var flaglike = etude.Value.ChainedTo == string.Empty &&
                            // (etude.Value.ChainedId.Count == 0) &&
                            etude.Value.LinkedTo == string.Empty &&
                            etude.Value.LinkedArea == string.Empty && !ParentHasArea(etude.Value);

            if (flaglike) {
                etudesFlaglike.Add(etude.Key, etude.Value);
                AddParentsToDictionary(etudesFlaglike, etude.Value);
            }
        }

        return etudesFlaglike;
    }

    public static bool ParentHasArea(EtudeInfo etude) {
        if (etude.ParentId == string.Empty)
            return false;

        if (loadedEtudes[etude.ParentId].LinkedArea == string.Empty) {
            return ParentHasArea(loadedEtudes[etude.ParentId]);
        }

        return true;
    }

    private static Dictionary<string, EtudeInfo> GetAreaEtudes() {
        var etudesWithAreaLink = new Dictionary<string, EtudeInfo>();

        foreach (var etude in loadedEtudes) {
            if (etude.Value.LinkedArea == _selectedArea.AssetGuid) {
                if (!etudesWithAreaLink.ContainsKey(etude.Key))
                    etudesWithAreaLink.Add(etude.Key, etude.Value);

                AddChildsToDictionary(etudesWithAreaLink, etude.Value);
                AddParentsToDictionary(etudesWithAreaLink, etude.Value);

            }
        }

        return etudesWithAreaLink;
    }

    private static void AddChildsToDictionary(Dictionary<string, EtudeInfo> dictionary, EtudeInfo etude) {
        foreach (var children in etude.ChildrenId) {
            if (dictionary.ContainsKey(children))
                continue;

            dictionary.Add(children, loadedEtudes[children]);
            AddChildsToDictionary(dictionary, loadedEtudes[children]);
        }
    }

    private static void AddParentsToDictionary(Dictionary<string, EtudeInfo> dictionary, EtudeInfo etude) {
        if (etude.ParentId == string.Empty)
            return;

        if (dictionary.ContainsKey(etude.ParentId))
            return;

        dictionary.Add(etude.ParentId, loadedEtudes[etude.ParentId]);
        AddParentsToDictionary(dictionary, loadedEtudes[etude.ParentId]);
    }

    private static void FillPlaymodeEtudeData(Etude etude) {
        var etudeIdReferences = loadedEtudes[etude.Blueprint.AssetGuid];
        UpdateStateInRef(etude, etudeIdReferences);
    }

    private static void UpdateStateInRef(Etude etude, EtudeInfo etudeIdReferences) {
        if (etude.IsCompleted) {
            etudeIdReferences.State = EtudeInfo.EtudeState.Completed;
            return;
        }

        if (etude.CompletionInProgress) {
            etudeIdReferences.State = EtudeInfo.EtudeState.CompletionBlocked;
            return;
        }

        if (etude.IsPlaying) {
            etudeIdReferences.State = EtudeInfo.EtudeState.Active;
        } else {
            etudeIdReferences.State = EtudeInfo.EtudeState.Started;
        }
    }

    // Not localizing this beacuse I doubt doing that is meaningful in any way.
    private static string EtudeValidationProblem(string etudeID, EtudeInfo etude) {
        if (etude.ChainedTo == string.Empty && etude.LinkedTo == string.Empty)
            return "Chained/Linked to Nothing";

        foreach (var chained in etude.ChainedId) {
            var chainedEtude = loadedEtudes[chained];
            if (chainedEtude.ParentId != etude.ParentId)
                return $"Chained etude {chainedEtude.Name} ({chainedEtude.Blueprint.AssetGuid}) has different parent: {chainedEtude.ParentId} than {etude.Name} parent: {etude.ParentId}";
        }

        foreach (var linked in etude.LinkedId) {
            var linkedEtude = loadedEtudes[linked];
            if (linkedEtude.ParentId != etude.ParentId && loadedEtudes[linked].ParentId != etudeID)
                return $"Linked to child {linkedEtude.Name} ({linkedEtude.Blueprint.AssetGuid}) with different parent: {linkedEtude.ParentId} than {etude.Name} parent {etude.ParentId}";
        }

        return null;
    }
    private static void UpdateEtudeStates() {
        if (Application.isPlaying) {
            foreach (var etude in loadedEtudes)
                UpdateEtudeState(etude.Key, etude.Value);
        }
    }
    public static void UpdateEtudeState(string etudeID, EtudeInfo etude) {
        var blueprintEtude = (BlueprintEtude)ResourcesLibrary.TryGetBlueprint(etudeID);

        var item = Game.Instance.Player.EtudesSystem.Etudes.Get(blueprintEtude);
        if (item != null)
            UpdateStateInRef(item, etude);
        else if (Game.Instance.Player.EtudesSystem.EtudeIsPreCompleted(blueprintEtude))
            etude.State = EtudeInfo.EtudeState.CompleteBeforeActive;
        else if (Game.Instance.Player.EtudesSystem.EtudeIsCompleted(blueprintEtude))
            etude.State = EtudeInfo.EtudeState.Completed;
    }
    private static void Traverse(this EtudeInfo etude, Action<EtudeInfo> action) {
        action(etude);
        foreach (var cildrenID in etude.ChildrenId) {
            Traverse(loadedEtudes[cildrenID], action);
        }
    }
    private static void TraverseParents(this EtudeInfo etude, Action<EtudeInfo> action) {
        while (loadedEtudes.TryGetValue(etude.ParentId, out var parent)) {
            action(parent);
            etude = parent;
        }
    }
    private static void OpenCloseAllChildren(this EtudeInfo etude, bool state)
        => etude.Traverse((e) => e.ShowChildren = state);
    private static void OpenCloseParents(this EtudeInfo etude, bool state)
        => etude.TraverseParents((e) => e.ShowChildren = state);
}
