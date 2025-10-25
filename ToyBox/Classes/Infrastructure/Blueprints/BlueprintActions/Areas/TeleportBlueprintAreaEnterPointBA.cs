using Kingmaker;
using Kingmaker.Blueprints.Area;
using Kingmaker.EntitySystem.Persistence;
using ToyBox.Infrastructure.Utilities;

namespace ToyBox.Infrastructure.Blueprints.BlueprintActions;
[NeedsTesting]
public partial class TeleportBlueprintAreaEnterPointBA : BlueprintActionFeature, IBlueprintAction<BlueprintAreaEnterPoint> {
    public bool CanExecute(BlueprintAreaEnterPoint blueprint, params object[] parameter) {
        return IsInGame();
    }

    private bool Execute(BlueprintAreaEnterPoint blueprint, params object[] parameter) {
        LogExecution(blueprint, parameter);
        Game.Instance.LoadArea(blueprint, AutoSaveMode.None, null);
        return true;
    }
    public bool? OnGui(BlueprintAreaEnterPoint blueprint, bool isFeatureSearch, params object[] parameter) {
        bool? result = null;
        if (CanExecute(blueprint, parameter)) {
            _ = UI.Button(StyleActionString(m_Teleport, isFeatureSearch), () => {
                result = Execute(blueprint, parameter);
            });
        }
        return result;
    }
    public bool GetContext(out BlueprintAreaEnterPoint? context) {
        return ContextProvider.Blueprint(out context);
    }

    public override void OnGui() {
        if (GetContext(out var bp)) {
            _ = OnGui(bp!, true);
        }
    }
    [LocalizedString("ToyBox_Infrastructure_Blueprints_BlueprintActions_TeleportBlueprintAreaEnterPointBA_TeleportText", "Teleport")]
    private static partial string m_Teleport { get; }
    [LocalizedString("ToyBox_Infrastructure_Blueprints_BlueprintActions_TeleportBlueprintAreaEnterPointBA_Name", "Teleport to BlueprintAreaEnterPoint")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Infrastructure_Blueprints_BlueprintActions_TeleportBlueprintAreaEnterPointBA_Description", "Loads the area of the specified BlueprintAreaEnterPoint.")]
    public override partial string Description { get; }
}
