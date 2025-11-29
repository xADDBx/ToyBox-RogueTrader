using Kingmaker.EntitySystem.Entities;
using ToyBox.Infrastructure.Utilities;

namespace ToyBox.Features.PartyTab.Careers;

[IsTested]
public partial class ModifyCharacterLevelFeature : Feature, INeedContextFeature<BaseUnitEntity> {
    [LocalizedString("ToyBox_Features_PartyTab_Careers_ModifyCharacterLevelFeature_Name", "Modify Character Level")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_PartyTab_Careers_ModifyCharacterLevelFeature_Description", "Allows changing the character level of a unit without changing its experience. Use Increase Unit Level instead to properly adjust experience. This gets reset on save reload.")]
    public override partial string Description { get; }

    public bool GetContext(out BaseUnitEntity? context) {
        return ContextProvider.BaseUnitEntity(out context, false);
    }

    public override void OnGui() {
        if (GetContext(out var unit)) {
            using (HorizontalScope()) {
                OnGui(unit!);
            }
        }
    }

    public void OnGui(BaseUnitEntity unit) {
        UI.Label((m_CharacterLevelLocalizedText + ": ").Cyan(), AutoWidth());
        var currentExperience = unit.Progression.Experience;
        var level = unit.Progression.m_CharacterLevel;
        if (UI.ValueAdjuster(ref level, 1, 0, 55)) {
            unit.Progression.m_CharacterLevel = level;
        }
        Space(10);
        UI.Label(Description.Green());
    }

    [LocalizedString("ToyBox_Features_PartyTab_Careers_ModifyCharacterLevelFeature_m_CharacterLevelLocalizedText", "Character Level")]
    private static partial string m_CharacterLevelLocalizedText { get; }
}
