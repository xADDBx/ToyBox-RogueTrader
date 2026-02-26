using Kingmaker;
using Kingmaker.Achievements;
using ToyBox.Classes.Infrastructure.Features;
using ToyBox.Infrastructure.Utilities;

namespace ToyBox.Infrastructure.Blueprints.BlueprintActions;

[IsTested]
public partial class UnlockAchievementBA : BlueprintActionFeature, IBlueprintAction<AchievementData> {

    public bool CanExecute(AchievementData blueprint, ActionParameter parameter) {
        return IsInGame() && (Game.Instance.Player.Achievements.m_Achievements?.Any(ach => !ach.IsUnlocked && ach.Data == blueprint) ?? false);
    }
    public bool Execute(AchievementData blueprint, ActionParameter parameter) {
        LogExecution(blueprint);
        var ach = Game.Instance.Player.Achievements.m_Achievements.First(ach => !ach.IsUnlocked && ach.Data == blueprint);
        ach.IsUnlocked = true;
        ach.NeedCommit = true;
        ach.Manager.OnAchievementUnlocked(ach);
        return true;
    }
    public bool? OnGui(AchievementData blueprint, bool isFeatureSearch, ActionParameter parameter) {
        bool? result = null;
        if (CanExecute(blueprint, parameter)) {
            _ = UI.Button(StyleActionString(m_UnlockText, isFeatureSearch), () => {
                result = Execute(blueprint, parameter);
            });
        } else if (isFeatureSearch) {
            if (IsInGame()) {
                UI.Label(m_AchievementAlreadyUnlockedText.Red().Bold());
            } else {
                UI.Label(SharedStrings.ThisCannotBeUsedFromTheMainMenu.Red().Bold());
            }
        }
        return result;
    }

    public bool GetContext(out AchievementData? context) {
        return ContextProvider.Blueprint(out context);
    }

    public override void OnGui() {
        if (GetContext(out var bp)) {
            _ = OnGui(bp!, true, default);
        }
    }
    [LocalizedString("ToyBox_Infrastructure_Blueprints_BlueprintActions_UnlockAchievementBA_Name", "Unlock Achievement")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Infrastructure_Blueprints_BlueprintActions_UnlockAchievementBA_Description", "Unlocks the specified achievement.")]
    public override partial string Description { get; }
    [LocalizedString("ToyBox_Infrastructure_Blueprints_BlueprintActions_UnlockAchievementBA_UnlockText", "Unlock")]
    private static partial string m_UnlockText { get; }
    [LocalizedString("ToyBox_Infrastructure_Blueprints_BlueprintActions_UnlockAchievementBA_AchievementAlreadyUnlockedText", "Achievement is already unlocked")]
    private static partial string m_AchievementAlreadyUnlockedText { get; }
}
