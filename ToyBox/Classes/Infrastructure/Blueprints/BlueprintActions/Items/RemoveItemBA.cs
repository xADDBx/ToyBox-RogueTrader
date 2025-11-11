using Kingmaker;
using Kingmaker.Blueprints.Items;
using ToyBox.Infrastructure.Utilities;

namespace ToyBox.Infrastructure.Blueprints.BlueprintActions;

public partial class RemoveItemBA : BlueprintActionFeature, IBlueprintAction<BlueprintItem> {
    public bool CanExecute(BlueprintItem blueprint, params object[] parameter) {
        return IsInGame() && Game.Instance.Player.Inventory.Contains(blueprint);
    }

    private bool Execute(BlueprintItem blueprint, int count) {
        LogExecution(blueprint, count);
        Game.Instance.Player.Inventory.Remove(blueprint, count);
        return true;
    }
    public bool? OnGui(BlueprintItem blueprint, bool isFeatureSearch, params object[] parameter) {
        bool? result = null;
        if (CanExecute(blueprint, parameter)) {
            var count = 1;
            if (parameter.Length > 0 && parameter[0] is int tmpCount) {
                count = tmpCount;
            }
            _ = UI.Button(StyleActionString(m_RemoveText + $" {count}", isFeatureSearch), () => {
                result = Execute(blueprint, count);
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
            _ = OnGui(bp!, true);
        }
    }

    [LocalizedString("ToyBox_Infrastructure_Blueprints_BlueprintActions_RemoveItemBA_Remove_x", "Remove")]
    private static partial string m_RemoveText { get; }
    [LocalizedString("ToyBox_Infrastructure_Blueprints_BlueprintActions_RemoveItemBA_Name", "Remove Item")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Infrastructure_Blueprints_BlueprintActions_RemoveItemBA_Description", "Removes the specified BlueprintItem from your inventory.")]
    public override partial string Description { get; }
}
