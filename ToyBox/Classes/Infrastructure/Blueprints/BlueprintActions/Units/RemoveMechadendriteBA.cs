using Kingmaker;
using Kingmaker.Blueprints.Items.Equipment;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Utility.DotNetExtensions;
using ToyBox.Infrastructure.Utilities;

namespace ToyBox.Infrastructure.Blueprints.BlueprintActions;

[IsTested]
public partial class RemoveMechadendriteBA : BlueprintActionFeature, IBlueprintAction<BlueprintItemMechadendrite>, INeedContextFeature<BaseUnitEntity> {
    [LocalizedString("ToyBox_Infrastructure_Blueprints_BlueprintActions_RemoveMechadendriteBA_Name", "Remove Mechadendrite")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Infrastructure_Blueprints_BlueprintActions_RemoveMechadendriteBA_Description", "Removes the specified mechadendrite from the specified unit.")]
    public override partial string Description { get; }

    public bool CanExecute(BlueprintItemMechadendrite blueprint, params object[] parameter) {
        if (parameter.Length > 0 && parameter[0] is BaseUnitEntity unit) {
            return unit.Body.Mechadendrites.Any(slot => slot?.Item?.Blueprint == blueprint);
        } else {
            return false;
        }
    }
    private bool Execute(BlueprintItemMechadendrite blueprint, params object[] parameter) {
        LogExecution(blueprint, parameter);
        var ch = (BaseUnitEntity)parameter[0];
        var slot = ch.Body.Mechadendrites.First(slot => slot?.Item?.Blueprint == blueprint);
        var item = slot.Item;
        slot.RemoveItem(true, true);
        ch.Body.Mechadendrites.Remove(slot);
        ch.View.Mechadendrites.Remove(item);
        try {
            Game.Instance.Player.Inventory.Remove(item);
        } catch (Exception ex) {
            Log($"Exception while removing Mechadendrite Entity from inventory:\n{ex}");
        }
        return true;
    }
    public bool? OnGui(BlueprintItemMechadendrite blueprint, bool isFeatureSearch, params object[] parameter) {
        bool? result = null;
        if (CanExecute(blueprint, parameter)) {
            _ = UI.Button(StyleActionString(m_RemoveLocalizedText, isFeatureSearch), () => {
                result = Execute(blueprint, parameter);
            });
        } else if (isFeatureSearch) {
            UI.Label(m_UnitDoesn_tHaveThisMechadendriteLocalizedText.Red().Bold());
        }
        return result;
    }
    public override void OnGui() {
        if (GetContext(out BlueprintItemMechadendrite? bp) && GetContext(out BaseUnitEntity? unit)) {
            _ = OnGui(bp!, true, unit!);
        }
    }
    public bool GetContext(out BaseUnitEntity? context) {
        return ContextProvider.BaseUnitEntity(out context);
    }

    public bool GetContext(out BlueprintItemMechadendrite? context) {
        return ContextProvider.Blueprint(out context);
    }

    [LocalizedString("ToyBox_Infrastructure_Blueprints_BlueprintActions_RemoveMechadendriteBA_m_RemoveLocalizedText", "Unequip Mechadendrite")]
    private static partial string m_RemoveLocalizedText { get; }
    [LocalizedString("ToyBox_Infrastructure_Blueprints_BlueprintActions_RemoveMechadendriteBA_m_UnitDoesn_tHaveThisMechadendriteLocalizedText", "Unit doesn't have this Mechadendrite")]
    private static partial string m_UnitDoesn_tHaveThisMechadendriteLocalizedText { get; }
}
