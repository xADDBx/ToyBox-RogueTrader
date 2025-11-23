using Kingmaker;
using Kingmaker.Cheats;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.GameModes;

namespace ToyBox.Features.BagOfTricks.Teleport;

[IsTested]
public partial class TeleportMainToCursorFeature : FeatureWithBindableAction {
    [LocalizedString("ToyBox_Features_BagOfTricks_Teleport_TeleportMainToCursorFeature_Name", "Teleport Main Character To Cursor")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_Teleport_TeleportMainToCursorFeature_Description", "Teleports your main character unit to the position your mouse points at.")]
    public override partial string Description { get; }

    public override void ExecuteAction(params object[] parameter) {
        if (IsInGame() && (Game.Instance.CurrentMode == GameModeType.Default || Game.Instance.CurrentMode == GameModeType.Pause)) {
            var position = GetCursorPositionInWorld();
            List<BaseUnitEntity> units = [Game.Instance.Player.MainCharacterEntity];
            LogExecution(position, units);
            CheatsTransfer.LocalTeleport(position, units);
        }
    }
}
