using Kingmaker.Blueprints;
using Kingmaker.EntitySystem.Entities;
using ToyBox.Infrastructure.Utilities;

namespace ToyBox.Infrastructure.Blueprints.BlueprintActions;

public partial class AddAbilityResourceBA : BlueprintActionFeature, IBlueprintAction<BlueprintAbilityResource>, INeedContextFeature<BaseUnitEntity> {
    public bool CanExecute(BlueprintAbilityResource blueprint, params object[] parameter) {
        if (parameter.Length > 0 && parameter[0] is BaseUnitEntity unit) {
            return !unit.AbilityResources.ContainsResource(blueprint);
        } else {
            return false;
        }
    }
    private bool Execute(BlueprintAbilityResource blueprint, params object[] parameter) {
        LogExecution(blueprint, parameter);
        ((BaseUnitEntity)parameter[0])!.AbilityResources.Add(blueprint, true);
        return true;
    }
    public bool? OnGui(BlueprintAbilityResource blueprint, bool isFeatureSearch, params object[] parameter) {
        bool? result = null;
        if (CanExecute(blueprint, parameter)) {
            _ = UI.Button(StyleActionString(m_AddText, isFeatureSearch), () => {
                result = Execute(blueprint, parameter);
            });
        } else if (isFeatureSearch) {
            UI.Label(m_UnitAlreadyHasThisAbilityResourc.Red().Bold());
        }
        return result;
    }
    public override void OnGui() {
        if (GetContext(out BlueprintAbilityResource? bp) && GetContext(out BaseUnitEntity? unit)) {
            _ = OnGui(bp!, true, unit!);
        }
    }
    public bool GetContext(out BaseUnitEntity? context) {
        return ContextProvider.BaseUnitEntity(out context);
    }

    public bool GetContext(out BlueprintAbilityResource? context) {
        return ContextProvider.Blueprint(out context);
    }

    [LocalizedString("ToyBox_Infrastructure_Blueprints_BlueprintActions_AddAbilityResourceBA_Name", "Add Ability Resource")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Infrastructure_Blueprints_BlueprintActions_AddAbilityResourceBA_Description", "Adds the specified BlueprintAbilityResource to the chosen unit.")]
    public override partial string Description { get; }
    [LocalizedString("ToyBox_Infrastructure_Blueprints_BlueprintActions_AddAbilityResourceBA_AddText", "Add")]
    private static partial string m_AddText { get; }
    [LocalizedString("ToyBox_Infrastructure_Blueprints_BlueprintActions_AddAbilityResourceBA_UnitAlreadyHasThisAbilityResourc", "Unit already has this ability resource")]
    private static partial string m_UnitAlreadyHasThisAbilityResourc { get; }
}
