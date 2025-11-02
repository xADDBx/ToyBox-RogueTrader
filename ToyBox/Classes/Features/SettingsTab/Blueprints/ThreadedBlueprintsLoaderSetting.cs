namespace ToyBox.Features.SettingsFeatures.Blueprints;

public partial class ThreadedBlueprintsLoaderSetting : ToggledFeature {
    public override ref bool IsEnabled {
        get {
            return ref Settings.EnableThreadedBlueprintLoader;
        }
    }
    [LocalizedString("ToyBox_Features_SettingsFeatures_Blueprints_ThreadedBlueprintsLoaderSetting_Name", "Enable threaded Blueprint Loader (Needs Restart!)")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_SettingsFeatures_Blueprints_ThreadedBlueprintsLoaderSetting_Description", "Massively speeds up blueprint loading; but can have very rare bugs (especially after updates).")]
    public override partial string Description { get; }
}
