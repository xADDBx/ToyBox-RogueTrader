using Kingmaker;
using Kingmaker.Controllers.MapObjects;
using Kingmaker.GameModes;
using Kingmaker.Utility.DotNetExtensions;

namespace ToyBox.Features.BagOfTricks.Common;

public partial class RerollPerceptionFeature : FeatureWithBindableAction {
    [LocalizedString("ToyBox_Features_BagOfTricks_Common_RerollPerceptionFeature_Name", "Reroll Perception")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_Common_RerollPerceptionFeature_Description", "Resets the saved results of awareness rolls on map objects.")]
    public override partial string Description { get; }
    public override void ExecuteAction(params object[] parameter) {
        if (IsInGame()) {
            LogExecution(parameter);
            var objects = Game.Instance.State.MapObjects;
            foreach (var mo in objects) {
                mo.LastAwarenessRollRank.Clear();
            }
            var ac = GameModesFactory.AllControllers.Select(c => c.Controller).OfType<PartyAwarenessController>().FirstOrDefault();
            ac?.m_ForceUpdateCharacterMap.AddRange(Game.Instance.Player.PartyAndPets);
        }
    }
}
