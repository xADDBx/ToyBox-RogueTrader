namespace ToyBox.Features.SearchAndPick;

public partial class SortCollationCategoriesByCountSetting : ToggledFeature {
    public override ref bool IsEnabled {
        get {
            return ref Settings.SortCollationCategoriesByCount;
        }
    }
    [LocalizedString("ToyBox_Features_SearchAndPick_SortCollationCategoriesByCountSetting_Name", "Sort by Count")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_SearchAndPick_SortCollationCategoriesByCountSetting_Description", "Whether to sort collation categories by their amount of blueprints instead of names.")]
    public override partial string Description { get; }
}
