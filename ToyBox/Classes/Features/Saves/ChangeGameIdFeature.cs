using Kingmaker;

namespace ToyBox.Features.Saves;

public partial class ChangeGameIdFeature : Feature {
    [LocalizedString("ToyBox_Features_Saves_ChangeSaveIdFeature_Name", "Change Game Id")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_Saves_ChangeSaveIdFeature_Description", "Allows changing the game id of the current session, which causes saves after this to be grouped under a different header.")]
    public override partial string Description { get; }

    public override void OnGui() {
        using (HorizontalScope()) {
            UI.Label("Game Id: ");
            var curId = Game.Instance?.Player?.GameId;
            if (curId == null) {
                UI.Label(m_N_ALocalizedText);
            } else {
                UI.EditableLabel(curId, "GameId", s => {
                    Game.Instance!.Player.GameId = s;
                });
            }
        }
    }

    [LocalizedString("ToyBox_Features_Saves_ChangeSaveIdFeature_m_N_ALocalizedText", "N/A")]
    private static partial string m_N_ALocalizedText { get; }
}
