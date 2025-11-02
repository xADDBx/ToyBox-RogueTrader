namespace ToyBox.Features.SettingsFeatures.BrowserSettings;

[IsTested]
public partial class SearchAsYouTypeFeature : ToggledFeature {
    public override ref bool IsEnabled {
        get {
            return ref Settings.ToggleSearchAsYouType;
        }
    }

    [LocalizedString("ToyBox_Features_SettingsFeatures_BrowserSettings_SearchAsYouTypeFeature_Name", "Search as you type")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_SettingsFeatures_BrowserSettings_SearchAsYouTypeFeature_Description", "Automatically start a new search after a configurable delay even while you are typing")]
    public override partial string Description { get; }
}
