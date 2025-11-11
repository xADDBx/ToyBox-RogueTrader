using Kingmaker;
using Kingmaker.Blueprints.Items;

namespace ToyBox.Features.BagOfTricks.Common;

public partial class GiveAllItemsFeature : FeatureWithBindableAction {
    [LocalizedString("ToyBox_Features_BagOfTricks_Common_GiveAllItemsFeature_Name", "Give All Items")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_Common_GiveAllItemsFeature_Description", "Adds 1 of each BlueprintItem to the player inventory.")]
    public override partial string Description { get; }
    public override void ExecuteAction(params object[] parameter) {
        _ = BPLoader.GetBlueprintsOfType<BlueprintItem>(bps => {
            LogExecution(parameter);
            if (IsInGame()) {
                foreach (var bp in bps) {
                    _ = Game.Instance.Player.Inventory.Add(bp);
                }
            }
        });
    }
}
