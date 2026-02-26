using Kingmaker;
using Kingmaker.Cheats;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.GameModes;
using ToyBox.Classes.Infrastructure.Features;

namespace ToyBox.Features.BagOfTricks.Teleport;

[IsTested]
public partial class TeleportMainToCursorFeature : FeatureWithBindableAction {
    [LocalizedString("ToyBox_Features_BagOfTricks_Teleport_TeleportMainToCursorFeature_Name", "Teleport Main Character To Cursor")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_Teleport_TeleportMainToCursorFeature_Description", "Teleports your main character unit to the position your mouse points at.")]
    public override partial string Description { get; }
    public override bool CanExecute(ActionParameter parameter) {
        return IsInGame();
    }
    public override void ExecuteAction(ActionParameter parameter) {
        if (CanExecute(parameter) && (Game.Instance.CurrentMode == GameModeType.Default || Game.Instance.CurrentMode == GameModeType.Pause)) {
            var position = GetCursorPositionInWorld();
            List<BaseUnitEntity> units = [Game.Instance.Player.MainCharacterEntity];
            LogExecution(position, units);
            CheatsTransfer.LocalTeleport(position, units);
        }
    }
}
