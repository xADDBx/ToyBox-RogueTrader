using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using ToyBox.Infrastructure.Utilities;
using UnityEngine;

namespace ToyBox.Features.PartyTab;

public partial class IncreaseUnitLevelFeature : Feature, INeedContextFeature<BaseUnitEntity> {
    [LocalizedString("ToyBox_Features_PartyTab_IncreaseUnitLevelFeature_Name", "Increase Unit Level")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_PartyTab_IncreaseUnitLevelFeature_Description", "Allows granting enough experience to increase level by 1.")]
    public override partial string Description { get; }

    public bool GetContext(out BaseUnitEntity? context) {
        return ContextProvider.BaseUnitEntity(out context, false);
    }

    public override void OnGui() {
        if (GetContext(out var unit)) {
            OnGui(unit!);
        }
    }
    private static readonly TimedCache<float> m_LevelLabelWidth = new(() => {
        return CalculateLargestLabelSize([m_Lvl_LocalizedText + " 00 > 00 "], GUI.skin.label);
    });
    private static readonly TimedCache<float> m_MaxLabelWidth = new(() => {
        return CalculateLargestLabelSize([m_MaxLocalizedText], GUI.skin.button) + 5 * Main.UIScale;
    });

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
        if (highestReachableLevel > currentLevel && ToyBoxUnitHelper.IsPartyOrPet(unit)) {
            UI.Label(m_Lvl_LocalizedText + $" {currentLevel} > ".Green() + $"{highestReachableLevel}".Cyan(), Width(m_LevelLabelWidth));
        } else {
            UI.Label(m_Lvl_LocalizedText + $" {currentLevel}".Green(), Width(m_LevelLabelWidth));
        }
        if (Game.Instance.Player.AllCharacters.Contains(unit) && unit.Master == null) {
            if (maxLevelIndex > highestReachableLevel) {
                if (UI.Button("+1", null, null, Width(m_MaxLabelWidth))) {
                    unit.Progression.AdvanceExperienceTo(xpTable.GetBonus(highestReachableLevel + 1), true);
                }
            } else {
                UI.Label(m_MaxLocalizedText, Width(m_MaxLabelWidth));
            }
        } else {
            UI.Label("", Width(m_MaxLabelWidth));
        }
    }
    [LocalizedString("ToyBox_Features_PartyTab_IncreaseUnitLevelFeature_Maximum_Abbreviated", "max")]
    private static partial string m_MaxLocalizedText { get; }
    [LocalizedString("ToyBox_Features_PartyTab_IncreaseUnitLevelFeature_Level_Abbreviated", "Lvl.")]
    private static partial string m_Lvl_LocalizedText { get; }
}
