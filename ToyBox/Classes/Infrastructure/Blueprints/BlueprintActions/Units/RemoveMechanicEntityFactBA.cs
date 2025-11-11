using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic.Mechanics.Blueprints;
using ToyBox.Infrastructure.Utilities;

namespace ToyBox.Infrastructure.Blueprints.BlueprintActions;

public partial class RemoveMechanicEntityFactBA : BlueprintActionFeature, IBlueprintAction<BlueprintMechanicEntityFact>, INeedContextFeature<BaseUnitEntity> {
    public bool CanExecute(BlueprintMechanicEntityFact blueprint, params object[] parameter) {
        if (parameter.Length > 0 && parameter[0] is BaseUnitEntity unit) {
            return unit.Facts.Get(blueprint) != null;
        }
        return false;
    }
    private bool Execute(BlueprintMechanicEntityFact blueprint, params object[] parameter) {
        LogExecution(blueprint, parameter);
        var unit = (BaseUnitEntity)parameter[0];
        unit.Facts.Remove(blueprint);
        return true;
    }
    public bool? OnGui(BlueprintMechanicEntityFact blueprint, bool isFeatureSearch, params object[] parameter) {
        bool? result = null;
        if (CanExecute(blueprint, parameter)) {
            _ = UI.Button(StyleActionString(m_RemoveText, isFeatureSearch), () => {
                result = Execute(blueprint, parameter);
            });
            UI.Label(" ");
        } else if (isFeatureSearch) {
            UI.Label(m_UnitDoesNotHaveThisFactText.Red().Bold());
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

    [LocalizedString("ToyBox_Infrastructure_Blueprints_BlueprintActions_RemoveMechanicEntityFactBA_RemoveText", "Remove")]
    private static partial string m_RemoveText { get; }
    [LocalizedString("ToyBox_Infrastructure_Blueprints_BlueprintActions_RemoveMechanicEntityFactBA_Name", "Remove Fact")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Infrastructure_Blueprints_BlueprintActions_RemoveMechanicEntityFactBA_Description", "Removes the specified BlueprintMechanicEntityFact from the chosen unit.")]
    public override partial string Description { get; }
    [LocalizedString("ToyBox_Infrastructure_Blueprints_BlueprintActions_RemoveMechanicEntityFactBA_UnitDoesNotHaveThisFactText", "Unit does not have this Fact")]
    private static partial string m_UnitDoesNotHaveThisFactText { get; }
}
