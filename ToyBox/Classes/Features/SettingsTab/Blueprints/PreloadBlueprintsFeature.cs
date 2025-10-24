namespace ToyBox.Features.SettingsFeatures.Blueprints;

public partial class PreloadBlueprintsFeature : ToggledFeature {
    [LocalizedString("ToyBox_Features_SettingsFeatures_BlueprintLoaderSettings_PreloadBlueprintsFeature_PreloadBlueprintsText", "Preload Blueprints")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_SettingsFeatures_BlueprintLoaderSettings_PreloadBlueprintsFeature_AlwaysLoadAllBlueprintsOnGameSta", "Always load all Blueprints on game Start. This increases initial load times and RAM usage, but can decrease game loading times once finished.")]
    public override partial string Description { get; }
    public override ref bool IsEnabled {
        get {
            return ref Settings.PreloadBlueprints;
        }
    }
}
