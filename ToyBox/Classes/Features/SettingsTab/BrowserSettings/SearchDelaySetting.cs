namespace ToyBox.Features.SettingsFeatures.BrowserSettings;

[IsTested]
public partial class SearchDelaySetting : FeatureWithFloatSlider {
    public override bool IsEnabled {
        get {
            return Settings.ToggleSearchAsYouType;
        }
    }

    public override ref float Value {
        get {
            return ref Settings.SearchDelay;
        }
    }

    public override bool ShouldHide {
        get {
            return !GetInstance<SearchAsYouTypeFeature>().IsEnabled;
        }
    }

    public override float Min {
        get {
            return 0f;
        }
    }

    public override float Max {
        get {
            return 5f;
        }
    }

    public override float? Default {
        get {
            return 0.3f;
        }
    }

    [LocalizedString("ToyBox_Features_SettingsFeatures_BrowserSettings_SearchDelaySetting_Name", "Search Delay")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_SettingsFeatures_BrowserSettings_SearchDelaySetting_Description", "This is the time in seconds that is waited before a new search is automatically started")]
    public override partial string Description { get; }
}
