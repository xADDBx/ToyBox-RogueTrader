using Kingmaker;
using Kingmaker.Cheats;
using Kingmaker.GameModes;
using Kingmaker.UI.Selection;

namespace ToyBox.Features.BagOfTricks.Teleport;

public partial class TeleportSelectedToCursorFeature : FeatureWithBindableAction {
    [LocalizedString("ToyBox_Features_BagOfTricks_Teleport_TeleportSelectedToCursorFeature_Name", "Teleport Selected Characters To Cursor")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_Teleport_TeleportSelectedToCursorFeature_Description", "Teleports the selected units to the position your mouse points at.")]
    public override partial string Description { get; }

    public override void ExecuteAction(params object[] parameter) {
        if (IsInGame() && (Game.Instance.CurrentMode == GameModeType.Default || Game.Instance.CurrentMode == GameModeType.Pause)) {
            var position = GetCursorPositionInWorld();
            var units = SelectionManagerBase.Instance.SelectedUnits ?? [];
            LogExecution(position, units);
            CheatsTransfer.LocalTeleport(position, units);
        }
    }
}
