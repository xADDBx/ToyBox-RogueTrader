namespace ToyBox.Features.SettingsFeatures.Blueprints;
[IsTested]
public partial class BlueprintsLoaderChunkSizeSetting : FeatureWithLogIntSlider {
    [LocalizedString("ToyBox_Features_SettingsFeatures_Blueprints_BlueprintsLoaderChunkSizeSetting_Name", "Chunk Size")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_SettingsFeatures_Blueprints_BlueprintsLoaderChunkSizeSetting_Description", "Affects the amount of blueprints a thread loads at once. A lower number means better load balancing but more synchronization overhead.")]
    public override partial string Description { get; }
    public override bool IsEnabled {
        get {
            return true;
        }
    }

    public override ref int Value {
        get {
            return ref Settings.BlueprintsLoaderChunkSize;
        }
    }

    public override int Min {
        get {
            return 1;
        }
    }

    public override int Max {
        get {
            return 250000;
        }
    }

    public override int? Default {
        get {
            return 200;
        }
    }
    public override bool ShouldHide {
        get {
            return !GetInstance<ThreadedBlueprintsLoaderSetting>().IsEnabled;
        }
    }
}
