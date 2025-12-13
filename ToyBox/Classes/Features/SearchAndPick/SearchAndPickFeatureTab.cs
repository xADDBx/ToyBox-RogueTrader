namespace ToyBox.Features.SearchAndPick;

public partial class SearchAndPickFeatureTab : FeatureTab {
    [LocalizedString("ToyBox_Features_SearchAndPick_SearchAndPickFeatureTab_Name", "Search 'n Pick")]
    public override partial string Name { get; }
    public SearchAndPickFeatureTab() {
        AddFeature(new SortCollationCategoriesByCountSetting());
        AddFeature(new SearchAndPickFeature());
    }
    public override void OnGui() {
        Feature.GetInstance<SearchAndPickFeature>().OnGui();
    }
}
