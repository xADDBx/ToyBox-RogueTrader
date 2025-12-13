using Kingmaker.Blueprints;
using ToyBox.Infrastructure.Utilities;
using UnityEngine;

namespace ToyBox.Features.SearchAndPick;

public partial class SearchAndPickFeature : Feature {
    [LocalizedString("ToyBox_Features_SearchAndPick_SearchAndPickFeature_Name", "Search 'n Pick")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_SearchAndPick_SearchAndPickFeature_Description", "Allows browsing through all the blueprints in the game and doing various actions with them.")]
    public override partial string Description { get; }
    private readonly TimedCache<float> m_FilterWidth = new(() => CalculateLargestLabelWidth(BlueprintFilters.Filters.Select(f => f.Name), GUI.skin.button));
    private string? m_CurrentCollationCategory;
    private Browser<SimpleBlueprint>? m_SearchNPickBrowser;
    private Browser<string>? m_CollationCategoryBrowser;
    private bool m_ShowCollationCategoryPicker = false;
    private bool m_ShowCharacterFilterPicker = false;
    public override void OnGui() {
        using (HorizontalScope()) {
            if (UI.SelectionGrid(ref Settings.CurrentBlueprintFilter, BlueprintFilters.Filters, 1, filter => filter.Name, Width(m_FilterWidth + 10 * Main.UIScale))) {
                m_CurrentCollationCategory = null;
            }
            var categories = Settings.CurrentBlueprintFilter.GetAllCollationCategories();
            if (categories != null) {
                if (categories.Count > 0) {
                    var categoryWidth = Settings.CurrentBlueprintFilter.GetCollationCategoryWidth();
                    if (m_CurrentCollationCategory == null) {
                        m_CurrentCollationCategory = categories[0]; 
                        m_CollationCategoryBrowser = new(s => {
                            if (s == BlueprintFilter<SimpleBlueprint>.AllLocalizedText) {
#warning Sort Order
                                return "";
                            } else {
                                return s;
                            }
                        }, s => s, categories, showDivBetweenItems: false, overridePageWidth: categoryWidth, orderInitialCollection: true);
                        SetCategoryComparer();
                        m_CollationCategoryBrowser.UpdateItems(categories);
                        m_SearchNPickBrowser ??= new(BPHelper.GetSortKey, BPHelper.GetSearchKey, overridePageWidth: (int)(EffectiveWindowWidth() - (m_FilterWidth + 20 * Main.UIScale)), orderInitialCollection: true);
                        m_SearchNPickBrowser.UpdateItems(Settings.CurrentBlueprintFilter.GetCollatedBlueprints(m_CurrentCollationCategory)!);
                    }
                    if (m_ShowCollationCategoryPicker) {
                        using (VerticalScope()) {
                            var feature = GetInstance<SortCollationCategoriesByCountSetting>();
                            if (UI.Toggle(feature.Name.Cyan(), null, ref feature.IsEnabled, feature.Initialize, feature.Destroy)) {
                                SetCategoryComparer();
                            }
                            UI.Label((m_CurrentCollationCategoryLocalizedText + ": ").Green() + m_CurrentCollationCategory.Cyan());
                            m_CollationCategoryBrowser!.OnGUI(category => {
                                if (category == m_CurrentCollationCategory) {
                                    GUILayout.Toggle(true, category.Orange() + $" ({Settings.CurrentBlueprintFilter.GetCountForCategory(category)!.Value})", UI.LeftAlignedButtonStyle, Width(categoryWidth));
                                } else {
                                    if (GUILayout.Toggle(false, category.Yellow() + $" ({Settings.CurrentBlueprintFilter.GetCountForCategory(category)!.Value})", UI.LeftAlignedButtonStyle, Width(categoryWidth))) {
                                        m_CurrentCollationCategory = category;
                                        m_SearchNPickBrowser!.UpdateItems(Settings.CurrentBlueprintFilter.GetCollatedBlueprints(category)!);
                                    }
                                }
                            });
                        }
                    }
                    using (VerticalScope()) {
                        UI.DisclosureToggle(ref m_ShowCollationCategoryPicker, m_ShowCategoryPickerLocalizedText.Cyan());
                        UI.DisclosureToggle(ref m_ShowCharacterFilterPicker, m_ShowCharacterFilterPickerLocalizedText.Cyan());
                        if (m_ShowCharacterFilterPicker) {
                            using (HorizontalScope()) {
                                Space(10);
                                CharacterPicker.OnFilterPickerGUI();
                            }
                        }
                        CharacterPicker.OnCharacterPickerGUI();
                        m_SearchNPickBrowser!.OnGUI(bp => BlueprintUI.BlueprintRowGUI(m_SearchNPickBrowser, bp, CharacterPicker.CurrentUnit), BlueprintUI.BlueprintHeaderGUI);
                    }
                } else {
                    UI.Label("????????????????????????".Red().Bold());
                }
            } else if (Settings.CurrentBlueprintFilter.IsCollating) {
                UI.Label(m_Collating_LocalizedText.Red().Bold());
            }
        }
    }
    private void SetCategoryComparer() {
        m_CollationCategoryBrowser!.SetComparer(GetInstance<SortCollationCategoriesByCountSetting>().IsEnabled ?  
            Comparer<string>.Create((string catA, string catB) => {
                var ret = (Settings.CurrentBlueprintFilter.GetCountForCategory(catB) ?? 0) - (Settings.CurrentBlueprintFilter.GetCountForCategory(catA) ?? 0);
                if (ret == 0) {
                    return catA.CompareTo(catB);
                } else {
                    return ret;
                }
        }) : BlueprintFilter<SimpleBlueprint>.Sorter);
        m_CollationCategoryBrowser.RedoSearch();
    }

    [LocalizedString("ToyBox_Features_SearchAndPick_SearchAndPickFeature_m_ShowCharacterFilterPickerLocalizedText", "Show Character Filter Picker")]
    private static partial string m_ShowCharacterFilterPickerLocalizedText { get; }
    [LocalizedString("ToyBox_Features_SearchAndPick_SearchAndPickFeature_m_CurrentCollationCategoryLocalizedText", "Current Category")]
    private static partial string m_CurrentCollationCategoryLocalizedText { get; }
    [LocalizedString("ToyBox_Features_SearchAndPick_SearchAndPickFeature_m_Collating_LocalizedText", "Collating!")]
    private static partial string m_Collating_LocalizedText { get; }
    [LocalizedString("ToyBox_Features_SearchAndPick_SearchAndPickFeature_m_ShowCategoryPickerLocalizedText", "Show Category Picker")]
    private static partial string m_ShowCategoryPickerLocalizedText { get; }
}
