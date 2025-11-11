namespace ToyBox.Features.BagOfTricks.RTSpecific;

public partial class ModifyFactionReputationFeature : Feature {
    [LocalizedString("ToyBox_Features_BagOfTricks_RTSpecific_ModifyFactionReputationFeature_Name", "Modify Faction Reputation")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_RTSpecific_ModifyFactionReputationFeature_Description", "Allows you to modify the reputation at the various in-game factions.")]
    public override partial string Description { get; }

    public override void OnGui() {
        throw new NotImplementedException();
    }
}
