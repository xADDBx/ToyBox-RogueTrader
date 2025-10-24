using ToyBox.Features.SettingsTab.Inspector;
using UnityEngine;

namespace ToyBox.Infrastructure.Inspector;
public static partial class InspectorUI {
    [LocalizedString("ToyBox_Infrastructure_Inspector_InspectorUI_ShowSearchText", "Show Search")]
    private static partial string m_ShowSearchText { get; }
    [LocalizedString("ToyBox_Infrastructure_Inspector_InspectorUI_ShowSearchSettingsText", "Show Settings")]
    private static partial string m_ShowSettingsText { get; }
    [LocalizedString("ToyBox_Infrastructure_Inspector_InspectorUI_SearchByNameText", "Search by Name")]
    private static partial string m_SearchByNameText { get; }
    [LocalizedString("ToyBox_Infrastructure_Inspector_InspectorUI_SearchByTypeText", "Search by Type")]
    private static partial string m_SearchByTypeText { get; }
    [LocalizedString("ToyBox_Infrastructure_Inspector_InspectorUI_SearchByValueText", "Search by Value")]
    private static partial string m_SearchByValueText { get; }
    [LocalizedString("ToyBox_Infrastructure_Inspector_InspectorUI_SearchDepthText", "Search Depth")]
    private static partial string m_SearchDepthText { get; }
    [LocalizedString("ToyBox_Infrastructure_Inspector_InspectorUI_StoppedDrawingEntriesToPreventUI", "Stopped drawing entries to prevent UI crash")]
    private static partial string m_StoppedDrawingEntriesToPreventUI { get; }
    private static GUIStyle m_ButtonStyle {
        get {
            field ??= new(GUI.skin.button) { alignment = TextAnchor.MiddleLeft, stretchHeight = false };
            return field;
        }
    }
    private static readonly Dictionary<object, InspectorNode> m_CurrentlyInspecting = [];
    private static readonly HashSet<object> m_ExpandedKeys = [];
    static InspectorUI() {
        Main.OnHideGUIAction += ClearCache;
    }
    public static void RebuildCurrent() {
        m_CurrentlyInspecting.Clear();
    }
    public static void ClearCache() {
        m_CurrentlyInspecting.Clear();
        m_ExpandedKeys.Clear();
        InspectorSearcher.ShouldCancel = true;
        InspectorSearcher.LastPrompt = "";
    }
    public static void InspectToggle(object key, string? title = null, object? toInspect = null, int indent = 0, bool inspectInline = false, params GUILayoutOption[] options) {
        title ??= key.ToString();
        toInspect ??= key;
        var expanded = m_ExpandedKeys.Contains(key);
        if (inspectInline) {
            using (VerticalScope()) {
                DisclosureToggle(key, title, expanded, options);
                InspectIfExpanded(key, toInspect, indent);
            }
        } else {
            DisclosureToggle(key, title, expanded, options);
        }
    }

    private static void DisclosureToggle(object key, string title, bool expanded, params GUILayoutOption[] options) {
        if (options.Length == 0) {
            options = [Width(UI.DisclosureGlyphWidth.Value)];
        }
        if (UI.DisclosureToggle(ref expanded, title, options)) {
            if (expanded) {
                m_ExpandedKeys.Clear();
                _ = m_ExpandedKeys.Add(key);
            } else {
                _ = m_ExpandedKeys.Remove(key);
            }
        }
    }

