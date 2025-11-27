using Kingmaker.Blueprints;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Visual.Sound;
using ToyBox.Infrastructure.Blueprints.BlueprintActions;
using ToyBox.Infrastructure.Utilities;

namespace ToyBox.Classes.Infrastructure.Blueprints.BlueprintActions.Units;

public partial class PlayVoiceBA : BlueprintActionFeature, IBlueprintAction<BlueprintUnitAsksList>, INeedContextFeature<BaseUnitEntity> {
    [LocalizedString("ToyBox_Classes_Infrastructure_Blueprints_BlueprintActions_Units_PlayVoiceBA_Name", "Play Voice Example")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Classes_Infrastructure_Blueprints_BlueprintActions_Units_PlayVoiceBA_Description", "Plays the PartyMemberUnconscious sound with the specified voice (if that sound doesn't exist for said voice nothing happens).")]
    public override partial string Description { get; }

    public bool CanExecute(BlueprintUnitAsksList blueprint, params object[] parameter) {
        if (parameter.Length > 0 && parameter[0] is BaseUnitEntity unit) {
            return true;
        }
        return false;
    }
    private bool Execute(BlueprintUnitAsksList blueprint, params object[] parameter) {
        LogExecution(blueprint, parameter);
        var unit = (BaseUnitEntity)parameter[0];
        var comp = blueprint.GetComponent<UnitAsksComponent>();
        if (unit.Asks.List == blueprint) {
            return new BarkWrapper(comp.PartyMemberUnconscious, unit.View.Asks).Schedule();
        } else {
            var manager = new UnitBarksManager(unit, comp);
            manager.LoadBanks();
            return new BarkWrapper(comp.PartyMemberUnconscious, manager).Schedule(callback: (_, _, _) => {
                manager.UnloadBanks();
            });
        }
    }
    public bool? OnGui(BlueprintUnitAsksList blueprint, bool isFeatureSearch, params object[] parameter) {
        bool? result = null;
        if (CanExecute(blueprint, parameter)) {
            _ = UI.Button(StyleActionString(m_PlayExampleLocalizedText, isFeatureSearch), () => {
                result = Execute(blueprint, parameter);
            });
            UI.Label(" ");
        } else if (isFeatureSearch) {
            UI.Label(m_CanOnlyPlayAnExampleOfTheCurrentLocalizedText.Red().Bold());
        }
        return result;
    }
    public bool GetContext(out BlueprintUnitAsksList? context) {
        return ContextProvider.Blueprint(out context);
    }
    public bool GetContext(out BaseUnitEntity? context) {
        return ContextProvider.BaseUnitEntity(out context);
    }

    public override void OnGui() {
        if (GetContext(out BlueprintUnitAsksList? bp) && GetContext(out BaseUnitEntity? unit)) {
            _ = OnGui(bp!, true, unit!);
        }
    }

    [LocalizedString("ToyBox_Classes_Infrastructure_Blueprints_BlueprintActions_Units_PlayVoiceBA_m_PlayExampleLocalizedText", "Play Example")]
    private static partial string m_PlayExampleLocalizedText { get; }
    [LocalizedString("ToyBox_Classes_Infrastructure_Blueprints_BlueprintActions_Units_PlayVoiceBA_m_CanOnlyPlayAnExampleOfTheCurrentLocalizedText", "Can only play an example of the current voice!")]
    private static partial string m_CanOnlyPlayAnExampleOfTheCurrentLocalizedText { get; }
}
