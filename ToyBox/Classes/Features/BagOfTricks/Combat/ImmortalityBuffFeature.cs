using Kingmaker.Cheats;
using ToyBox.Classes.Infrastructure.Features;

namespace ToyBox.Features.BagOfTricks.Combat;

[IsTested]
public partial class ImmortalityBuffFeature : FeatureWithBindableAction {
    [LocalizedString("ToyBox_Features_BagOfTricks_Combat_ImmortalityBuffFeature_Name", "Make Immortal")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_Combat_ImmortalityBuffFeature_Description", "Applies a buff to the selected units which makes them immortal.")]
    public override partial string Description { get; }
    public override bool CanExecute(ActionParameter parameter) {
        return IsInGame();
    }
    public override void ExecuteAction(ActionParameter parameter) {
        if (CanExecute(parameter)) {
            LogExecution(parameter);
            CheatsCombat.Iddqd("");
        }
    }
}
