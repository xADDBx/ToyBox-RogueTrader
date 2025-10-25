using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Mechanics.Blueprints;
using ToyBox.Infrastructure.Utilities;

namespace ToyBox.Infrastructure.Blueprints.BlueprintActions;
[NeedsTesting]
public partial class AddMechanicEntityFactBA : BlueprintActionFeature, IBlueprintAction<BlueprintMechanicEntityFact>, INeedContextFeature<BaseUnitEntity> {
    public bool CanExecute(BlueprintMechanicEntityFact blueprint, params object[] parameter) {
        if (parameter.Length > 0 && parameter[0] is BaseUnitEntity unit) {
            return unit.Facts.Get(blueprint) == null;
        }
        return false;
    }
    internal bool Execute(BlueprintMechanicEntityFact blueprint, params object[] parameter) {
        LogExecution(blueprint, parameter);
        if (blueprint is BlueprintAbility) {
            var fact = ((BaseUnitEntity)parameter[0]).AddFact(blueprint);
            if (fact != null) {
                // Abilities need or source or they disappear after reloading
                fact.AddSource(new Kingmaker.EntitySystem.EntityFactSource(blueprint));
                return true;
            } else {
                return false;
            }
        } else {
            return ((BaseUnitEntity)parameter[0]).AddFact(blueprint) != null;
        }
    }
    public bool? OnGui(BlueprintMechanicEntityFact blueprint, bool isFeatureSearch, params object[] parameter) {
        bool? result = null;
        if (CanExecute(blueprint, parameter)) {
            _ = UI.Button(StyleActionString(m_AddText, isFeatureSearch), () => {
                result = Execute(blueprint, parameter);
            });
            UI.Label(" ");
        } else if (isFeatureSearch) {
            UI.Label(m_UnitAlreadyHasThisFactText.Red().Bold());
        }

        return result;
    }
    public bool GetContext(out BlueprintMechanicEntityFact? context) {
        return ContextProvider.Blueprint(out context);
    }

    public bool GetContext(out BaseUnitEntity? context) {
        return ContextProvider.BaseUnitEntity(out context);
    }

    public override void OnGui() {
        if (GetContext(out BlueprintMechanicEntityFact? bp) && GetContext(out BaseUnitEntity? unit)) {
            _ = OnGui(bp!, true, unit!);
        }
    }
    [LocalizedString("ToyBox_Infrastructure_Blueprints_BlueprintActions_AddMechanicEntityFactBA_AddText", "Add")]
    private static partial string m_AddText { get; }
    [LocalizedString("ToyBox_Infrastructure_Blueprints_BlueprintActions_AddMechanicEntityFactBA_Name", "Add Fact")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Infrastructure_Blueprints_BlueprintActions_AddMechanicEntityFactBA_Description", "Adds the specified BlueprintMechanicEntityFact to the chosen unit.")]
    public override partial string Description { get; }
    [LocalizedString("ToyBox_Infrastructure_Blueprints_BlueprintActions_AddMechanicEntityFactBA_UnitAlreadyHasThisFactText", "Unit already has this Fact")]
    private static partial string m_UnitAlreadyHasThisFactText { get; }
}
