using Kingmaker.AreaLogic.Cutscenes;
using Kingmaker.Designers.EventConditionActionSystem.ContextData;
using Kingmaker.ElementsSystem.ContextData;
using ToyBox.Infrastructure.Utilities;

namespace ToyBox.Infrastructure.Blueprints.BlueprintActions;
public partial class PlayCutsceneBA : BlueprintActionFeature, IBlueprintAction<Cutscene> {

    public bool CanExecute(Cutscene blueprint, params object[] parameter) {
        return IsInGame();
    }

    private bool Execute(Cutscene blueprint) {
        LogExecution(blueprint);
        ToggleModWindow();

        var cutscenePlayerData = CutscenePlayerData.Queue.FirstOrDefault(c => c.PlayActionId == blueprint.name);
        if (cutscenePlayerData != null) {
            cutscenePlayerData.PreventDestruction = true;
            cutscenePlayerData.Stop();
            cutscenePlayerData.PreventDestruction = false;
        }
        var state = ContextData<SpawnedUnitData>.Current?.State;
        CutscenePlayerView.Play(blueprint, null, true, state).PlayerData.PlayActionId = blueprint.name;

        return true;
    }
    public bool? OnGui(Cutscene blueprint, bool isFeatureSearch, params object[] parameter) {
        bool? result = null;
        if (CanExecute(blueprint)) {
            _ = UI.Button(StyleActionString(m_PlayText, isFeatureSearch), () => {
                result = Execute(blueprint);
            });
        } else if (isFeatureSearch) {
            UI.Label(SharedStrings.ThisCannotBeUsedFromTheMainMenu.Red().Bold());
        }
        return result;
    }

    public bool GetContext(out Cutscene? context) {
        return ContextProvider.Blueprint(out context);
    }

    public override void OnGui() {
        if (GetContext(out var bp)) {
            _ = OnGui(bp!, true);
        }
    }
    [LocalizedString("ToyBox_Infrastructure_Blueprints_BlueprintActions_PlayCutsceneBA_Name", "Play Cutscene")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Infrastructure_Blueprints_BlueprintActions_PlayCutsceneBA_Description", "Plays the specified Cutscene.")]
    public override partial string Description { get; }
    [LocalizedString("ToyBox_Infrastructure_Blueprints_BlueprintActions_PlayCutsceneBA_PlayText", "Play")]
    private static partial string m_PlayText { get; }
}
