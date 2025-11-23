using Kingmaker;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.GameModes;

namespace ToyBox.Features.BagOfTricks.Common;

[IsTested]
public partial class ChangePartyFeature : FeatureWithBindableAction {
    [LocalizedString("ToyBox_Features_BagOfTricks_Common_ChangePartyFeature_Name", "Change Party")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_Common_ChangePartyFeature_Description", "Opens the party member selection screen.")]
    public override partial string Description { get; }

    public override void ExecuteAction(params object[] parameter) {
        if (IsInGame() && (Game.Instance.CurrentMode == GameModeType.Default || Game.Instance.CurrentMode == GameModeType.Pause || Game.Instance.CurrentMode == GameModeType.GlobalMap)) {
            LogExecution(parameter);
            ToggleModWindow();
            new ShowPartySelection() {
                ActionsAfterPartySelection = new(),
                ActionsIfCanceled = new(),
                ShowRemoteCompanions = true,
            }.Run();
        }
    }
}
