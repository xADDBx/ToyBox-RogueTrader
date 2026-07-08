using Kingmaker.Blueprints;
using ToyBox.Infrastructure.Inspector;
using static ToyBox.Infrastructure.UI;

namespace ToyBox.Features.PatchTool;

public class BlueprintPickerGUI {
    private string m_PickerText = "";
    private bool m_NoSuchBP = false;
    private bool m_ShowBrowserPicker = false;
    private bool m_ShowBrowser = false;
    private Browser<SimpleBlueprint>? m_Browser;
    private Type? m_LoadedForCategory;
    private Type? m_Category;
    private string m_CategorySearchString = "";
    private readonly List<Type> m_Categories = [.. BlueprintIdCache.CachedIdTypes.Append(typeof(SimpleBlueprint)).OrderBy(a => a.Name)];
    public void OnGUI(Action<string> callback, Type? setCategory = null) {
        using (HorizontalScope()) {
            Space(20);
            using (VerticalScope()) {
                if (setCategory == null) {
                    _ = DisclosureToggle(ref m_ShowBrowserPicker, PatchToolStrings.ShowBrowserPicker);
                } else {
                    m_Category = setCategory;
                }
                if (m_ShowBrowserPicker || setCategory != null) {
                    if (setCategory == null) {
                        using (HorizontalScope()) {
                            Label(PatchToolStrings.BpCategory.Cyan(), Width(150));
                            _ = ActionTextField(ref m_CategorySearchString, "patchtool_category_search", null, null, Width(300));
                        }
                        var filtered = string.IsNullOrEmpty(m_CategorySearchString)
                            ? m_Categories
                            : m_Categories.Where(t => t.Name.IndexOf(m_CategorySearchString, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
                        if (SelectionGrid(ref m_Category, filtered, 4, t => t.Name, SharedStrings.NoneText, Width(0.9f * EffectiveWindowWidth()))) {
                            m_Browser = null;
                        }
                        Space(20);
                    }
                    if (m_Category != null) {
                        _ = DisclosureToggle(ref m_ShowBrowser, PatchToolStrings.ShowBrowser);
                        if (m_ShowBrowser) {
                            if (m_Browser == null || m_LoadedForCategory != m_Category) {
                                m_LoadedForCategory = m_Category;
                                var browser = new Browser<SimpleBlueprint>(BPHelper.GetSortKey, BPHelper.GetSearchKey, null, null, true, (int)(0.9f * EffectiveWindowWidth()));
                                m_Browser = browser;
                                _ = BlueprintsOfType(m_Category, items => {
                                    if (m_Browser == browser) {
                                        browser.QueueUpdateItems(items);
                                    }
                                });
                            }
                            m_Browser.OnGUI(bp => {
                                Space(10);
                                using (VerticalScope()) {
                                    using (HorizontalScope()) {
                                        _ = Button(SharedStrings.PickBlueprintText, () => {
                                            callback(bp.AssetGuid.ToString());
                                        });
                                        Space(17);
                                        var title = BPHelper.GetTitle(bp, name => name.Cyan().Bold());
                                        InspectorUI.InspectToggle(bp, title, bp, 0, false, Width(300));
                                        Space(5);
                                        Label(bp.GetType().Name.Grey(), AutoWidth());
                                        Space(17);
                                        var guid = bp.AssetGuid.ToString();
                                        _ = TextField(ref guid, null, Width(300));
                                        Space(17);
                                        var desc = BPHelper.GetDescription(bp);
                                        if (!string.IsNullOrWhiteSpace(desc)) {
                                            Label(desc!.Green(), Width(1000));
                                        }
                                    }
                                    InspectorUI.InspectIfExpanded(bp);
                                }
                            });
                        }
                    }
                }
                Space(20);
                using (HorizontalScope()) {
                    Label(SharedStrings.EnterTargetBlueprintIdText, Width(200));
                    var before = m_PickerText;
                    _ = TextField(ref m_PickerText, null, Width(350));
                    if (before != m_PickerText) {
                        m_NoSuchBP = false;
                    }
                    _ = Button(SharedStrings.PickBlueprintText, () => {
                        if (ResourcesLibrary.BlueprintsCache.m_LoadedBlueprints.ContainsKey(m_PickerText)) {
                            callback(m_PickerText);
                        } else {
                            m_NoSuchBP = true;
                        }
                    });
                    if (m_NoSuchBP) {
                        Space(20);
                        Label(SharedStrings.NoBlueprintWithThatGuidFound.Yellow(), Width(300));
                    }
                }
            }
        }
    }
}
