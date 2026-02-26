using Kingmaker;
using Kingmaker.Cheats;
using Kingmaker.GameModes;
using Kingmaker.UI.Selection;
using ToyBox.Classes.Infrastructure.Features;

namespace ToyBox.Features.BagOfTricks.Teleport;

[IsTested]
public partial class TeleportSelectedToCursorFeature : FeatureWithBindableAction {
    [LocalizedString("ToyBox_Features_BagOfTricks_Teleport_TeleportSelectedToCursorFeature_Name", "Teleport Selected Characters To Cursor")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_Teleport_TeleportSelectedToCursorFeature_Description", "Teleports the selected units to the position your mouse points at.")]
    public override partial string Description { get; }
    public override bool CanExecute(ActionParameter parameter) {
        return IsInGame() && (Game.Instance.CurrentMode == GameModeType.Default || Game.Instance.CurrentMode == GameModeType.Pause);
    }
    public override void ExecuteAction(ActionParameter parameter) {
        if (CanExecute(parameter)) {
            var position = GetCursorPositionInWorld();
            var units = SelectionManagerBase.Instance.SelectedUnits ?? [];
            LogExecution(position, units);
            CheatsTransfer.LocalTeleport(position, units);
        }
    }
}
