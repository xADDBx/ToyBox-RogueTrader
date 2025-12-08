using Kingmaker;
using Kingmaker.Items;
using Kingmaker.Utility;

namespace ToyBox.Features.Loot;

public partial class LootChecklistFeature : Feature {
    [LocalizedString("ToyBox_Features_Loot_LootChecklistFeature_Name", "Loot Checklist")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_Loot_LootChecklistFeature_Description", "Displays a list of all the loot left on the map.")]
    public override partial string Description { get; }
    private string m_SearchQuery = "";
    public bool IsGatheringLoot {
        get;
        private set;
    }
    public static List<ItemEntity>? GetItemLootFromWrapper(LootWrapper present, string searchText) {
        if (present.InteractionLoot != null) {
            return [.. present.InteractionLoot.Loot.Items.Where(i => string.IsNullOrWhiteSpace(searchText) || i.Name.ToUpper().Contains(searchText))];
        }
        return null;
    }
    public static List<ItemEntity>? GetUnitLootFromWrapper(LootWrapper present, string searchText) {
        if (present.Unit != null) {
            return [.. present.Unit.Inventory.Items.Where(i => string.IsNullOrWhiteSpace(searchText) || i.Name.ToUpper().Contains(searchText))];
        }
        return null;
    }
    public static List<ItemEntity>? GetLootFromWrapper(LootWrapper present, string searchText) {
        return GetItemLootFromWrapper(present, searchText) ?? GetUnitLootFromWrapper(present, searchText);
    }
    public static string GetSource(LootWrapper present) {
        if (present.InteractionLoot != null) {
            var name = present.InteractionLoot.Source.ToString();
            if (string.IsNullOrEmpty(name)) {
                name = m_GroundLocalizedText;
            }
            return name;
        }
        return present.Unit?.CharacterName ?? m_UnknownLocalizedText;
    }
    public override void OnGui() {
        if (Game.Instance.CurrentlyLoadedArea == null || !IsInGame()) {
            UI.Label(m_NotInAnyArea_LocalizedText.Red().Bold());
            return;
        }
        var isEmpty = true;
        UI.Label(Game.Instance.CurrentlyLoadedArea.AreaDisplayName.Orange());
        using (HorizontalScope()) {
            UI.Label(m_SearchLocalizedText + ": ");
            UI.TextField(ref m_SearchQuery);
            IsGatheringLoot = true;
            var lootGroups = MassLootHelper.GetMassLootFromCurrentArea().GroupBy(p => p.InteractionLoot != null ? m_ContainersLocalizedText : m_UnitsLocalizedText);
            IsGatheringLoot = false;
            using (VerticalScope()) {
                var actualSearchQuery = m_SearchQuery.ToUpper();
                IEnumerable<IGrouping<string, LootWrapper>> orderedLootGroups = [.. lootGroups.Where(g => g.Key == m_ContainersLocalizedText), .. lootGroups.Where(g => g.Key == m_UnitsLocalizedText)];
                foreach (var group in orderedLootGroups) {
                    var presents = group.OrderByDescending(p => {
                        return GetLootFromWrapper(p, actualSearchQuery)?.Count ?? 0;
                    });
                    UI.Label($"{group.Key}".Cyan());
                    using (HorizontalScope()) {
                        Space(5);
                        using (VerticalScope()) {
                            Div.DrawDiv();
                            foreach (var present in presents) {
                                var loot = GetLootFromWrapper(present, actualSearchQuery);
                                if (loot?.Count > 0 && present.Unit != null) {
                                    isEmpty = false;
                                    Div.DrawDiv();
                                    using (HorizontalScope()) {
                                        Space(5);
                                        UI.Label(GetSource(present).Orange());
                                        Space(15);
                                        using (VerticalScope()) {
                                            foreach (var item in loot) {
                                                var description = item.Blueprint.Description;
                                                using (HorizontalScope()) {
                                                    UI.Label(StripHTML(item.Name));
                                                    if (!string.IsNullOrWhiteSpace(description)) {
                                                        UI.Label(StripHTML(description.Green()));
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        if (isEmpty) {
            UI.Label(m_NoLootAvailable_LocalizedText.Orange());
        }
    }

    [LocalizedString("ToyBox_Features_Loot_LootChecklistFeature_m_SearchLocalizedText", "Search")]
    private static partial string m_SearchLocalizedText { get; }
    [LocalizedString("ToyBox_Features_Loot_LootChecklistFeature_m_ContainersLocalizedText", "Containers")]
    private static partial string m_ContainersLocalizedText { get; }
    [LocalizedString("ToyBox_Features_Loot_LootChecklistFeature_m_UnitsLocalizedText", "Units")]
    private static partial string m_UnitsLocalizedText { get; }
    [LocalizedString("ToyBox_Features_Loot_LootChecklistFeature_m_SourceIsUnknownLocalizedText", "Unknown")]
    private static partial string m_UnknownLocalizedText { get; }
    [LocalizedString("ToyBox_Features_Loot_LootChecklistFeature_m_SourceIsGroundLocalizedText", "Ground")]
    private static partial string m_GroundLocalizedText { get; }
    [LocalizedString("ToyBox_Features_Loot_LootChecklistFeature_m_NoLootAvailable_LocalizedText", "No Loot Available.")]
    private static partial string m_NoLootAvailable_LocalizedText { get; }
    [LocalizedString("ToyBox_Features_Loot_LootChecklistFeature_m_NotInAnyArea_LocalizedText", "Not in any area!")]
    private static partial string m_NotInAnyArea_LocalizedText { get; }
}
