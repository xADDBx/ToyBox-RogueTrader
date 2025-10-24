using Kingmaker;
using Kingmaker.Blueprints.Quests;
using ToyBox.Infrastructure.Utilities;

namespace ToyBox.Infrastructure.Blueprints.BlueprintActions;
[NeedsTesting]
public partial class StartQuestBA : BlueprintActionFeature, IBlueprintAction<BlueprintQuest> {

    public bool CanExecute(BlueprintQuest blueprint, params object[] parameter) {
        return IsInGame() && Game.Instance.Player.QuestBook.GetQuest(blueprint) == null;
    }
    private bool Execute(BlueprintQuest blueprint) {
        LogExecution(blueprint);
        Game.Instance.Player.QuestBook.GiveObjective(blueprint.Objectives.First());
        return true;
    }
    public bool? OnGui(BlueprintQuest blueprint, bool isFeatureSearch, params object[] parameter) {
        bool? result = null;
        if (CanExecute(blueprint)) {
            _ = UI.Button(StyleActionString(m_StartText, isFeatureSearch), () => {
                result = Execute(blueprint);
            });
        } else if (isFeatureSearch) {
            if (IsInGame()) {
                UI.Label(m_QuestAlreadyStartedText.Red().Bold());
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
    [LocalizedString("ToyBox_Infrastructure_Blueprints_BlueprintActions_StartQuestBA_Name", "Start Quest")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Infrastructure_Blueprints_BlueprintActions_StartQuestBA_Description", "Starts the specified BlueprintQuest by starting its first objective.")]
    public override partial string Description { get; }
    [LocalizedString("ToyBox_Infrastructure_Blueprints_BlueprintActions_StartQuestBA_StartText", "Start")]
    private static partial string m_StartText { get; }
    [LocalizedString("ToyBox_Infrastructure_Blueprints_BlueprintActions_StartQuestBA_QuestAlreadyStartedText", "Quest already started")]
    private static partial string m_QuestAlreadyStartedText { get; }
}
