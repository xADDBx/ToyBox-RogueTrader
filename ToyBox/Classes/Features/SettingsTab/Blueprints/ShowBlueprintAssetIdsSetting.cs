namespace ToyBox.Features.SettingsFeatures.Blueprints;

[IsTested]
public partial class ShowBlueprintAssetIdsSetting : ToggledFeature {
    public override ref bool IsEnabled {
        get {
            return ref Settings.ToggleShowBlueprintAssetIds;
        }
    }

    [LocalizedString("ToyBox_Features_SettingsFeatures_Blueprints_ShowBlueprintAssetIdsSetting_Name", "Show Blueprint AssetGuids")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_SettingsFeatures_Blueprints_ShowBlueprintAssetIdsSetting_Description", "Whether Browsers should display the asset id of a blueprint.")]
    public override partial string Description { get; }
}
