using Kingmaker.Cheats;

namespace ToyBox.Features.BagOfTricks.Combat;
public partial class RestAllFeature : FeatureWithBindableAction {
    [LocalizedString("ToyBox_Features_BagOfTricks_Combat_RestAllFeature_Name", "Rest All")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_Combat_RestAllFeature_Description", "Revives and heals all characters + restores action points, ability cooldowns and item charges.")]
    public override partial string Description { get; }

    public override void ExecuteAction(params object[] parameter) {
        if (IsInGame()) {
            LogExecution(parameter);
            CheatsCombat.RestAll();
        }
    }
}
