using Kingmaker.EntitySystem.Entities;
using Kingmaker.Visual.Sound;
using ToyBox.Infrastructure.Utilities;

namespace ToyBox.Infrastructure.Blueprints.BlueprintActions.Units;

public partial class ChangeVoiceBA : BlueprintActionFeature, IBlueprintAction<BlueprintUnitAsksList>, INeedContextFeature<BaseUnitEntity> {
    [LocalizedString("ToyBox_Infrastructure_Blueprints_BlueprintActions_Units_ChangeVoiceBA_Name", "Change Voice")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Infrastructure_Blueprints_BlueprintActions_Units_ChangeVoiceBA_Description", "Changes the voice of the specified unit to the specified BlueprintUnitAsksList.")]
    public override partial string Description { get; }

    public bool CanExecute(BlueprintUnitAsksList blueprint, params object[] parameter) {
        if (parameter.Length > 0 && parameter[0] is BaseUnitEntity unit) {
            return unit.Asks.List != blueprint;
        }
        return false;
    }
    private bool Execute(BlueprintUnitAsksList blueprint, params object[] parameter) {
        LogExecution(blueprint, parameter);
        var unit = (BaseUnitEntity)parameter[0];
        unit.Asks.SetCustom(blueprint);
        unit.View.UpdateAsks();
        return true;
    }
    public bool? OnGui(BlueprintUnitAsksList blueprint, bool isFeatureSearch, params object[] parameter) {
        bool? result = null;
        if (CanExecute(blueprint, parameter)) {
            _ = UI.Button(StyleActionString(m_ChangeVoiceLocalizedText, isFeatureSearch), () => {
                result = Execute(blueprint, parameter);
            });
            UI.Label(" ");
        } else if (isFeatureSearch) {
            UI.Label(m_ThisIsTheCurrentVoice_LocalizedText.Red().Bold());
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

    [LocalizedString("ToyBox_Infrastructure_Blueprints_BlueprintActions_Units_ChangeVoiceBA_m_ChangeVoiceLocalizedText", "Change Voice")]
    private static partial string m_ChangeVoiceLocalizedText { get; }
    [LocalizedString("ToyBox_Infrastructure_Blueprints_BlueprintActions_Units_ChangeVoiceBA_m_ThisIsTheCurrentVoice_LocalizedText", "This is the current voice!")]
    private static partial string m_ThisIsTheCurrentVoice_LocalizedText { get; }
}
