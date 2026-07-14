namespace ToyBox.Features.Colonies;

public partial class ColoniesFeatureTab : FeatureTab {
    [LocalizedString("ToyBox_Features_Colonies_ColoniesFeatureTab_Name", "Colonies")]
    public override partial string Name { get; }
    public ColoniesFeatureTab() {
        AddFeature(new ColonyEditorFeature());
    }
}
