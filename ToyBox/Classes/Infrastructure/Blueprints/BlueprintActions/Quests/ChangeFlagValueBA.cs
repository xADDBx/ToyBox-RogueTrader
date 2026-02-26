using Kingmaker;
using Kingmaker.Blueprints;
using ToyBox.Infrastructure.Utilities;

namespace ToyBox.Infrastructure.Blueprints.BlueprintActions;

[IsTested]
public partial class ChangeFlagValueBA : BlueprintActionFeature, IBlueprintAction<BlueprintUnlockableFlag> {

    public bool CanExecute(BlueprintUnlockableFlag blueprint, ActionParameter parameter) {
        return IsInGame() && blueprint.IsUnlocked;
    }
    public bool Execute(BlueprintUnlockableFlag blueprint, ActionParameter parameter) {
        LogExecution(blueprint, parameter);
        blueprint.Value += parameter.IntParam;
        return true;
    }
    public bool? OnGui(BlueprintUnlockableFlag blueprint, bool isFeatureSearch, ActionParameter parameter) {
        bool? result = null;
        if (CanExecute(blueprint, parameter)) {
            using (HorizontalScope()) {
                _ = UI.Button(StyleActionString("<", isFeatureSearch), () => {
                    result = Execute(blueprint, new(-parameter.IntParam));
                });
                UI.Label(StyleActionString($" {Game.Instance.Player.UnlockableFlags.GetFlagValue(blueprint)} ".Bold().Orange(), isFeatureSearch));
                _ = UI.Button(StyleActionString(">", isFeatureSearch), () => {
                    result = Execute(blueprint, parameter);
                });
            }
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
            _ = OnGui(bp!, true, default);
        }
    }
    [LocalizedString("ToyBox_Infrastructure_Blueprints_BlueprintActions_ChangeFlagValueBA_Name", "Modify Flag Value")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Infrastructure_Blueprints_BlueprintActions_ChangeFlagValueBA_Description", "Increases or decreases the value of the specified BlueprintUnlockableFlag.")]
    public override partial string Description { get; }
    [LocalizedString("ToyBox_Infrastructure_Blueprints_BlueprintActions_ChangeFlagValueBA_FlagIsNotUnlockedText", "Flag is not unlocked")]
    private static partial string m_FlagIsNotUnlockedText { get; }
}
