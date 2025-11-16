using Kingmaker.Controllers;
using Kingmaker.Enums;
using UnityEngine;

namespace ToyBox.Features.BagOfTricks.RTSpecific;

[IsTested]
public partial class ModifyFactionReputationFeature : Feature {
    [LocalizedString("ToyBox_Features_BagOfTricks_RTSpecific_ModifyFactionReputationFeature_Name", "Modify Faction Reputation")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_RTSpecific_ModifyFactionReputationFeature_Description", "Allows you to modify the reputation at the various in-game factions.")]
    public override partial string Description { get; }
    private FactionType m_SelectedFaction = FactionType.None;
    private int m_Adjustment = 100;
    public override void OnGui() {
        using (HorizontalScope()) {
            UI.Label(Name);
            Space(10);
            UI.Label(Description.Green());
        }
        if (!IsInGame()) {
            UI.Label(SharedStrings.ThisCannotBeUsedFromTheMainMenu.Red().Bold());
            return;
        }
        _ = UI.SelectionGrid(ref m_SelectedFaction, 6, @enum => @enum.ToString());
        if (m_SelectedFaction != FactionType.None) {
            using (HorizontalScope()) {
                UI.Label(m_CurrentReputationLocalizedText.Bold() + ": ", Width(250 * Main.UIScale));
                using (VerticalScope()) {
                    using (HorizontalScope()) {
                        UI.Label(m_LevelLocalizedText + ": ", Width(100 * Main.UIScale));
                        UI.Label(ReputationHelper.GetCurrentReputationLevel(m_SelectedFaction).ToString());
                    }
                    using (HorizontalScope()) {
                        UI.Label(m_ExperienceLocalizedText + ": ", Width(100 * Main.UIScale));
                        UI.Label($"{ReputationHelper.GetCurrentReputationPoints(m_SelectedFaction)}/{ReputationHelper.GetNextLevelReputationPoints(m_SelectedFaction)}");
                    }
                    using (HorizontalScope()) {
                        UI.Label(m_AdjustReputationByTheFollowingAmLocalizedText + ":");
                        if (UI.TextField(ref m_Adjustment, null, GUILayout.MinWidth(200), AutoWidth())) {
                            m_Adjustment = m_Adjustment < 0 ? 1 : m_Adjustment;
                        }
                        Space(10);
                        _ = UI.Button(m_AddLocalizedText, () => ReputationHelper.GainFactionReputation(m_SelectedFaction, m_Adjustment));
                        Space(10);
                        _ = UI.Button(m_RemoveLocalizedText, () => ReputationHelper.GainFactionReputation(m_SelectedFaction, -m_Adjustment));
                    }
                }
            }
        }
    }

    [LocalizedString("ToyBox_Features_BagOfTricks_RTSpecific_ModifyFactionReputationFeature_m_CurrentReputationLocalizedText", "Current Reputation")]
    private static partial string m_CurrentReputationLocalizedText { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_RTSpecific_ModifyFactionReputationFeature_m_LevelLocalizedText", "Level")]
    private static partial string m_LevelLocalizedText { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_RTSpecific_ModifyFactionReputationFeature_m_ExperienceLocalizedText", "Experience")]
    private static partial string m_ExperienceLocalizedText { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_RTSpecific_ModifyFactionReputationFeature_m_AdjustReputationByTheFollowingAmLocalizedText", "Adjust Reputation by the following amount")]
    private static partial string m_AdjustReputationByTheFollowingAmLocalizedText { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_RTSpecific_ModifyFactionReputationFeature_m_AddLocalizedText", "Add")]
    private static partial string m_AddLocalizedText { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_RTSpecific_ModifyFactionReputationFeature_m_RemoveLocalizedText", "Remove")]
    private static partial string m_RemoveLocalizedText { get; }
}