    public static void InspectIfExpanded(object key, object? toInspect = null, int indent = 0) {
        if (m_ExpandedKeys.Contains(key)) {
            using (HorizontalScope()) {
                Space(indent);
                Inspect(toInspect ?? key);
            }
        }
    }
    private static string m_NameSearch = "";
    private static string m_TypeSearch = "";
    private static string m_ValueSearch = "";
    private static bool m_DoShowSearch = false;
    private static bool m_DoShowSettings = false;
    private static int m_SearchDepth = 2;
    private static int m_DrawnNodes;
    public static void Inspect(object? obj) {
        using (VerticalScope()) {
            if (obj == null) {
                UI.Label(SharedStrings.CurrentlyInspectingText + ": " + "<null>".Cyan());
            } else {
                var valueText = "";
                try {
                    valueText = obj.ToString();
                } catch (Exception ex) {
                    Warn($"Encountered exception in Inspect -> obj.ToString():\n{ex}");
                }
                using (HorizontalScope()) {
                    UI.Label(SharedStrings.CurrentlyInspectingText + ": " + valueText.Cyan());
                    Space(20);
                    _ = UI.DisclosureToggle(ref m_DoShowSearch, m_ShowSearchText);
                    Space(20);
                    _ = UI.DisclosureToggle(ref m_DoShowSettings, m_ShowSettingsText);
                    if (InspectorSearcher.IsRunning) {
                        Space(20);
                        UI.Label(SharedStrings.SearchInProgresText.Orange());
                        Space(20);
                        _ = UI.Button(SharedStrings.CancelText.Cyan(), () => InspectorSearcher.ShouldCancel = true);
                    }
                }
                if (m_DoShowSettings) {
                    using (VerticalScope()) {
                        Feature.GetInstance<InspectorShowNullAndEmptyMembersSetting>().OnGui();
                        Feature.GetInstance<InspectorShowStaticMembersSetting>().OnGui();
                        Feature.GetInstance<InspectorShowEnumerableFieldsSetting>().OnGui();
                        Feature.GetInstance<InspectorShowCompilerGeneratedFieldsSetting>().OnGui();
                        Feature.GetInstance<InspectorSlimModeSetting>().OnGui();
                        Feature.GetInstance<InspectorDrawLimitSetting>().OnGui();
                        Feature.GetInstance<InspectorIndentWidthSetting>().OnGui();
                        Feature.GetInstance<InspectorNameFractionOfWidthSetting>().OnGui();
                        Feature.GetInstance<InspectorSearcherBatchSizeSetting>().OnGui();
                    }
                }
                if (m_DoShowSearch) {
                    using (HorizontalScope()) {
                        UI.Label(m_SearchDepthText + ": ", Width(200));
                        if (UI.ValueAdjuster(ref m_SearchDepth, 1, 0, 8, false)) {
                            if (InspectorSearcher.DidSearch) {
                                InspectorSearcher.LastPrompt = null;
                            }
                        }
                    }
                }
                if (!m_CurrentlyInspecting.TryGetValue(obj, out var root)) {
                    root = InspectorTraverser.BuildRoot(obj);
                    m_CurrentlyInspecting[obj] = root;
                }

                SearchBarGUI(root);
                m_DrawnNodes = 0;
                foreach (var child in root.Children) {
                    if (!InspectorSearcher.DidSearch || child.IsMatched) {
                        DrawNode(child, 1);
                    }
                }
                if (m_DrawnNodes >= Settings.InspectorDrawLimit) {
                    UI.Label(m_StoppedDrawingEntriesToPreventUI.Red().Bold());
                }
            }
        }
    }
    private static void SearchBarGUI(InspectorNode root) {
        if (m_DoShowSearch) {
            using (HorizontalScope()) {
                UI.Label(m_SearchByNameText + ":", Width(200));
                _ = UI.ActionTextField(ref m_NameSearch, "InspectorNameSearch", null, (string prompt) => {
                    InspectorSearcher.StartSearch(InspectorSearcher.SearchMode.NameSearch, root, m_SearchDepth, prompt);
                }, Width(200), GUILayout.MaxWidth(EffectiveWindowWidth() * 0.3f));
                Space(10);
                _ = UI.Button(SharedStrings.SearchText, () => {
                    InspectorSearcher.StartSearch(InspectorSearcher.SearchMode.NameSearch, root, m_SearchDepth, m_NameSearch);
                });
            }
            using (HorizontalScope()) {
                UI.Label(m_SearchByTypeText + ":", Width(200));
                _ = UI.ActionTextField(ref m_TypeSearch, "InspectorTypeSearch", null, (string prompt) => {
                    InspectorSearcher.StartSearch(InspectorSearcher.SearchMode.TypeSearch, root, m_SearchDepth, prompt);
                }, Width(200), GUILayout.MaxWidth(EffectiveWindowWidth() * 0.3f));
                Space(10);
                _ = UI.Button(SharedStrings.SearchText, () => {
                    InspectorSearcher.StartSearch(InspectorSearcher.SearchMode.TypeSearch, root, m_SearchDepth, m_TypeSearch);
                });
            }
            using (HorizontalScope()) {
                UI.Label(m_SearchByValueText + ":", Width(200));
                _ = UI.ActionTextField(ref m_ValueSearch, "InspectorValueSearch", null, (string prompt) => {
                    InspectorSearcher.StartSearch(InspectorSearcher.SearchMode.ValueSearch, root, m_SearchDepth, prompt);
                }, Width(200), GUILayout.MaxWidth(EffectiveWindowWidth() * 0.3f));
                Space(10);
                _ = UI.Button(SharedStrings.SearchText, () => {
                    InspectorSearcher.StartSearch(InspectorSearcher.SearchMode.ValueSearch, root, m_SearchDepth, m_ValueSearch);
                });
            }
        }
    }
    public static void DrawNode(InspectorNode node, int indent) {
        if (m_DrawnNodes >= Settings.InspectorDrawLimit) {
            return;
        }
        using (HorizontalScope()) {
            GUILayout.Space(indent * Settings.InspectorIndentWidth);
            if (!Settings.ToggleInspectorShowNullAndEmptyMembers && (node.IsNull || node.IsEnumerable && node.Children.Count == 0)) {
                return;
            }

            m_DrawnNodes++;

            var discWidth = UI.DisclosureGlyphWidth.Value;
            var leftOverWidth = EffectiveWindowWidth() /*- (indent * Settings.InspectorIndentWidth)*/ - 40 - discWidth;
            var calculatedWidth = Settings.InspectorNameFractionOfWidth * leftOverWidth;
            if (Settings.ToggleInspectorSlimMode) {
                calculatedWidth = Math.Min(calculatedWidth * leftOverWidth, node.OwnTextLength!.Value);
            }

            if (node.Children.Count > 0) {
                _ = UI.DisclosureToggle(ref node.IsExpanded, node.LabelText, Width(calculatedWidth + discWidth));
            } else {
                Space(discWidth);
                GUILayout.Label(node.LabelText, Width(calculatedWidth));
            }

            if (Settings.ToggleInspectorSlimMode) {
                Space(10);
            }

            // TextArea does not parse color tags; so it needs this workaround to colour text
            var currentColor = GUI.contentColor;
            GUI.contentColor = node.ColorOverride ?? currentColor;
            _ = GUILayout.TextArea(node.ValueText);
            GUI.contentColor = currentColor;
            if (node.AfterText != "") {
                GUILayout.Label(node.AfterText, m_ButtonStyle, AutoWidth());
            } else {
                UI.Label("");
            }
        }
        if (InspectorSearcher.DidSearch) {
            foreach (var child in node.Children) {
                if (node.IsExpanded || child.IsMatched) {
                    DrawNode(child, indent + 1);
                }
            }
        } else {
            if (node.IsExpanded) {
                foreach (var child in node.Children) {
                    DrawNode(child, indent + 1);
                }
            }
        }
    }
}
