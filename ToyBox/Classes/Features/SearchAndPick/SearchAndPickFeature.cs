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
    private readonly Browser<SimpleBlueprint> m_SearchNPickBrowser = new(BPHelper.GetSortKey, BPHelper.GetSearchKey, overridePageWidth: (int)(EffectiveWindowWidth() * 0.8f));
    public override void OnGui() {
        using (HorizontalScope()) {
            if (UI.SelectionGrid(ref Settings.CurrentBlueprintFilter, BlueprintFilters.Filters, 1, filter => filter.Name, Width(m_FilterWidth + 10 * Main.UIScale))) {
                m_CurrentCollationCategory = null;
            }
            var categories = Settings.CurrentBlueprintFilter.GetAllCollationCategories();
            if (categories != null) {
                if (categories.Count > 0) {
                    if (m_CurrentCollationCategory == null) {
                        m_CurrentCollationCategory = categories[0];
                        m_SearchNPickBrowser.UpdateItems(Settings.CurrentBlueprintFilter.GetCollatedBlueprints(m_CurrentCollationCategory)!);
                    }
                    if (UI.SelectionGrid(ref m_CurrentCollationCategory!, categories!, 1, null)) {
                        m_SearchNPickBrowser.UpdateItems(Settings.CurrentBlueprintFilter.GetCollatedBlueprints(m_CurrentCollationCategory)!);
                    }
                    m_SearchNPickBrowser.OnGUI(bp => BlueprintUI.BlueprintRowGUI(m_SearchNPickBrowser, bp, CharacterPicker.CurrentUnit), BlueprintUI.BlueprintHeaderGUI);
                } else {
                    UI.Label("????????????????????????".Red().Bold());
                }
            } else {
                UI.Label("Collating!".Red().Bold());
            }
        }
    }
}
