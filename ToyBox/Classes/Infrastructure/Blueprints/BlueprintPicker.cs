using Kingmaker.Blueprints;
using ToyBox.Infrastructure.Inspector;
using ToyBox.Infrastructure.Utilities;
using UnityEngine;

namespace ToyBox.Infrastructure;

public static class BlueprintPicker<T> where T : SimpleBlueprint {
    private static string m_CurrentlyTyped = "";
    private static bool m_EnteredInvalidGuid = false;
    private static bool m_ShowBrowser = false;
    private static Browser<T>? m_Browser;
    private static WeakReference<T>? m_CurrentBlueprint;
    // This is a TimedCache and not Lazy for the case where the user changes their UI scale
    private static readonly TimedCache<float> m_ButtonWidth = new(() => CalculateLargestLabelSize([SharedStrings.PickBlueprintText], GUI.skin.button));
    private static float m_CachedTitleWidth;
    private static float m_CachedTypeWidth;
    private static float m_CachedAssetIdWidth;
    public static T? CurrentBlueprint {
        get {
            if (m_CurrentBlueprint is not null && m_CurrentBlueprint.TryGetTarget(out var bp)) {
                return bp;
            } else {
                return null;
            }
        }
    }
    public static bool OnPickerGUI() {
        var didChange = false;
        using (HorizontalScope()) {
            Space(20);
            using (VerticalScope()) {
                _ = UI.DisclosureToggle(ref m_ShowBrowser, SharedStrings.ShowListOfBlueprintsText);
                if (m_ShowBrowser) {
                    if (m_Browser == null) {
                        var bps = BPLoader.GetBlueprintsOfType<T>();
                        if (bps != null) {
                            Main.ScheduleForMainThread(() => {
                                m_Browser = new(BPHelper.GetSortKey, BPHelper.GetSearchKey, bps, null, true, (int)(0.9f * EffectiveWindowWidth()));
                            });
                        }
                    } else {
                        if (!m_Browser.IsCachedValid && m_Browser.PagedItems.Count > 0) {
                            m_CachedTitleWidth = Math.Min(0.3f * EffectiveWindowWidth(), CalculateLargestLabelSize(m_Browser.PagedItems.Select(bp => BPHelper.GetTitle(bp).Cyan().Bold())));
                            m_CachedTypeWidth = Math.Min(0.2f * EffectiveWindowWidth(), CalculateLargestLabelSize(m_Browser.PagedItems.Select(bp => bp.GetType().Name.Grey())));
                            m_CachedAssetIdWidth = Math.Min(0.3f * EffectiveWindowWidth(), CalculateLargestLabelSize(m_Browser.PagedItems.Select(bp => bp.AssetGuid.ToString()), GUI.skin.textField));
                            m_Browser.SetCacheValid();
                        }
                        m_Browser.OnGUI(bp => {
                            Space(10);
                            using (VerticalScope()) {
                                using (HorizontalScope(EffectiveWindowWidth() * 0.9f)) {
                                    string title;
                                    if (bp != CurrentBlueprint) {
                                        _ = UI.Button(SharedStrings.PickBlueprintText, () => {
                                            m_CurrentBlueprint = new(bp);
                                            didChange = true;
                                        }, null, Width(m_ButtonWidth));
                                        title = BPHelper.GetTitle(bp).Cyan().Bold();
                                    } else {
                                        Space(m_ButtonWidth + GUI.skin.button.margin.horizontal);
                                        title = BPHelper.GetTitle(bp).Orange().Bold();
                                    }
                                    InspectorUI.InspectToggle(bp, title, null, 0, false, Width(m_CachedTitleWidth + UI.DisclosureGlyphWidth.Value));
                                    Space(5);
                                    UI.Label(bp.GetType().Name.Grey(), Width(m_CachedTypeWidth));
                                    Space(5);
                                    var tmp = bp.AssetGuid.ToString();
                                    _ = UI.TextField(ref tmp, null, Width(m_CachedAssetIdWidth));
                                    Space(5);
                                    var desc = BPHelper.GetDescription(bp);
                                    if (!string.IsNullOrWhiteSpace(desc)) {
                                        UI.Label(desc!.Green());
                                    }
                                }
                                InspectorUI.InspectIfExpanded(bp, null, (int)(m_ButtonWidth + UI.DisclosureGlyphWidth.Value));
                            }
                        });
                    }
                } else {
                    using (HorizontalScope()) {
                        UI.Label(SharedStrings.EnterTargetBlueprintIdText, Width(200));
                        var before = m_CurrentlyTyped;
                        _ = UI.TextField(ref m_CurrentlyTyped, null, Width(350));
                        if (before != m_CurrentlyTyped) {
                            m_EnteredInvalidGuid = false;
                        }
                        _ = UI.Button(SharedStrings.PickBlueprintText, () => {
                            if (ResourcesLibrary.TryGetBlueprint(m_CurrentlyTyped) is T maybeBP) {
                                m_CurrentBlueprint = new(maybeBP);
                                didChange = true;
                            } else {
                                m_EnteredInvalidGuid = true;
                            }
                        });
                        if (m_EnteredInvalidGuid) {
                            Space(20);
                            UI.Label(SharedStrings.NoBlueprintWithThatGuidFound.Yellow(), Width(300));
                        }
                    }
                }
            }
        }
        if (didChange) {
            m_ShowBrowser = false;
        }
        return didChange;
    }
}
