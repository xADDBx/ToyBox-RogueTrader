using Kingmaker;

namespace ToyBox.Features.BagOfTricks.Combat;
public partial class RemoveBuffsFeature : FeatureWithBindableAction {
    [LocalizedString("ToyBox_Features_BagOfTricks_Combat_RemoveBuffsFeature_Name", "Remove Buffs")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_Combat_RemoveBuffsFeature_Description", "Removes all non-hidden and non-persistent buffs from all party members and pets.")]
    public override partial string Description { get; }

    public override void ExecuteAction(params object[] parameter) {
        if (IsInGame()) {
            var units = Game.Instance.Player?.PartyAndPets ?? [];
            LogExecution(units);
            foreach (var unit in units) {
                foreach (var buff in unit.Buffs.Enumerable) {
                    if (buff.Blueprint.IsHiddenInUI) {
                        continue;
                    }

                    if (buff.Blueprint.StayOnDeath) {
                        continue;
                    }

                    unit.Facts.Remove(buff);
                }
            }
        }
    }
}
