using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Mechanics.Blueprints;
using ToyBox.Classes.Infrastructure.Features;
using ToyBox.Infrastructure.Utilities;

namespace ToyBox.Infrastructure.Blueprints.BlueprintActions;

public partial class AddMechanicEntityFactBA : BlueprintActionFeature, IBlueprintAction<BlueprintMechanicEntityFact>, INeedContextFeature<BaseUnitEntity> {
    public bool CanExecute(BlueprintMechanicEntityFact blueprint, ActionParameter parameter) {
        if (parameter.UnitParam is BaseUnitEntity unit) {
            return unit.Facts.Get(blueprint) == null;
        }
        return false;
    }
    public bool Execute(BlueprintMechanicEntityFact blueprint, ActionParameter parameter) {
        LogExecution(blueprint, parameter);
        if (blueprint is BlueprintAbility) {
            var fact = parameter.UnitParam!.AddFact(blueprint);
            if (fact != null) {
                // Abilities need or source or they disappear after reloading
                fact.AddSource(new Kingmaker.EntitySystem.EntityFactSource(blueprint));
                return true;
            } else {
                return false;
            }
        } else {
            return parameter.UnitParam!.AddFact(blueprint) != null;
        }
    }
    public bool? OnGui(BlueprintMechanicEntityFact blueprint, bool isFeatureSearch, ActionParameter parameter) {
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
            _ = OnGui(bp!, true, new(unit));
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
