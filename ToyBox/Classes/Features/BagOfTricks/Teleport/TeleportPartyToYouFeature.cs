using Kingmaker;
using Kingmaker.Cheats;
using Kingmaker.GameModes;
using ToyBox.Classes.Infrastructure.Features;

namespace ToyBox.Features.BagOfTricks.Teleport;

[IsTested]
public partial class TeleportPartyToYouFeature : FeatureWithBindableAction {
    [LocalizedString("ToyBox_Features_BagOfTricks_Teleport_TeleportPartyToYouFeature_Name", "Teleport Party To You")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_Teleport_TeleportPartyToYouFeature_Description", "Teleports your party members and pets to the main character unit.")]
    public override partial string Description { get; }
    public override bool CanExecute(ActionParameter parameter) {
        return IsInGame();
    }
    public override void ExecuteAction(ActionParameter parameter) {
        if (CanExecute(parameter) && (Game.Instance.CurrentMode == GameModeType.Default || Game.Instance.CurrentMode == GameModeType.Pause)) {
            var position = Game.Instance.Player.MainCharacterEntity.Position;
            var units = Game.Instance.Player.m_PartyAndPets ?? [];
            LogExecution(position, units);
            CheatsTransfer.LocalTeleport(position, units);
        }
    }
}
