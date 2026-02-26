using Kingmaker;
using Kingmaker.Blueprints.Items;
using ToyBox.Infrastructure.Utilities;

namespace ToyBox.Infrastructure.Blueprints.BlueprintActions;

public partial class AddItemBA : BlueprintActionFeature, IBlueprintAction<BlueprintItem> {
    public bool CanExecute(BlueprintItem blueprint, ActionParameter parameter) {
        return IsInGame();
    }

    public bool Execute(BlueprintItem blueprint, ActionParameter parameter) {
        LogExecution(blueprint, parameter);
        Game.Instance.Player.Inventory.Add(blueprint, parameter.IntParam);
        return true;
    }
    public bool? OnGui(BlueprintItem blueprint, bool isFeatureSearch, ActionParameter parameter) {
        bool? result = null;
        if (CanExecute(blueprint, parameter)) {
            _ = UI.Button(StyleActionString(m_AddText.Format(parameter.IntParam), isFeatureSearch), () => {
                result = Execute(blueprint, parameter);
            });
        } else if (isFeatureSearch) {
            UI.Label(SharedStrings.ThisCannotBeUsedFromTheMainMenu.Red().Bold());
        }
        return result;
    }

    public bool GetContext(out BlueprintItem? context) {
        return ContextProvider.Blueprint(out context);
    }

    public override void OnGui() {
        if (GetContext(out var bp)) {
            _ = OnGui(bp!, true, default);
        }
    }

    [LocalizedString("ToyBox_Infrastructure_Blueprints_BlueprintActions_AddItemBA_Add_x", "Add {0} Items")]
    private static partial string m_AddText { get; }
    [LocalizedString("ToyBox_Infrastructure_Blueprints_BlueprintActions_AddItemBA_Name", "Add Item")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Infrastructure_Blueprints_BlueprintActions_AddItemBA_Description", "Adds the specified BlueprintItem to your inventory.")]
    public override partial string Description { get; }
}
