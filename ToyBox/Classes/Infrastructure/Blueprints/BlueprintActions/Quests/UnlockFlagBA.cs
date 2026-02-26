using Kingmaker.Blueprints;
using ToyBox.Classes.Infrastructure.Features;
using ToyBox.Infrastructure.Utilities;

namespace ToyBox.Infrastructure.Blueprints.BlueprintActions;

[IsTested]
public partial class UnlockFlagBA : BlueprintActionFeature, IBlueprintAction<BlueprintUnlockableFlag> {

    public bool CanExecute(BlueprintUnlockableFlag blueprint, ActionParameter parameter) {
        return IsInGame() && blueprint.IsLocked;
    }
    public bool Execute(BlueprintUnlockableFlag blueprint, ActionParameter parameter) {
        LogExecution(blueprint);
        blueprint.Unlock();
        return true;
    }
    public bool? OnGui(BlueprintUnlockableFlag blueprint, bool isFeatureSearch, ActionParameter parameter) {
        bool? result = null;
        if (CanExecute(blueprint, parameter)) {
            _ = UI.Button(StyleActionString(m_UnlockText, isFeatureSearch), () => {
                result = Execute(blueprint, parameter);
            });
        } else if (isFeatureSearch) {
            if (IsInGame()) {
                UI.Label(m_FlagIsNotLockedText.Red().Bold());
            } else {
                UI.Label(SharedStrings.ThisCannotBeUsedFromTheMainMenu.Red().Bold());
            }
        }
        return result;
    }

    public bool GetContext(out BlueprintUnlockableFlag? context) {
        return ContextProvider.Blueprint(out context);
    }

    public override void OnGui() {
        if (GetContext(out var bp)) {
            _ = OnGui(bp!, true, default);
        }
    }
    [LocalizedString("ToyBox_Infrastructure_Blueprints_BlueprintActions_UnlockFlagBA_Name", "Unlock Flag")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Infrastructure_Blueprints_BlueprintActions_UnlockFlagBA_Description", "Unlocks the specified BlueprintUnlockableFlag.")]
    public override partial string Description { get; }
    [LocalizedString("ToyBox_Infrastructure_Blueprints_BlueprintActions_UnlockFlagBA_UnlockText", "Unlock")]
    private static partial string m_UnlockText { get; }
    [LocalizedString("ToyBox_Infrastructure_Blueprints_BlueprintActions_UnlockFlagBA_FlagIsNotLockedText", "Flag is not locked")]
    private static partial string m_FlagIsNotLockedText { get; }
}
