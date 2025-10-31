using Kingmaker.Cheats;

namespace ToyBox.Features.BagOfTricks.Combat;
[NeedsTesting]
public partial class ImmortalityBuffFeature : FeatureWithBindableAction {
    [LocalizedString("ToyBox_Features_BagOfTricks_Combat_ImmortalityBuffFeature_Name", "Make Immortal")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_Combat_ImmortalityBuffFeature_Description", "Applies a buff to the selected units which makes them immortal.")]
    public override partial string Description { get; }

    public override void ExecuteAction(params object[] parameter) {
        if (IsInGame()) {
            LogExecution(parameter);
            CheatsCombat.Iddqd("");
        }
    }
}
