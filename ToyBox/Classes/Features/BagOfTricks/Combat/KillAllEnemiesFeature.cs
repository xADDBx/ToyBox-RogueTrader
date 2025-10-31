using Kingmaker;
using Kingmaker.Cheats;

namespace ToyBox.Features.BagOfTricks.Combat;
[NeedsTesting]
public partial class KillAllEnemiesFeature : FeatureWithBindableAction {
    [LocalizedString("ToyBox_Features_BagOfTricks_Combat_KillAllEnemiesFeature_Name", "Kill All Enemies")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_Combat_KillAllEnemiesFeature_Description", "Kills all enemies that are currently in combat with you.")]
    public override partial string Description { get; }

    public override void ExecuteAction(params object[] parameter) {
        if (IsInGame() && Game.Instance.Player.IsInCombat) {
            LogExecution(parameter);
            var units = Game.Instance.State?.AllBaseUnits ?? [];
            foreach (var unit in units) {
                if (unit.IsInCombat && unit.IsPlayerEnemy) {
                    CheatsCombat.KillUnit(unit);
                }
            }
        }
    }
}
