namespace ToyBox.Features.SearchAndPick;

public partial class SearchAndPickFeatureTab : FeatureTab {
    [LocalizedString("ToyBox_Features_SearchAndPick_SearchAndPickFeatureTab_Name", "Search 'n Pick")]
    public override partial string Name { get; }
    public SearchAndPickFeatureTab() {
        AddFeature(new SearchAndPickFeature());
    }
}
