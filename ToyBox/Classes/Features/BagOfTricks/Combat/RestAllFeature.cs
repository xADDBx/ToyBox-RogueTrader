using Kingmaker.Cheats;
using ToyBox.Classes.Infrastructure.Features;

namespace ToyBox.Features.BagOfTricks.Combat;

[IsTested]
public partial class RestAllFeature : FeatureWithBindableAction {
    [LocalizedString("ToyBox_Features_BagOfTricks_Combat_RestAllFeature_Name", "Rest All")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_Combat_RestAllFeature_Description", "Revives and heals all characters + restores action points, ability cooldowns and item charges.")]
    public override partial string Description { get; }
    public override bool CanExecute(ActionParameter parameter) {
        return IsInGame();
    }
    public override void ExecuteAction(ActionParameter parameter) {
        if (CanExecute(parameter)) {
            LogExecution(parameter);
            CheatsCombat.RestAll();
        }
    }
}
