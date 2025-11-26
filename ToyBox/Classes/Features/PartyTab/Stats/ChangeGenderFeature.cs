using Kingmaker.Blueprints.Base;
using Kingmaker.EntitySystem.Entities;
using ToyBox.Infrastructure.Utilities;

namespace ToyBox.Features.PartyTab.Stats;

public partial class ChangeGenderFeature : FeatureWithAction, INeedContextFeature<BaseUnitEntity> {
    [LocalizedString("ToyBox_Features_PartyTab_Stats_ChangeGenderFeature_Name", "Change Gender")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_PartyTab_Stats_ChangeGenderFeature_Description", "Toggles the characters gender between male and female.")]
    public override partial string Description { get; }

    public override void ExecuteAction(params object[] parameter) {
        var unit = (parameter[0] as BaseUnitEntity)!;
        switch (unit.Gender) {
            case Gender.Male: unit.Description?.SetGender(Gender.Female); break;
            case Gender.Female: unit.Description?.SetGender(Gender.Male); break;
        }
    }
    public bool GetContext(out BaseUnitEntity? context) {
        return ContextProvider.BaseUnitEntity(out context, false);
    }

    public override void OnGui() {
        if (GetContext(out var unit)) {
            OnGui(unit!);
        }
    }
    public void OnGui(BaseUnitEntity unit) {
        using (HorizontalScope()) {
            UI.Label(Name + ": ");
            Space(5);
            var isFemale = unit.Gender == Gender.Female;
            if (UI.Button(isFemale ? "♀".Magenta() : "♂".Aqua(), null, null, Width(Main.UIScale * 40))) {
                ExecuteAction(unit);
            }
        }
    }
}
