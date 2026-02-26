using Kingmaker;
using Kingmaker.Blueprints.Items;
using ToyBox.Classes.Infrastructure.Features;
using ToyBox.Infrastructure.Utilities;

namespace ToyBox.Infrastructure.Blueprints.BlueprintActions;

public partial class RemoveItemBA : BlueprintActionFeature, IBlueprintAction<BlueprintItem> {
    public bool CanExecute(BlueprintItem blueprint, ActionParameter parameter) {
        return IsInGame() && Game.Instance.Player.Inventory.Contains(blueprint);
    }

    public bool Execute(BlueprintItem blueprint, ActionParameter parameter) {
        LogExecution(blueprint, parameter);
        Game.Instance.Player.Inventory.Remove(blueprint, parameter.IntParam);
        return true;
    }
    public bool? OnGui(BlueprintItem blueprint, bool isFeatureSearch, ActionParameter parameter) {
        bool? result = null;
        if (CanExecute(blueprint, parameter)) {
            _ = UI.Button(StyleActionString(m_RemoveText.Format(parameter.IntParam), isFeatureSearch), () => {
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

    [LocalizedString("ToyBox_Infrastructure_Blueprints_BlueprintActions_RemoveItemBA_Remove_x", "Remove {0} Items")]
    private static partial string m_RemoveText { get; }
    [LocalizedString("ToyBox_Infrastructure_Blueprints_BlueprintActions_RemoveItemBA_Name", "Remove Item")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Infrastructure_Blueprints_BlueprintActions_RemoveItemBA_Description", "Removes the specified BlueprintItem from your inventory.")]
    public override partial string Description { get; }
}
