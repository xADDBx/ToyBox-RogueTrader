using Kingmaker.Blueprints.Area;
using Kingmaker.Cheats;
using ToyBox.Infrastructure.Utilities;

namespace ToyBox.Infrastructure.Blueprints.BlueprintActions;
[NeedsTesting]
public partial class LoadAreaPresetBA : BlueprintActionFeature, IBlueprintAction<BlueprintAreaPreset> {
    public bool CanExecute(BlueprintAreaPreset blueprint, params object[] parameter) {
        return true;
    }

    private bool Execute(BlueprintAreaPreset blueprint, params object[] parameter) {
        LogExecution(blueprint, parameter);
        CheatsTransfer.StartNewGame(blueprint);
        return true;
    }
    public bool? OnGui(BlueprintAreaPreset blueprint, bool isFeatureSearch, params object[] parameter) {
        bool? result = null;
        if (CanExecute(blueprint, parameter)) {
            _ = UI.Button(StyleActionString(m_LoadPresetText, isFeatureSearch), () => {
                result = Execute(blueprint, parameter);
            });
        }
        return result;
    }
    public bool GetContext(out BlueprintAreaPreset? context) {
        return ContextProvider.Blueprint(out context);
    }

    public override void OnGui() {
        if (GetContext(out var bp)) {
            _ = OnGui(bp!, true);
        }
    }
    [LocalizedString("ToyBox_Infrastructure_Blueprints_BlueprintActions_LoadAreaPresetBA_LoadPresetText", "Load Preset")]
    private static partial string m_LoadPresetText { get; }
    [LocalizedString("ToyBox_Infrastructure_Blueprints_BlueprintActions_LoadAreaPresetBA_Name", "Load Area Preset")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Infrastructure_Blueprints_BlueprintActions_LoadAreaPresetBA_Description", "Loads a specified BlueprintAreaPreset.")]
    public override partial string Description { get; }
}
