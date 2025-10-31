using Kingmaker;
using Kingmaker.UnitLogic.Parts;

namespace ToyBox.Features.BagOfTricks.Combat;
[NeedsTesting]
public partial class RestSelectedFeature : FeatureWithBindableAction {
    [LocalizedString("ToyBox_Features_BagOfTricks_Combat_RestSelectedFeature_Name", "Rest Selected")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_Combat_RestSelectedFeature_Description", "Revives and heals the selected characters + restores action points and ability cooldowns.")]
    public override partial string Description { get; }

    public override void ExecuteAction(params object[] parameter) {
        if (IsInGame()) {
            var units = Game.Instance.SelectionCharacter?.SelectedUnits ?? [];
            LogExecution(units);
            foreach (var unit in units) {
                PartHealth.RestUnit(unit);
            }
        }
    }
}
