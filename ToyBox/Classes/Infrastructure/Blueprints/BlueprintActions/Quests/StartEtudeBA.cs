using Kingmaker;
using Kingmaker.AreaLogic.Etudes;
using ToyBox.Infrastructure.Utilities;

namespace ToyBox.Infrastructure.Blueprints.BlueprintActions;

[IsTested]
public partial class StartEtudeBA : BlueprintActionFeature, IBlueprintAction<BlueprintEtude> {
    public bool CanExecute(BlueprintEtude blueprint, ActionParameter parameter) {
        return IsInGame() && Game.Instance.Player.EtudesSystem.EtudeIsNotStarted(blueprint);
    }
    public bool Execute(BlueprintEtude blueprint, ActionParameter parameter) {
        LogExecution(blueprint);
        Game.Instance.Player.EtudesSystem.StartEtude(blueprint);
        return true;
    }
    public bool? OnGui(BlueprintEtude blueprint, bool isFeatureSearch, ActionParameter parameter) {
        bool? result = null;
        if (CanExecute(blueprint, parameter)) {
            _ = UI.Button(StyleActionString(m_StartText, isFeatureSearch), () => {
                result = Execute(blueprint, parameter);
            });
        } else if (isFeatureSearch) {
            if (IsInGame()) {
                UI.Label(m_EtudeIsAlreadyStartedOrCompleted.Red().Bold());
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
            _ = OnGui(bp!, true, default);
        }
    }
    [LocalizedString("ToyBox_Infrastructure_Blueprints_BlueprintActions_StartEtudeBA_Name", "Start Etude")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Infrastructure_Blueprints_BlueprintActions_StartEtudeBA_Description", "Starts the specified BlueprintEtude.")]
    public override partial string Description { get; }
    [LocalizedString("ToyBox_Infrastructure_Blueprints_BlueprintActions_StartEtudeBA_StartText", "Start")]
    private static partial string m_StartText { get; }
    [LocalizedString("ToyBox_Infrastructure_Blueprints_BlueprintActions_StartEtudeBA_EtudeIsAlreadyStartedOrCompleted", "Etude is already started or completed")]
    private static partial string m_EtudeIsAlreadyStartedOrCompleted { get; }
}
