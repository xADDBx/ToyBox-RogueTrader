using Kingmaker;
using Kingmaker.Cheats;
using Kingmaker.GameModes;

namespace ToyBox.Features.BagOfTricks.Teleport;
[NeedsTesting]
public partial class TeleportPartyToYouFeature : FeatureWithBindableAction {
    [LocalizedString("ToyBox_Features_BagOfTricks_Teleport_TeleportPartyToYouFeature_Name", "Teleport Party To You")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_Teleport_TeleportPartyToYouFeature_Description", "Teleports your party members and pets to the main character unit.")]
    public override partial string Description { get; }

    public override void ExecuteAction(params object[] parameter) {
        if (IsInGame() && (Game.Instance.CurrentMode == GameModeType.Default || Game.Instance.CurrentMode == GameModeType.Pause)) {
            var position = Game.Instance.Player.MainCharacterEntity.Position;
            var units = Game.Instance.Player.m_PartyAndPets ?? [];
            LogExecution(position, units);
            CheatsTransfer.LocalTeleport(position, units);
        }
    }
}
