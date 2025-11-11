using Kingmaker;
using Kingmaker.AreaLogic.QuestSystem;
using Kingmaker.Blueprints.Quests;
using ToyBox.Infrastructure.Utilities;

namespace ToyBox.Infrastructure.Blueprints.BlueprintActions;

public partial class CompleteQuestBA : BlueprintActionFeature, IBlueprintAction<BlueprintQuest> {

    public bool CanExecute(BlueprintQuest blueprint, params object[] parameter) {
        return IsInGame() && Game.Instance.Player.QuestBook.GetQuest(blueprint)?.State == QuestState.Started;
    }
    private bool Execute(BlueprintQuest blueprint) {
        LogExecution(blueprint);
        foreach (var objective in blueprint.Objectives) {
            Game.Instance.Player.QuestBook.CompleteObjective(objective);
        }
        return true;
    }
    public bool? OnGui(BlueprintQuest blueprint, bool isFeatureSearch, params object[] parameter) {
        bool? result = null;
        if (CanExecute(blueprint)) {
            _ = UI.Button(StyleActionString(m_CompleteText, isFeatureSearch), () => {
                result = Execute(blueprint);
            });
        } else if (isFeatureSearch) {
            if (IsInGame()) {
                UI.Label(m_QuestIsNotStartedText.Red().Bold());
            } else {
                UI.Label(SharedStrings.ThisCannotBeUsedFromTheMainMenu.Red().Bold());
            }
        }
        return result;
    }
    public bool GetContext(out BlueprintQuest? context) {
        return ContextProvider.Blueprint(out context);
    }

    public override void OnGui() {
        if (GetContext(out var bp)) {
            _ = OnGui(bp!, true);
        }
    }
    [LocalizedString("ToyBox_Infrastructure_Blueprints_BlueprintActions_CompleteQuestBA_Name", "Complete Quest")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Infrastructure_Blueprints_BlueprintActions_CompleteQuestBA_Description", "Completes the specified BlueprintQuest by forcing each objective to complete.")]
    public override partial string Description { get; }
    [LocalizedString("ToyBox_Infrastructure_Blueprints_BlueprintActions_CompleteQuestBA_CompleteText", "Complete")]
    private static partial string m_CompleteText { get; }
    [LocalizedString("ToyBox_Infrastructure_Blueprints_BlueprintActions_CompleteQuestBA_QuestIsNotStartedText", "Quest is not started")]
    private static partial string m_QuestIsNotStartedText { get; }
}
