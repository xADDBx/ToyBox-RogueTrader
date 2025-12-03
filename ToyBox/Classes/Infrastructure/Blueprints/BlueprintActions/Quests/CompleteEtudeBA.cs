using Kingmaker;
using Kingmaker.AreaLogic.Etudes;
using ToyBox.Infrastructure.Utilities;

namespace ToyBox.Infrastructure.Blueprints.BlueprintActions;

[IsTested]
public partial class CompleteEtudeBA : BlueprintActionFeature, IBlueprintAction<BlueprintEtude> {

    public bool CanExecute(BlueprintEtude blueprint, params object[] parameter) {
        return IsInGame() && !Game.Instance.Player.EtudesSystem.EtudeIsNotStarted(blueprint) && !Game.Instance.Player.EtudesSystem.EtudeIsCompleted(blueprint);
    }
    private bool Execute(BlueprintEtude blueprint) {
        LogExecution(blueprint);
        Game.Instance.Player.EtudesSystem.MarkEtudeCompleted(blueprint);
        return true;
    }
    public bool? OnGui(BlueprintEtude blueprint, bool isFeatureSearch, params object[] parameter) {
        bool? result = null;
        if (CanExecute(blueprint)) {
            _ = UI.Button(StyleActionString(m_CompleteText, isFeatureSearch), () => {
                result = Execute(blueprint);
            });
        } else if (isFeatureSearch) {
            if (IsInGame()) {
                UI.Label(m_EtudeIsNotStartedText.Red().Bold());
            } else {
                UI.Label(SharedStrings.ThisCannotBeUsedFromTheMainMenu.Red().Bold());
            }
        }
        return result;
    }

    public bool GetContext(out BlueprintEtude? context) {
        return ContextProvider.Blueprint(out context);
    }

    public override void OnGui() {
        if (GetContext(out var bp)) {
            _ = OnGui(bp!, true);
        }
    }
    [LocalizedString("ToyBox_Infrastructure_Blueprints_BlueprintActions_CompleteEtudeBA_Name", "Complete Etude")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Infrastructure_Blueprints_BlueprintActions_CompleteEtudeBA_Description", "Complete the specified BlueprintEtude. A failed Etude is often used to mark a failed quest state.")]
    public override partial string Description { get; }
    [LocalizedString("ToyBox_Infrastructure_Blueprints_BlueprintActions_CompleteEtudeBA_CompleteText", "Complete")]
    private static partial string m_CompleteText { get; }
    [LocalizedString("ToyBox_Infrastructure_Blueprints_BlueprintActions_CompleteEtudeBA_EtudeIsNotStartedText", "Etude is not started")]
    private static partial string m_EtudeIsNotStartedText { get; }
}
