using Kingmaker;
using Kingmaker.UnitLogic;
using ToyBox.Infrastructure.Utilities;

namespace ToyBox.Features.BagOfTricks.QualityOfLife;

public partial class FixIncorrectMainCharacterFeature : FeatureWithAction {
    [LocalizedString("ToyBox_Features_BagOfTricks_QualityOfLife_FixIncorrectMainCharacterFeature_Name", "Fix Incorrect Main Character")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_QualityOfLife_FixIncorrectMainCharacterFeature_Description", "Certain situations might cause the game to incorrectly assume someone else is the main character. This tries to restore the original main character.")]
    public override partial string Description { get; }

    public override void ExecuteAction(params object[] parameter) {
        if (IsInGame()) {
            Helpers.LogExecution(this, parameter);
            var probablyPlayer = Game.Instance.Player.Party?.Where(x => !x.IsCustomCompanion() && !x.IsStoryCompanion()).ToList();
            if (probablyPlayer is { Count: 1 }) {
                var newMainCharacter = probablyPlayer[0];
                Warn($"Promoting {newMainCharacter.CharacterName} to original main character!");
                Game.Instance.Player.MainCharacterOriginal = new(newMainCharacter);
            }
        }
    }

    public override void OnGui() {
        using (HorizontalScope()) {
            if (UI.Button(Name)) {
                ExecuteAction();
            }
            Space(10);
            UI.Label(Description.Green());
        }
    }
}
