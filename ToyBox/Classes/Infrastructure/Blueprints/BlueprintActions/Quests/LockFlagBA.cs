using Kingmaker.Blueprints;
using ToyBox.Infrastructure.Utilities;

namespace ToyBox.Infrastructure.Blueprints.BlueprintActions;

public partial class LockFlagBA : BlueprintActionFeature, IBlueprintAction<BlueprintUnlockableFlag> {

    public bool CanExecute(BlueprintUnlockableFlag blueprint, params object[] parameter) {
        return IsInGame() && blueprint.IsUnlocked;
    }
    private bool Execute(BlueprintUnlockableFlag blueprint) {
        LogExecution(blueprint);
        blueprint.Lock();
        return true;
    }
    public bool? OnGui(BlueprintUnlockableFlag blueprint, bool isFeatureSearch, params object[] parameter) {
        bool? result = null;
        if (CanExecute(blueprint)) {
            _ = UI.Button(StyleActionString(m_LockText, isFeatureSearch), () => {
                result = Execute(blueprint);
            });
        } else if (isFeatureSearch) {
            if (IsInGame()) {
                UI.Label(m_FlagIsNotUnlockedText.Red().Bold());
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
            _ = OnGui(bp!, true);
        }
    }
    [LocalizedString("ToyBox_Infrastructure_Blueprints_BlueprintActions_LockFlagBA_Name", "Lock Flag")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Infrastructure_Blueprints_BlueprintActions_LockFlagBA_Description", "Locks the specified BlueprintUnlockableFlag.")]
    public override partial string Description { get; }
    [LocalizedString("ToyBox_Infrastructure_Blueprints_BlueprintActions_LockFlagBA_LockText", "Lock")]
    private static partial string m_LockText { get; }
    [LocalizedString("ToyBox_Infrastructure_Blueprints_BlueprintActions_LockFlagBA_FlagIsNotUnlockedText", "Flag is not unlocked")]
    private static partial string m_FlagIsNotUnlockedText { get; }
}
