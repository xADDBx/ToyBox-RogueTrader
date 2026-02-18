namespace ToyBox.Features.SettingsFeatures.BrowserSettings;

[IsTested]
public partial class SearchDescriptionsSetting : ToggledFeature {
    public override ref bool IsEnabled {
        get {
            return ref Settings.ToggleSearchDescriptions;
        }
    }

    [LocalizedString("ToyBox_Features_SettingsFeatures_BrowserSettings_SearchDescriptionsSetting_Name", "Search Descriptions")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_SettingsFeatures_BrowserSettings_SearchDescriptionsSetting_Description", "Also search through the descriptions of blueprints")]
    public override partial string Description { get; }
    public override void Enable() {
        base.Enable();
        BPHelper.ClearNameCaches();
    }
    public override void Disable() {
        base.Disable();
        BPHelper.ClearNameCaches();
    }
}
