using Kingmaker.Blueprints.Items.Equipment;
using Kingmaker.EntitySystem.Entities;
using ToyBox.Infrastructure.Utilities;

namespace ToyBox.Infrastructure.Blueprints.BlueprintActions;

public partial class AddMechadendriteBA : BlueprintActionFeature, IBlueprintAction<BlueprintItemMechadendrite>, INeedContextFeature<BaseUnitEntity> {
    [LocalizedString("ToyBox_Infrastructure_Blueprints_BlueprintActions_AddMechadendriteBA_Name", "Add Mechadendrite")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Infrastructure_Blueprints_BlueprintActions_AddMechadendriteBA_Description", "Adds the specified mechadendrite to the specified unit.")]
    public override partial string Description { get; }

    public bool CanExecute(BlueprintItemMechadendrite blueprint, params object[] parameter) {
        if (parameter.Length > 0 && parameter[0] is BaseUnitEntity unit) {
            return !unit.Body.Mechadendrites.Any(slot => slot?.Item?.Blueprint == blueprint);
        } else {
            return false;
        }
    }
    private bool Execute(BlueprintItemMechadendrite blueprint, params object[] parameter) {
        LogExecution(blueprint, parameter);
        var ch = (BaseUnitEntity)parameter[0];
        var slot = new Kingmaker.Items.Slots.EquipmentSlot<BlueprintItemMechadendrite>(ch);
        ch.Body.Mechadendrites.Add(slot);
        ch.Body.TryInsertItem(blueprint, slot);
        ch.View.Mechadendrites.Add(slot.Item);
        return true;
    }
    public bool? OnGui(BlueprintItemMechadendrite blueprint, bool isFeatureSearch, params object[] parameter) {
        bool? result = null;
        if (CanExecute(blueprint, parameter)) {
            _ = UI.Button(StyleActionString(m_AddLocalizedText, isFeatureSearch), () => {
                result = Execute(blueprint, parameter);
            });
        } else if (isFeatureSearch) {
            UI.Label(m_UnitAlreadyHasThisMechadendriteLocalizedText.Red().Bold());
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

    [LocalizedString("ToyBox_Infrastructure_Blueprints_BlueprintActions_AddMechadendriteBA_m_AddLocalizedText", "Equip Mechadendrite")]
    private static partial string m_AddLocalizedText { get; }
    [LocalizedString("ToyBox_Infrastructure_Blueprints_BlueprintActions_AddMechadendriteBA_m_UnitAlreadyHasThisMechadendriteLocalizedText", "Unit already has this Mechadendrite")]
    private static partial string m_UnitAlreadyHasThisMechadendriteLocalizedText { get; }
}
