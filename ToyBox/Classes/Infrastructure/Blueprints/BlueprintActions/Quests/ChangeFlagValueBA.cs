using Kingmaker;
using Kingmaker.Blueprints;
using ToyBox.Infrastructure.Utilities;

namespace ToyBox.Infrastructure.Blueprints.BlueprintActions;
[NeedsTesting]
public partial class ChangeFlagValueBA : BlueprintActionFeature, IBlueprintAction<BlueprintUnlockableFlag> {

    public bool CanExecute(BlueprintUnlockableFlag blueprint, params object[] parameter) {
        return IsInGame()
            && Game.Instance.Player.UnlockableFlags.IsUnlocked(blueprint);
    }
    private bool ExecuteIncrease(BlueprintUnlockableFlag blueprint, int count) {
        LogExecution(blueprint, count);
        Game.Instance.Player.UnlockableFlags.SetFlagValue(blueprint, Game.Instance.Player.UnlockableFlags.GetFlagValue(blueprint) + count);
        return true;
    }
    private bool ExecuteDecrease(BlueprintUnlockableFlag blueprint, int count) {
        LogExecution(blueprint, -count);
        Game.Instance.Player.UnlockableFlags.SetFlagValue(blueprint, Game.Instance.Player.UnlockableFlags.GetFlagValue(blueprint) - count);
        return true;
    }
    public bool? OnGui(BlueprintUnlockableFlag blueprint, bool isFeatureSearch, params object[] parameter) {
        bool? result = null;
        if (CanExecute(blueprint)) {
            var count = 1;
            if (parameter.Length > 0 && parameter[0] is int tmpCount) {
                count = tmpCount;
            }
            _ = UI.Button(StyleActionString("<", isFeatureSearch), () => {
                result = ExecuteDecrease(blueprint, count);
            });
            UI.Label(StyleActionString($" {Game.Instance.Player.UnlockableFlags.GetFlagValue(blueprint)} ".Bold().Orange(), isFeatureSearch));
            _ = UI.Button(StyleActionString(">", isFeatureSearch), () => {
                result = ExecuteIncrease(blueprint, count);
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
    [LocalizedString("ToyBox_Infrastructure_Blueprints_BlueprintActions_ChangeFlagValueBA_Name", "Modify Flag Value")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Infrastructure_Blueprints_BlueprintActions_ChangeFlagValueBA_Description", "Increases or decreases the value of the specified BlueprintUnlockableFlag.")]
    public override partial string Description { get; }
    [LocalizedString("ToyBox_Infrastructure_Blueprints_BlueprintActions_ChangeFlagValueBA_FlagIsNotUnlockedText", "Flag is not unlocked")]
    private static partial string m_FlagIsNotUnlockedText { get; }
}
