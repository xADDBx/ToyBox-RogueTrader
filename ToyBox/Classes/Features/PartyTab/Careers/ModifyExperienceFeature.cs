using Kingmaker.EntitySystem.Entities;
using ToyBox.Infrastructure.Utilities;
using UnityEngine;

namespace ToyBox.Features.PartyTab.Careers;

[IsTested]
public partial class ModifyExperienceFeature : Feature, INeedContextFeature<BaseUnitEntity> {
    [LocalizedString("ToyBox_Features_PartyTab_Careers_ModifyExperienceFeature_Name", "Modify Unit Experience")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_PartyTab_Careers_ModifyExperienceFeature_Description", "Allows changing the amount of experience points a unit has.")]
    public override partial string Description { get; }

    public bool GetContext(out BaseUnitEntity? context) {
        return ContextProvider.BaseUnitEntity(out context, false);
    }

    public override void OnGui() {
        if (GetContext(out var unit)) {
            OnGui(unit!);
        }
    }
    private static readonly TimedCache<float> m_ButtonWidth = new(() => {
        return CalculateLargestLabelSize([m_AdjustBasedOnLevelLocalizedText], GUI.skin.button) + 5 * Main.UIScale;
    });
    public void OnGui(BaseUnitEntity unit) {
        UI.Label((m_ExperienceLocalizedText + ": ").Cyan(), AutoWidth());
        using (VerticalScope()) {
            using (HorizontalScope()) {
                var currentExperience = unit.Progression.Experience;
                _ = UI.TextField(ref currentExperience, pair => {
                    unit.Progression.Experience = pair.newContent;
                }, Width(m_ButtonWidth));
                Space(Main.UIScale * 10);
                UI.Label(Description.Green());
            }
            using (HorizontalScope()) {
                if (UI.Button(m_AdjustBasedOnLevelLocalizedText, null, null, Width(m_ButtonWidth))) {
                    unit.Progression.Experience = unit.Progression.ExperienceTable.GetBonus(unit.Progression.CharacterLevel);
                }
                Space(Main.UIScale * 10);
                UI.Label(m_ThisSetsYourExperienceToMatchTheLocalizedText.Green());
            }
        }
    }

    [LocalizedString("ToyBox_Features_PartyTab_Careers_ModifyExperienceFeature_m_ExperienceLocalizedText", "Experience")]
    private static partial string m_ExperienceLocalizedText { get; }
    [LocalizedString("ToyBox_Features_PartyTab_Careers_ModifyExperienceFeature_m_AdjustBasedOnLevelLocalizedText", "Adjust based on Level")]
    private static partial string m_AdjustBasedOnLevelLocalizedText { get; }
    [LocalizedString("ToyBox_Features_PartyTab_Careers_ModifyExperienceFeature_m_ThisSetsYourExperienceToMatchTheLocalizedText", "This sets your experience to match the current value of character level")]
    private static partial string m_ThisSetsYourExperienceToMatchTheLocalizedText { get; }
}
