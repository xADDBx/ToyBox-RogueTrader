using Kingmaker;
using Kingmaker.AreaLogic.QuestSystem;
using Kingmaker.Blueprints.Quests;
using ToyBox.Infrastructure.Utilities;

namespace ToyBox.Infrastructure.Blueprints.BlueprintActions;

[IsTested]
public partial class CompleteQuestObjectiveBA : BlueprintActionFeature, IBlueprintAction<BlueprintQuestObjective> {

    public bool CanExecute(BlueprintQuestObjective blueprint, ActionParameter parameter) {
        return IsInGame() && (Game.Instance.Player.QuestBook.GetQuest(blueprint.Quest)?.TryGetObjective(blueprint)?.State ?? QuestObjectiveState.None) == QuestObjectiveState.Started;
    }
    public bool Execute(BlueprintQuestObjective blueprint, ActionParameter parameter) {
        LogExecution(blueprint);
        Game.Instance.Player.QuestBook.CompleteObjective(blueprint);
        return true;
    }
    public bool? OnGui(BlueprintQuestObjective blueprint, bool isFeatureSearch, ActionParameter parameter) {
        bool? result = null;
        if (CanExecute(blueprint, parameter)) {
            _ = UI.Button(StyleActionString(m_CompleteText, isFeatureSearch), () => {
                result = Execute(blueprint, parameter);
            });
        } else if (isFeatureSearch) {
            if (IsInGame()) {
                UI.Label(m_QuestObjectiveIsNotStartedText.Red().Bold());
            } else {
                UI.Label(SharedStrings.ThisCannotBeUsedFromTheMainMenu.Red().Bold());
            }
        }
        return result;
    }

    public bool GetContext(out BlueprintQuestObjective? context) {
        return ContextProvider.Blueprint(out context);
    }

    public override void OnGui() {
        if (GetContext(out var bp)) {
            _ = OnGui(bp!, true, default);
        }
    }
    [LocalizedString("ToyBox_Infrastructure_Blueprints_BlueprintActions_CompleteQuestObjectiveBA_Name", "Complete Quest Objective")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Infrastructure_Blueprints_BlueprintActions_CompleteQuestObjectiveBA_Description", "Completes the specified BlueprintQuestObjective.")]
    public override partial string Description { get; }
    [LocalizedString("ToyBox_Infrastructure_Blueprints_BlueprintActions_CompleteQuestObjectiveBA_CompleteText", "Complete")]
    private static partial string m_CompleteText { get; }
    [LocalizedString("ToyBox_Infrastructure_Blueprints_BlueprintActions_CompleteQuestObjectiveBA_QuestObjectiveIsNotStartedText", "Quest objective is not started")]
    private static partial string m_QuestObjectiveIsNotStartedText { get; }
}
