using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using ToyBox.Infrastructure.Utilities;

namespace ToyBox.Features.PartyTab;

public partial class IncreaseUnitLevelFeature : Feature, INeedContextFeature<BaseUnitEntity> {
    [LocalizedString("ToyBox_Features_PartyTab_IncreaseUnitLevelFeature_Name", "Increase Unit Level")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_PartyTab_IncreaseUnitLevelFeature_Description", "Allows increasing the level of a unit by 1.")]
    public override partial string Description { get; }

    public bool GetContext(out BaseUnitEntity? context) {
        return ContextProvider.BaseUnitEntity(out context);
    }

    public override void OnGui() {
        if (GetContext(out var unit)) {
            OnGui(unit!);
        }
    }

    public void OnGui(BaseUnitEntity unit) {
        var currentLevel = unit.Progression.CharacterLevel;
        var xpTable = unit.Progression.ExperienceTable;
        var maxLevelIndex = xpTable.Bonuses.Length - 1;
        var hasLevelsLeftToReach = currentLevel >= 0 && currentLevel < maxLevelIndex;
        var highestReachableLevel = currentLevel;
        // As long as highest reached is below max level, and experience is enough to reach the next level
        while (highestReachableLevel < maxLevelIndex && unit.Progression.Experience >= xpTable.GetBonus(highestReachableLevel + 1)) {
            highestReachableLevel++;
        }
        if (highestReachableLevel > currentLevel) {
            UI.Label(m_Lvl_LocalizedText + $" {currentLevel} > ".Green() + $"{highestReachableLevel}".Cyan(), Width(Main.UIScale * 75));
        } else {
            UI.Label(m_Lvl_LocalizedText + $" {currentLevel}".Green(), Width(Main.UIScale * 75));
        }
        // ??? Maybe filtering for pets?
        if (Game.Instance.Player.AllCharacters.Contains(unit)) {
            if (maxLevelIndex > highestReachableLevel) {
                if (UI.Button("+1", null, null, Width(Main.UIScale * 35))) {
                    unit.Progression.AdvanceExperienceTo(xpTable.GetBonus(highestReachableLevel + 1), true);
                }
            } else {
                UI.Label(m_MaxLocalizedText, Width(Main.UIScale * 35));
            }
        } else {
            UI.Label("", Width(Main.UIScale * 35));
        }
    }
    [LocalizedString("ToyBox_Features_PartyTab_IncreaseUnitLevelFeature_Maximum_Abbreviated", "max")]
    private static partial string m_MaxLocalizedText { get; }
    [LocalizedString("ToyBox_Features_PartyTab_IncreaseUnitLevelFeature_Level_Abbreviated", "Lvl.")]
    private static partial string m_Lvl_LocalizedText { get; }
}
