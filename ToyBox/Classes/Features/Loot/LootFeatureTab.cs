namespace ToyBox.Features.Loot;

public partial class LootFeatureTab : FeatureTab {
    [LocalizedString("ToyBox_Features_Loot_LootFeatureTab_Name", "Loot")]
    public override partial string Name { get; }
    public LootFeatureTab() {
        AddFeature(new OpenMassLootAction(), m_LootLocalizedText);
        AddFeature(new MassLootShowHiddenItemsSetting(), m_LootLocalizedText);
        AddFeature(new MassLootShowLivingNPCItemsSetting(), m_LootLocalizedText);
        AddFeature(new LootChecklistShowHiddenLootSetting(), m_ChecklistLocalizedText);
        AddFeature(new LootChecklistFeature(), m_ChecklistLocalizedText);
    }
    public override void OnGui() {
        if (!IsInGame()) {
            UI.Label(SharedStrings.ThisCannotBeUsedFromTheMainMenu.Red().Bold());
            return;
        }
        base.OnGui();
    }

    [LocalizedString("ToyBox_Features_Loot_LootFeatureTab_m_LootLocalizedText", "Loot")]
    private static partial string m_LootLocalizedText { get; }
    [LocalizedString("ToyBox_Features_Loot_LootFeatureTab_m_ChecklistLocalizedText", "Checklist")]
    private static partial string m_ChecklistLocalizedText { get; }
}
