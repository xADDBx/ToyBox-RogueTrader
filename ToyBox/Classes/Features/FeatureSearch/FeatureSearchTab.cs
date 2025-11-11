namespace ToyBox.Features.FeatureSearch;

public partial class FeatureSearchTab : FeatureTab {
    [LocalizedString("ToyBox_Features_FeatureSearch_FeatureSearchTab_Name", "Feature Search")]
    public override partial string Name { get; }
    public FeatureSearchTab() {
        AddFeature(new FeatureSearchFeature());
    }
}
